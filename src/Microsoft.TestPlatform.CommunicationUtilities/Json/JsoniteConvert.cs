// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETFRAMEWORK

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

using Jsonite;

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Serialization;

/// <summary>
/// Reflection-based converter for mapping between Jsonite's untyped objects
/// (JsonObject/JsonArray/primitives) and strongly-typed .NET objects.
/// Used on .NET Framework where System.Text.Json is not available.
/// </summary>
internal static class JsoniteConvert
{
    /// <summary>
    /// Convert a .NET object into a Jsonite-serializable value
    /// (JsonObject, JsonArray, or primitive).
    /// </summary>
    public static object? ToJsonValue(object? value)
    {
        return ToJsonValueCore(value, new HashSet<object>(ReferenceEqualityComparer.Instance), 0);
    }

    private const int MaxDepth = 64;

    private static object? ToJsonValueCore(object? value, HashSet<object> visited, int depth)
    {
        if (value is null)
            return null;

        if (depth > MaxDepth)
            return null; // Prevent stack overflow on deeply nested structures

        var type = value.GetType();

        // Primitives, strings, and decimals pass through directly
        if (type.IsPrimitive || value is string || value is decimal)
            return value;

        // Enums → integer
        if (type.IsEnum)
            return Convert.ToInt32(value, CultureInfo.InvariantCulture);

        // Common value types → string
        if (value is Guid g)
            return g.ToString("D");
        if (value is Uri u)
            return u.OriginalString;
        if (value is DateTimeOffset dto)
            return dto.ToString("o", CultureInfo.InvariantCulture);
        if (value is DateTime dt)
            return dt.ToString("o", CultureInfo.InvariantCulture);
        if (value is TimeSpan ts)
            return ts.ToString();

        // Framework types that should not be traversed via reflection
        if (value is Type t)
            return t.AssemblyQualifiedName;
        if (value is Delegate || value is MemberInfo || value is System.Reflection.Assembly || value is System.Reflection.Module)
            return null;

        // Cycle detection for reference types
        if (!type.IsValueType && !visited.Add(value))
            return null; // Already visited — break the cycle

        try
        {
            // Dictionary<string, *> → JsonObject
            if (value is IDictionary dict)
            {
                var obj = new JsonObject();
                foreach (DictionaryEntry entry in dict)
                {
                    obj[Convert.ToString(entry.Key, CultureInfo.InvariantCulture) ?? string.Empty] = ToJsonValueCore(entry.Value, visited, depth + 1)!;
                }
                return obj;
            }

            // IEnumerable (but not string/dict) → JsonArray
            if (value is IEnumerable enumerable)
            {
                var arr = new JsonArray();
                foreach (var item in enumerable)
                {
                    arr.Add(ToJsonValueCore(item, visited, depth + 1)!);
                }
                return arr;
            }

            // Complex objects → JsonObject via reflection
            var result = new JsonObject();
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
                    continue;

                try
                {
                    var propValue = prop.GetValue(value);
                    result[prop.Name] = ToJsonValueCore(propValue, visited, depth + 1)!;
                }
                catch
                {
                    // Skip properties that throw on access
                }
            }
            return result;
        }
        finally
        {
            if (!type.IsValueType)
                visited.Remove(value);
        }
    }

    /// <summary>
    /// Reference equality comparer for cycle detection.
    /// </summary>
    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();
        bool IEqualityComparer<object>.Equals(object? x, object? y) => ReferenceEquals(x, y);
        int IEqualityComparer<object>.GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }

    /// <summary>
    /// Convert a Jsonite-deserialized value to a strongly-typed .NET object.
    /// </summary>
    public static T? To<T>(object? value)
    {
        return (T?)ConvertTo(value, typeof(T));
    }

    private static object? ConvertTo(object? value, Type targetType)
    {
        if (value is null)
        {
            return targetType.IsValueType && Nullable.GetUnderlyingType(targetType) is null
                ? Activator.CreateInstance(targetType)
                : null;
        }

        // Nullable<T> — unwrap and convert to underlying type
        var underlyingType = Nullable.GetUnderlyingType(targetType);
        if (underlyingType is not null)
            return ConvertTo(value, underlyingType);

        // If target is object, return as-is (Jsonite already produced a reasonable object)
        if (targetType == typeof(object))
            return value;

        // Assignable — no conversion needed
        if (targetType.IsInstanceOfType(value))
            return value;

        // String
        if (targetType == typeof(string))
            return Convert.ToString(value, CultureInfo.InvariantCulture);

        // Boolean — handle string "true"/"false" and numeric 0/1
        if (targetType == typeof(bool))
        {
            if (value is bool b)
                return b;
            if (value is string bs)
                return bool.Parse(bs);
            return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
        }

        // Enum
        if (targetType.IsEnum)
        {
            if (value is string s)
                return Enum.Parse(targetType, s, ignoreCase: true);
            return Enum.ToObject(targetType, Convert.ToInt64(value, CultureInfo.InvariantCulture));
        }

        // Numeric types
        if (targetType == typeof(int))
            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        if (targetType == typeof(long))
            return Convert.ToInt64(value, CultureInfo.InvariantCulture);
        if (targetType == typeof(double))
            return Convert.ToDouble(value, CultureInfo.InvariantCulture);
        if (targetType == typeof(float))
            return Convert.ToSingle(value, CultureInfo.InvariantCulture);
        if (targetType == typeof(short))
            return Convert.ToInt16(value, CultureInfo.InvariantCulture);
        if (targetType == typeof(byte))
            return Convert.ToByte(value, CultureInfo.InvariantCulture);
        if (targetType == typeof(decimal))
            return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        if (targetType == typeof(uint))
            return Convert.ToUInt32(value, CultureInfo.InvariantCulture);
        if (targetType == typeof(ulong))
            return Convert.ToUInt64(value, CultureInfo.InvariantCulture);
        if (targetType == typeof(ushort))
            return Convert.ToUInt16(value, CultureInfo.InvariantCulture);
        if (targetType == typeof(sbyte))
            return Convert.ToSByte(value, CultureInfo.InvariantCulture);

        // Guid
        if (targetType == typeof(Guid))
            return Guid.Parse(Convert.ToString(value, CultureInfo.InvariantCulture)!);

        // Uri
        if (targetType == typeof(Uri))
        {
            var uriStr = Convert.ToString(value, CultureInfo.InvariantCulture);
            return uriStr is null ? null : new Uri(uriStr);
        }

        // DateTime / DateTimeOffset
        if (targetType == typeof(DateTime))
            return DateTime.Parse(Convert.ToString(value, CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind);
        if (targetType == typeof(DateTimeOffset))
            return DateTimeOffset.Parse(Convert.ToString(value, CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture);

        // TimeSpan
        if (targetType == typeof(TimeSpan))
            return TimeSpan.Parse(Convert.ToString(value, CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture);

        // Array
        if (targetType.IsArray && value is IList sourceArray)
        {
            var elemType = targetType.GetElementType()!;
            var arr = Array.CreateInstance(elemType, sourceArray.Count);
            for (int i = 0; i < sourceArray.Count; i++)
                arr.SetValue(ConvertTo(sourceArray[i], elemType), i);
            return arr;
        }

        // Generic collections (List<T>, IList<T>, IEnumerable<T>, ICollection<T>, etc.)
        if (targetType.IsGenericType && value is IList sourceList)
        {
            var genDef = targetType.GetGenericTypeDefinition();
            if (genDef == typeof(List<>) || genDef == typeof(IList<>) ||
                genDef == typeof(IEnumerable<>) || genDef == typeof(ICollection<>) ||
                genDef == typeof(IReadOnlyList<>) || genDef == typeof(IReadOnlyCollection<>))
            {
                var elemType = targetType.GetGenericArguments()[0];
                var listType = typeof(List<>).MakeGenericType(elemType);
                var resultList = (IList)Activator.CreateInstance(listType)!;
                foreach (var item in sourceList)
                    resultList.Add(ConvertTo(item, elemType));
                return resultList;
            }
        }

        // Dictionary<K,V>
        if (targetType.IsGenericType && value is IDictionary<string, object> sourceDict)
        {
            var genDef = targetType.GetGenericTypeDefinition();
            if (genDef == typeof(Dictionary<,>) || genDef == typeof(IDictionary<,>))
            {
                var keyType = targetType.GetGenericArguments()[0];
                var valType = targetType.GetGenericArguments()[1];

                // Special case: Dictionary<string, IEnumerable<T>> — common in vstest payloads
                if (valType.IsGenericType)
                {
                    var valGenDef = valType.GetGenericTypeDefinition();
                    if (valGenDef == typeof(IEnumerable<>) || valGenDef == typeof(IList<>) ||
                        valGenDef == typeof(ICollection<>) || valGenDef == typeof(List<>))
                    {
                        var dictType = typeof(Dictionary<,>).MakeGenericType(keyType, valType);
                        var resultDict2 = (IDictionary)Activator.CreateInstance(dictType)!;
                        foreach (var kvp in sourceDict)
                        {
                            var key = ConvertTo(kvp.Key, keyType);
                            var val = ConvertTo(kvp.Value, valType);
                            resultDict2[key!] = val;
                        }
                        return resultDict2;
                    }
                }

                var resultDictType = typeof(Dictionary<,>).MakeGenericType(keyType, valType);
                var resultDict = (IDictionary)Activator.CreateInstance(resultDictType)!;
                foreach (var kvp in sourceDict)
                {
                    var key = ConvertTo(kvp.Key, keyType);
                    var val = ConvertTo(kvp.Value, valType);
                    resultDict[key!] = val;
                }
                return resultDict;
            }
        }

        // Complex objects: create from JsonObject using reflection
        if (value is IDictionary<string, object> objDict)
        {
            var instance = CreateInstance(targetType, objDict);
            var properties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var kvp in objDict)
            {
                var prop = FindProperty(properties, kvp.Key);
                if (prop is null)
                    continue;

                try
                {
                    var converted = ConvertTo(kvp.Value, prop.PropertyType);

                    if (prop.GetSetMethod() is not null)
                    {
                        // Public setter available
                        prop.SetValue(instance, converted);
                    }
                    else
                    {
                        // Private setter or no setter — try backing field
                        var backingField = targetType.GetField($"<{prop.Name}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (backingField is not null)
                        {
                            backingField.SetValue(instance, converted);
                        }
                        else
                        {
                            // Last resort: try private setter via reflection
                            var privateSetter = prop.GetSetMethod(nonPublic: true);
                            privateSetter?.Invoke(instance, new[] { converted });
                        }
                    }
                }
                catch
                {
                    // Skip properties that fail to set
                }
            }

            return instance;
        }

        // Last resort: try Convert.ChangeType
        try
        {
            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }
        catch
        {
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }
    }

    private static PropertyInfo? FindProperty(PropertyInfo[] properties, string name)
    {
        // Exact match first
        foreach (var p in properties)
        {
            if (string.Equals(p.Name, name, StringComparison.Ordinal))
                return p;
        }
        // Case-insensitive fallback
        foreach (var p in properties)
        {
            if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                return p;
        }
        return null;
    }

    private static object CreateInstance(Type type, IDictionary<string, object> data)
    {
        // Try parameterless constructor
        var paramlessCtor = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
        if (paramlessCtor is not null)
            return paramlessCtor.Invoke(Array.Empty<object>());

        // Try to find a constructor whose parameters match JSON property names
        foreach (var ctor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
        {
            var parameters = ctor.GetParameters();
            var args = new object?[parameters.Length];
            bool allResolved = true;

            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                // Match parameter name (case-insensitive) to JSON keys
                var key = data.Keys.FirstOrDefault(k => string.Equals(k, param.Name, StringComparison.OrdinalIgnoreCase));
                if (key is not null)
                {
                    args[i] = ConvertTo(data[key], param.ParameterType);
                }
                else if (param.HasDefaultValue)
                {
                    args[i] = param.DefaultValue;
                }
                else
                {
                    allResolved = false;
                    break;
                }
            }

            if (allResolved)
                return ctor.Invoke(args);
        }

        // Last resort: create uninitialized object (no constructor call)
        return FormatterServices.GetUninitializedObject(type);
    }
}

#endif
