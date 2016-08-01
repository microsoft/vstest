// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;

    using Newtonsoft.Json;

    /// <summary>
    ///  Base class for test related classes.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1012:AbstractTypesShouldNotHaveConstructors")]
    [DataContract]
    public abstract class TestObject
    {
        #region Fields

        /// <summary>
        /// The store for all the properties registered.
        /// </summary>
        /// <remarks>
        /// Not sending custom properties over because of serialization issues. 
        /// Custom TypeConverters for TestProperty does not work in Core currently. 
        /// This is not immediately needed for dotnet-test integration. Need to revisit.
        /// </remarks>
        [DataMember]
        [JsonIgnore]
#if FullCLR
        private Dictionary<TestProperty, object> store;
#else
        public Dictionary<TestProperty, object> store;
#endif
        
        /// <summary>
        /// Property used for Json (de)serialization of store dictionary
        /// Notes: Set the ObjectCreationHandling to Replace so that JSON does not use the "store" value as the value to set
        /// Notes: JSON will use "Auto" setting which does not replace the value from the payload message if "get" returns values
        /// Notes: "Replace" setting will always replace the property value from payload message
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<KeyValuePair<TestProperty, object>> StorekvpList
        {
            get
            {
                return this.store.ToList();
            }

            set
            {
                if (this.store == null)
                {
                    this.store = new Dictionary<TestProperty, object>();
                }

                foreach (var kvp in value.Where(kvp => !this.store.Keys.Contains(kvp.Key)))
                {
                    TestProperty.Register(kvp.Key.Id, kvp.Key.Label, kvp.Key.GetValueType(), typeof(TestObject));
                    this.SetPropertyValue(kvp.Key, kvp.Value);
                }
            }
        }

        #endregion Fields

        #region Constructors

#if FullCLR
        protected TestObject()
#else
        public TestObject()
#endif
        {
            this.store = new Dictionary<TestProperty, object>();
        }

        [OnSerializing]
#if FullCLR
        private void CacheLazyValuesOnSerializing(StreamingContext context)
#else
        public void CacheLazyValuesOnSerializing(StreamingContext context)
#endif
        {
            var lazyValues = this.store.Where(kvp => kvp.Value is ILazyPropertyValue).ToArray();

            foreach (var kvp in lazyValues)
            {
                var lazyValue = (ILazyPropertyValue)kvp.Value;
                var value = lazyValue.Value;
                this.store.Remove(kvp.Key);

                if (value != null)
                {
                    this.store.Add(kvp.Key, value);
                }
            }
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        ///   Returns the TestProperties currently specified in this TestObject.
        /// </summary>
        public IEnumerable<TestProperty> Properties
        {
            get { return this.store.Keys; }
        }

        /// <summary>
        /// Returns property value of the specify TestProperty
        /// </summary>
        /// <param name="property">TestObject's TestProperty</param>
        /// <returns>property value</returns>
        public object GetPropertyValue(TestProperty property)
        {
            ValidateArg.NotNull(property, "property");

            object defaultValue = null;
            var valueType = property.GetValueType();

            if (valueType != null && valueType.GetTypeInfo().IsValueType)
            {
                defaultValue = Activator.CreateInstance(valueType);
            }

            return PrivateGetPropertyValue(property, defaultValue);
        }

        /// <summary>
        ///   Returns property value of the specify TestProperty
        /// </summary>
        /// <typeparam name="T">Property value type</typeparam>
        /// <param name="property">TestObject's TestProperty</param>
        /// <param name="defaultValue">default property value if property is not present</param>
        /// <returns>property value</returns>
        public T GetPropertyValue<T>(TestProperty property, T defaultValue)
        {
            return GetPropertyValue<T>(property, defaultValue, CultureInfo.InvariantCulture);
        }

        /// <summary>
        ///   Set TestProperty's value
        /// </summary>
        /// <typeparam name="T">Property value type</typeparam>
        /// <param name="property">TestObject's TestProperty</param>
        /// <param name="value">value to be set</param>
        public void SetPropertyValue<T>(TestProperty property, T value)
        {
            SetPropertyValue<T>(property, value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        ///   Set TestProperty's value
        /// </summary>
        /// <typeparam name="T">Property value type</typeparam>
        /// <param name="property">TestObject's TestProperty</param>
        /// <param name="value">value to be set</param>
        public void SetPropertyValue<T>(TestProperty property, LazyPropertyValue<T> value)
        {
            SetPropertyValue<T>(property, value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        ///   Set TestProperty's value
        /// </summary>
        /// <param name="property">TestObject's TestProperty</param>
        /// <param name="value">value to be set</param>
        public void SetPropertyValue(TestProperty property, object value)
        {
            PrivateSetPropertyValue(property, value);
        }

        /// <summary>
        ///  Remove test property from the current TestObject.
        /// </summary>
        /// <param name="property"></param>
        public void RemovePropertyValue(TestProperty property)
        {
            ValidateArg.NotNull(property, "property");

            object value;
            if (this.store.TryGetValue(property, out value))
            {
                this.store.Remove(property);
            }
        }


        /// <summary>
        ///   Returns TestProperty's value 
        /// </summary>
        /// <returns>property's value. default value is returned if the property is not present</returns>
        public T GetPropertyValue<T>(TestProperty property, T defaultValue, CultureInfo culture)
        {
            ValidateArg.NotNull(property, "property");
            ValidateArg.NotNull(culture, "culture");

            object objValue = PrivateGetPropertyValue(property, defaultValue);

            return ConvertPropertyTo<T>(property, culture, objValue);
        }

        /// <summary>
        ///   Set TestProperty's value to the specified value T.
        /// </summary>
        public void SetPropertyValue<T>(TestProperty property, T value, CultureInfo culture)
        {
            ValidateArg.NotNull(property, "property");
            ValidateArg.NotNull(culture, "culture");

            object objValue = ConvertPropertyFrom<T>(property, culture, value);

            PrivateSetPropertyValue(property, objValue);
        }

        /// <summary>
        ///   Set TestProperty's value to the specified value T.
        /// </summary>
        public void SetPropertyValue<T>(TestProperty property, LazyPropertyValue<T> value, CultureInfo culture)
        {
            ValidateArg.NotNull(property, "property");
            ValidateArg.NotNull(culture, "culture");

            object objValue = ConvertPropertyFrom<T>(property, culture, value);

            PrivateSetPropertyValue(property, objValue);
        }

        #endregion Property Values

        #region Helpers
        /// <summary>
        ///   Return TestProperty's value
        /// </summary>
        /// <returns></returns>
        private object PrivateGetPropertyValue(TestProperty property, object defaultValue)
        {
            ValidateArg.NotNull(property, "property");

            object value;
            if (!this.store.TryGetValue(property, out value))
            {
                value = defaultValue;
            }

            return value;
        }

        /// <summary>
        ///   Set TestProperty's value
        /// </summary>
        private void PrivateSetPropertyValue(TestProperty property, object value)
        {
            ValidateArg.NotNull(property, "property");

            if (property.ValidateValueCallback == null || property.ValidateValueCallback(value))
            {
                this.store[property] = value;
            }
            else
            {
                throw new ArgumentException(property.Label);
            }
        }

        /// <summary>
        ///    Convert passed in value from TestProperty's specified value type.
        /// </summary>
        /// <returns>Converted object</returns>
        private static object ConvertPropertyFrom<T>(TestProperty property, CultureInfo culture, object value)
        {
            ValidateArg.NotNull(property, "property");
            ValidateArg.NotNull(culture, "culture");

            var valueType = property.GetValueType();

            if (valueType != null && valueType.GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo()))
            {
                return value;
            }

            TypeConverter converter = TypeDescriptor.GetConverter(valueType);
            if (converter == null)
            {
                throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, Resources.ConverterNotSupported, valueType.Name));
            }

            try
            {
                return converter.ConvertFrom(null, culture, value);
            }
            catch (FormatException)
            {
                throw;
            }
            catch (Exception e)
            {
                // some type converters throw strange exceptions (eg: System.Exception by Int32Converter)
                throw new FormatException(e.Message, e);
            }
        }

        /// <summary>
        ///   Convert passed in value into the specified type when property is registered.
        /// </summary>
        /// <returns>Converted object</returns>
        private static T ConvertPropertyTo<T>(TestProperty property, CultureInfo culture, object value)
        {
            ValidateArg.NotNull(property, "property");
            ValidateArg.NotNull(culture, "culture");

            var lazyValue = value as LazyPropertyValue<T>;

            if (value == null)
            {
                return default(T);
            }
            else if (value is T)
            {
                return (T)value;
            }
            else if (lazyValue != null)
            {
                return lazyValue.Value;
            }

            var valueType = property.GetValueType();

            TypeConverter converter = TypeDescriptor.GetConverter(valueType);

            if (converter == null)
            {
                throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, Resources.ConverterNotSupported, valueType.Name));
            }

            try
            {
                return (T)converter.ConvertTo(null, culture, value, typeof(T));
            }
            catch (FormatException)
            {
                throw;
            }
            catch (Exception e)
            {
                // some type converters throw strange exceptions (eg: System.Exception by Int32Converter)
                throw new FormatException(e.Message, e);
            }
        }

        #endregion Helpers

        private TraitCollection traits;

        public TraitCollection Traits
        {
            get
            {
                if (this.traits == null)
                {
                    this.traits = new TraitCollection(this);
                }

                return this.traits;
            }
        }
    }
}
