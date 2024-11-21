// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

using Microsoft.VisualStudio.TestPlatform.CoreUtilities;

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel;

/// <summary>
///  Base class for test related classes.
/// </summary>
[DataContract]
public abstract class TestObject
{
    private static readonly CustomKeyValueConverter KeyValueConverter = new();
    private static readonly CustomStringArrayConverter StringArrayConverter = new();

    // https://github.com/dotnet/runtime/blob/36bcc2c96045c6793dcfe3151c51a0597537917a/src/libraries/System.ComponentModel.TypeConverter/src/System/ComponentModel/ReflectTypeDescriptionProvider.cs#L154-L180
    // TODO: Validate if some types are not needed and remove them.
    private static readonly BooleanConverter BooleanConverter = new();
    private static readonly ByteConverter ByteConverter = new();
    private static readonly SByteConverter SByteConverter = new();
    private static readonly CharConverter CharConverter = new();
    private static readonly DoubleConverter DoubleConverter = new();
    private static readonly StringConverter StringConverter = new();
    private static readonly Int32Converter IntConverter = new();
#if NET7_0_OR_GREATER
    private static readonly Int128Converter Int128Converter = new();
#endif
    private static readonly Int16Converter Int16Converter = new();
    private static readonly Int64Converter Int64Converter = new();
    private static readonly SingleConverter SingleConverter = new();
#if NET7_0_OR_GREATER
    private static readonly HalfConverter HalfConverter = new();
    private static readonly UInt128Converter UInt128Converter = new();
#endif
    private static readonly UInt16Converter UInt16Converter = new();
    private static readonly UInt32Converter UInt32Converter = new();
    private static readonly UInt64Converter UInt64Converter = new();
    private static readonly TypeConverter TypeConverter = new();
    private static readonly CultureInfoConverter CultureInfoConverter = new();
#if NET7_0_OR_GREATER
    private static readonly DateOnlyConverter DateOnlyConverter = new();
#endif
    private static readonly DateTimeConverter DateTimeConverter = new();
    private static readonly DateTimeOffsetConverter DateTimeOffsetConverter = new();
    private static readonly DecimalConverter DecimalConverter = new();
#if NET7_0_OR_GREATER
    private static readonly TimeOnlyConverter TimeOnlyConverter = new();
#endif
    private static readonly TimeSpanConverter TimeSpanConverter = new();
    private static readonly GuidConverter GuidConverter = new();
    private static readonly UriTypeConverter UriTypeConverter = new();
#if NET7_0_OR_GREATER
    private static readonly VersionConverter VersionConverter = new();
#endif

    /// <summary>
    /// The store for all the properties registered.
    /// </summary>
    private readonly ConcurrentDictionary<TestProperty, object?> _store = new();

    /// <summary>
    /// Property used for Json (de)serialization of store dictionary. Serialization of dictionaries
    /// by default doesn't provide the required object representation. <c>List of KeyValuePair</c> on the
    /// other hand provides a clean Key, Value entries for <c>TestProperty</c> and it's value.
    /// </summary>
    [DataMember(Name = "Properties")]
    private List<KeyValuePair<TestProperty, object?>> StoreKeyValuePairs
    {
        get => _store.ToList();

        set
        {
            // Receive the <TestProperty, String> key value pairs from deserialized entity.
            // Store each property and value in the property data store.
            foreach (var property in value)
            {
                TestProperty.Register(
                    property.Key.Id,
                    property.Key.Label,
                    property.Key.Category,
                    property.Key.Description,
                    property.Key.GetValueType(),
                    null,
                    property.Key.Attributes,
                    typeof(TestObject));

                // Do not call SetPropertyValue(TestProperty property, object value) as it does not
                // invoke ConvertPropertyFrom and does not store the properties in correct types.
                SetPropertyValue(property.Key, property.Value, CultureInfo.InvariantCulture);
            }
        }
    }

    public IEnumerable<KeyValuePair<TestProperty, object?>> GetProperties()
    {
        return _store;
    }

    [OnSerializing]
#if FullCLR
        private void CacheLazyValuesOnSerializing(StreamingContext context)
#else
    public void CacheLazyValuesOnSerializing(StreamingContext context)
#endif
    {
        var lazyValues = _store.Where(kvp => kvp.Value is ILazyPropertyValue).ToArray();

        foreach (var kvp in lazyValues)
        {
            var lazyValue = (ILazyPropertyValue?)kvp.Value;
            var value = lazyValue?.Value;
            _store.TryRemove(kvp.Key, out _);

            if (value != null)
            {
                _store.TryAdd(kvp.Key, value);
            }
        }
    }

    /// <summary>
    /// Returns the TestProperties currently specified in this TestObject.
    /// </summary>
    public virtual IEnumerable<TestProperty> Properties
    {
        get { return _store.Keys; }
    }

    /// <summary>
    /// Returns property value of the specify TestProperty
    /// </summary>
    /// <param name="property">TestObject's TestProperty</param>
    /// <returns>property value</returns>
    public object? GetPropertyValue(TestProperty property)
    {
        ValidateArg.NotNull(property, nameof(property));
        object? defaultValue = null;
        var valueType = property.GetValueType();

        if (valueType != null && valueType.IsValueType)
        {
            if (valueType == typeof(int))
            {
                defaultValue = 0;
            }
            else if (valueType == typeof(Guid))
            {
                defaultValue = new Guid();
            }
            else if (valueType == typeof(bool))
            {
                defaultValue = false;
            }
            else
            {
                throw new ArgumentException($"The type '{valueType}' is unexpected.");
            }
        }

        return ProtectedGetPropertyValue(property, defaultValue);
    }

    /// <summary>
    /// Returns property value of the specify TestProperty
    /// </summary>
    /// <typeparam name="T">Property value type</typeparam>
    /// <param name="property">TestObject's TestProperty</param>
    /// <param name="defaultValue">default property value if property is not present</param>
    /// <returns>property value</returns>
    [return: NotNullIfNotNull("defaultValue")]
    public T? GetPropertyValue<T>(TestProperty property, T? defaultValue)
    {
        return GetPropertyValue(property, defaultValue, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Set TestProperty's value
    /// </summary>
    /// <typeparam name="T">Property value type</typeparam>
    /// <param name="property">TestObject's TestProperty</param>
    /// <param name="value">value to be set</param>
    public void SetPropertyValue<T>(TestProperty property, T value)
    {
        SetPropertyValue(property, value, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Set TestProperty's value
    /// </summary>
    /// <typeparam name="T">Property value type</typeparam>
    /// <param name="property">TestObject's TestProperty</param>
    /// <param name="value">value to be set</param>
    public void SetPropertyValue<T>(TestProperty property, LazyPropertyValue<T> value)
    {
        SetPropertyValue(property, value, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Set TestProperty's value
    /// </summary>
    /// <param name="property">TestObject's TestProperty</param>
    /// <param name="value">value to be set</param>
    public void SetPropertyValue(TestProperty property, object? value)
    {
        ProtectedSetPropertyValue(property, value);
    }

    /// <summary>
    ///  Remove test property from the current TestObject.
    /// </summary>
    /// <param name="property"></param>
    public void RemovePropertyValue(TestProperty property)
    {
        ValidateArg.NotNull(property, nameof(property));
        _store.TryRemove(property, out _);
    }

    /// <summary>
    /// Returns TestProperty's value
    /// </summary>
    /// <returns>property's value. default value is returned if the property is not present</returns>
    [return: NotNullIfNotNull("defaultValue")]
    public T? GetPropertyValue<T>(TestProperty property, T? defaultValue, CultureInfo culture)
    {
        ValidateArg.NotNull(property, nameof(property));
        ValidateArg.NotNull(culture, nameof(culture));
        object? objValue = ProtectedGetPropertyValue(property, defaultValue);

        return ConvertPropertyTo<T>(property, culture, objValue);
    }

    /// <summary>
    /// Set TestProperty's value to the specified value T.
    /// </summary>
    public void SetPropertyValue<T>(TestProperty property, T value, CultureInfo culture)
    {
        ValidateArg.NotNull(property, nameof(property));
        ValidateArg.NotNull(culture, nameof(culture));
        object? objValue = ConvertPropertyFrom<T>(property, culture, value);

        ProtectedSetPropertyValue(property, objValue);
    }

    /// <summary>
    /// Set TestProperty's value to the specified value T.
    /// </summary>
    public void SetPropertyValue<T>(TestProperty property, LazyPropertyValue<T> value, CultureInfo culture)
    {
        ValidateArg.NotNull(property, nameof(property));
        ValidateArg.NotNull(culture, nameof(culture));
        object? objValue = ConvertPropertyFrom<T>(property, culture, value);

        ProtectedSetPropertyValue(property, objValue);
    }

    /// <summary>
    /// Return TestProperty's value
    /// </summary>
    /// <returns></returns>
    [return: NotNullIfNotNull("defaultValue")]
    protected virtual object? ProtectedGetPropertyValue(TestProperty property, object? defaultValue)
    {
        ValidateArg.NotNull(property, nameof(property));
        if (!_store.TryGetValue(property, out var value) || value == null)
        {
            value = defaultValue;
        }

        return value;
    }

    /// <summary>
    /// Set TestProperty's value
    /// </summary>
    protected virtual void ProtectedSetPropertyValue(TestProperty property, object? value)
    {
        ValidateArg.NotNull(property, nameof(property));
        _store[property] = property.ValidateValueCallback == null || property.ValidateValueCallback(value)
            ? value
            : throw new ArgumentException(property.Label);
    }

    /// <summary>
    /// Convert passed in value from TestProperty's specified value type.
    /// </summary>
    /// <returns>Converted object</returns>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Awaiting more evidence that we need to take action. If this caused issues, we should still be able to special case some specific types instead of relying on TypeDescriptor")]
    private static object? ConvertPropertyFrom<T>(TestProperty property, CultureInfo culture, object? value)
    {
        ValidateArg.NotNull(property, nameof(property));
        ValidateArg.NotNull(culture, nameof(culture));
        var valueType = property.GetValueType();

        // Do not try conversion if the object is already of the type we're trying to convert.
        // Note that typeof(T) may be object in case the value is getting deserialized via the StoreKvpList, however
        // the de-serializer could have converted it already, hence the runtime type check.
        if (valueType != null && (valueType.IsAssignableFrom(typeof(T)) || valueType.IsAssignableFrom(value?.GetType())))
        {
            return value;
        }

        // Traits are KeyValuePair based. Use the custom converter in that case.
        if (valueType == typeof(KeyValuePair<string, string>[]))
        {
            return KeyValueConverter.ConvertFrom(null, culture, (string?)value);
        }

        // Use a custom string array converter for string[] types.
        if (valueType == typeof(string[]))
        {
            return StringArrayConverter.ConvertFrom(null, culture, (string?)value);
        }

        try
        {
            // These are already handled by TypeDescriptor.GetConverter, but it's not trimmer safe and
            // we want to make sure they are guaranteed to work.
            if (valueType == typeof(bool))
            {
                return BooleanConverter.ConvertFrom(null, culture, value ?? string.Empty);
            }

            if (valueType == typeof(byte))
            {
                return ByteConverter.ConvertFrom(null, culture, value ?? string.Empty);
            }

            if (valueType == typeof(sbyte))
            {
                return SByteConverter.ConvertFrom(null, culture, value ?? string.Empty);
            }

            if (valueType == typeof(char))
            {
                return CharConverter.ConvertFrom(null, culture, value ?? string.Empty);
            }

            if (valueType == typeof(double))
            {
                return DoubleConverter.ConvertFrom(null, culture, value ?? string.Empty);
            }

            if (valueType == typeof(string))
            {
                return StringConverter.ConvertFrom(null, culture, value ?? string.Empty);
            }

            if (valueType == typeof(int))
            {
                return IntConverter.ConvertFrom(null, culture, value ?? string.Empty);
            }

#if NET7_0_OR_GREATER
            if (valueType == typeof(Int128))
            {
                return Int128Converter.ConvertFrom(null, culture, value ?? string.Empty);
            }
#endif

            if (valueType == typeof(short))
            {
                return Int16Converter.ConvertFrom(null, culture, value ?? string.Empty);
            }

            if (valueType == typeof(long))
            {
                return Int64Converter.ConvertFrom(null, culture, value ?? string.Empty);
            }

            if (valueType == typeof(float))
            {
                return SingleConverter.ConvertFrom(null, culture, value ?? string.Empty);
            }

#if NET7_0_OR_GREATER
            if (valueType == typeof(Half))
            {
                return HalfConverter.ConvertFrom(null, culture, value ?? string.Empty);
            }

            if (valueType == typeof(UInt128))
            {
                return UInt128Converter.ConvertFrom(null, culture, value ?? string.Empty);
            }
#endif

            if (valueType == typeof(ushort))
            {
                return UInt16Converter.ConvertFrom(null, culture, value ?? string.Empty);
            }

            if (valueType == typeof(uint))
            {
                return UInt32Converter.ConvertFrom(null, culture, value ?? string.Empty);
            }

            if (valueType == typeof(ulong))
            {
                return UInt64Converter.ConvertFrom(null, culture, value ?? string.Empty);
            }

            if (valueType == typeof(Type))
            {
                return TypeConverter.ConvertFrom(null, culture, value ?? string.Empty);
            }

            if (valueType == typeof(CultureInfo))
            {
                return CultureInfoConverter.ConvertFrom(null, culture, value ?? string.Empty);
            }

#if NET7_0_OR_GREATER
            if (valueType == typeof(DateOnly))
            {
                return DateOnlyConverter.ConvertFrom(null, culture, value ?? string.Empty);
            }
#endif

            if (valueType == typeof(DateTime))
            {
                return DateTimeConverter.ConvertFrom(null, culture, value ?? string.Empty);
            }

            if (valueType == typeof(DateTimeOffset))
            {
                return DateTimeOffsetConverter.ConvertFrom(null, culture, value ?? string.Empty);
            }

            if (valueType == typeof(decimal))
            {
                return DecimalConverter.ConvertFrom(null, culture, value ?? string.Empty);
            }

#if NET7_0_OR_GREATER
            if (valueType == typeof(TimeOnly))
            {
                return TimeOnlyConverter.ConvertFrom(null, culture, value ?? string.Empty);
            }
#endif

            if (valueType == typeof(TimeSpan))
            {
                return TimeSpanConverter.ConvertFrom(null, culture, value ?? string.Empty);
            }

            if (valueType == typeof(Guid))
            {
                return GuidConverter.ConvertFrom(null, culture, value ?? string.Empty);
            }

            if (valueType == typeof(Uri))
            {
                return UriTypeConverter.ConvertFrom(null, culture, value ?? string.Empty);
            }

#if NET7_0_OR_GREATER
            if (valueType == typeof(Version))
            {
                return VersionConverter.ConvertFrom(null, culture, value ?? string.Empty);
            }
#endif
        }
        catch (FormatException)
        {
            throw;
        }
        catch (Exception e)
        {
            // some type converters throw strange exceptions (e.g.: System.Exception by Int32Converter)
            throw new FormatException(e.Message, e);
        }

        // TODO: Consider detecting that we are in source gen mode (or in NativeAOT mode) and avoid calling TypeDescriptor altogether?
        // Each of the approaches has pros and cons.
        // Ignoring this trimmer unfriendly code when in NativeAOT will help catch issues earlier, and have more deterministic behavior.
        // Keeping this trimmer unfriendly code even in NativeAOT will allow us to still have the possibility to work in case we fall in this path.
        TPDebug.Assert(valueType is not null, "valueType is null");

        throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, Resources.Resources.ConverterNotSupported, valueType.Name));
    }

    /// <summary>
    /// Convert passed in value into the specified type when property is registered.
    /// </summary>
    /// <returns>Converted object</returns>
    [return: NotNullIfNotNull("value")]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    private static T? ConvertPropertyTo<T>(TestProperty property, CultureInfo culture, object? value)
    {
        ValidateArg.NotNull(property, nameof(property));
        ValidateArg.NotNull(culture, nameof(culture));

        if (value == null)
        {
            return default;
        }
        else if (value is T t)
        {
            return t;
        }
        else if (value is LazyPropertyValue<T> lazyValue)
        {
            return lazyValue.Value!;
        }

        var valueType = property.GetValueType();

        try
        {
            // These are already handled by TypeDescriptor.GetConverter, but it's not trimmer safe and
            // we want to make sure they are guaranteed to work.
            if (valueType == typeof(bool))
            {
                return (T?)BooleanConverter.ConvertTo(null, culture, value, typeof(T))!;
            }

            if (valueType == typeof(byte))
            {
                return (T?)ByteConverter.ConvertTo(null, culture, value, typeof(T))!;
            }

            if (valueType == typeof(sbyte))
            {
                return (T?)SByteConverter.ConvertTo(null, culture, value, typeof(T))!;
            }

            if (valueType == typeof(char))
            {
                return (T?)CharConverter.ConvertTo(null, culture, value, typeof(T))!;
            }

            if (valueType == typeof(double))
            {
                return (T?)DoubleConverter.ConvertTo(null, culture, value, typeof(T))!;
            }

            if (valueType == typeof(string))
            {
                return (T?)StringConverter.ConvertTo(null, culture, value, typeof(T))!;
            }

            if (valueType == typeof(int))
            {
                return (T?)IntConverter.ConvertTo(null, culture, value, typeof(T))!;
            }

#if NET7_0_OR_GREATER
            if (valueType == typeof(Int128))
            {
                return (T?)Int128Converter.ConvertTo(null, culture, value, typeof(T))!;
            }
#endif

            if (valueType == typeof(short))
            {
                return (T?)Int16Converter.ConvertTo(null, culture, value, typeof(T))!;
            }

            if (valueType == typeof(long))
            {
                return (T?)Int64Converter.ConvertTo(null, culture, value, typeof(T))!;
            }

            if (valueType == typeof(float))
            {
                return (T?)SingleConverter.ConvertTo(null, culture, value, typeof(T))!;
            }

#if NET7_0_OR_GREATER
            if (valueType == typeof(Half))
            {
                return (T?)HalfConverter.ConvertTo(null, culture, value, typeof(T))!;
            }

            if (valueType == typeof(UInt128))
            {
                return (T?)UInt128Converter.ConvertTo(null, culture, value, typeof(T))!;
            }
#endif

            if (valueType == typeof(ushort))
            {
                return (T?)UInt16Converter.ConvertTo(null, culture, value, typeof(T))!;
            }

            if (valueType == typeof(uint))
            {
                return (T?)UInt32Converter.ConvertTo(null, culture, value, typeof(T))!;
            }

            if (valueType == typeof(ulong))
            {
                return (T?)UInt64Converter.ConvertTo(null, culture, value, typeof(T))!;
            }

            if (valueType == typeof(Type))
            {
                return (T?)TypeConverter.ConvertTo(null, culture, value, typeof(T))!;
            }

            if (valueType == typeof(CultureInfo))
            {
                return (T?)CultureInfoConverter.ConvertTo(null, culture, value, typeof(T))!;
            }

#if NET7_0_OR_GREATER
            if (valueType == typeof(DateOnly))
            {
                return (T?)DateOnlyConverter.ConvertTo(null, culture, value, typeof(T))!;
            }
#endif

            if (valueType == typeof(DateTime))
            {
                return (T?)DateTimeConverter.ConvertTo(null, culture, value, typeof(T))!;
            }

            if (valueType == typeof(DateTimeOffset))
            {
                return (T?)DateTimeOffsetConverter.ConvertTo(null, culture, value, typeof(T))!;
            }

            if (valueType == typeof(decimal))
            {
                return (T?)DecimalConverter.ConvertTo(null, culture, value, typeof(T))!;
            }

#if NET7_0_OR_GREATER
            if (valueType == typeof(TimeOnly))
            {
                return (T?)TimeOnlyConverter.ConvertTo(null, culture, value, typeof(T))!;
            }
#endif

            if (valueType == typeof(TimeSpan))
            {
                return (T?)TimeSpanConverter.ConvertTo(null, culture, value, typeof(T))!;
            }

            if (valueType == typeof(Guid))
            {
                return (T?)GuidConverter.ConvertTo(null, culture, value, typeof(T))!;
            }

            if (valueType == typeof(Uri))
            {
                return (T?)UriTypeConverter.ConvertTo(null, culture, value, typeof(T))!;
            }

#if NET7_0_OR_GREATER
            if (valueType == typeof(Version))
            {
                return (T?)VersionConverter.ConvertTo(null, culture, value, typeof(T))!;
            }
#endif

            throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, Resources.Resources.ConverterNotSupported, valueType.Name));
        }
        catch (FormatException)
        {
            throw;
        }
        catch (Exception e)
        {
            // some type converters throw strange exceptions (e.g.: System.Exception by Int32Converter)
            throw new FormatException(e.Message, e);
        }
    }

    private TraitCollection? _traits;

    public TraitCollection Traits
    {
        get
        {
            _traits ??= new TraitCollection(this);

            return _traits;
        }
    }
}
