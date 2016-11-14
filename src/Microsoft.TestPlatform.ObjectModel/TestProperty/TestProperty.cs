// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Reflection;
    using System.Runtime.Serialization;

    public delegate bool ValidateValueCallback(object value);

    [DataContract]
    public class TestProperty : IEquatable<TestProperty>
    {
        #region Fields

        private Type valueType;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TestProperty"/> class.
        /// </summary>
        private TestProperty()
        {
            // Default constructor for Serialization.
        }

        private TestProperty(string id, string label, string category, string description, Type valueType, ValidateValueCallback validateValueCallback, TestPropertyAttributes attributes)
        {
            ValidateArg.NotNullOrEmpty(id, "id");
            ValidateArg.NotNull(label, "label");
            ValidateArg.NotNull(category, "category");
            ValidateArg.NotNull(description, "description");
            ValidateArg.NotNull(valueType, "valueType");

            // If the type of property is unexpected, then fail as otherwise we will not be to serialize it over the wcf channel and serialize it in db.
            if (valueType == typeof(KeyValuePair<string, string>[]))
            {
                this.ValueType = "System.Collections.Generic.KeyValuePair`2[[System.String],[System.String]][]";
            }
            else if (valueType == typeof(string)
                || valueType == typeof(Uri)
                || valueType == typeof(string[])
                || valueType.AssemblyQualifiedName.Contains("System.Private")
                || valueType.AssemblyQualifiedName.Contains("mscorlib"))
            {
                // This comparison is a check to ensure assembly information is not embedded in data.
                // Use type.FullName instead of type.AssemblyQualifiedName since the internal assemblies
                // are different in desktop and coreclr. Thus AQN in coreclr includes System.Private.CoreLib which
                // is not available on the desktop.
                // Note that this doesn't handle generic types. Such types will fail during serialization.
                this.ValueType = valueType.FullName;
            }
            else if (valueType.GetTypeInfo().IsValueType)
            {
                // In case of custom types, let the assembly qualified name be available to help
                // deserialization on the client.
                this.ValueType = valueType.AssemblyQualifiedName;
            }
            else
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Resources.UnexpectedTypeOfProperty, valueType, id));
            }

            this.Id = id;
            this.Label = label;
            this.Category = category;
            this.Description = description;
            this.ValidateValueCallback = validateValueCallback;
            this.Attributes = attributes;
            this.valueType = valueType;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Gets or sets the Id for the property.
        /// </summary>
        [DataMember]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets a label for the property.
        /// </summary>
        [DataMember]
        public string Label { get; set; }

        /// <summary>
        /// Gets or sets a category for the property.
        /// </summary>
        [DataMember]
        public string Category { get; set; }

        /// <summary>
        /// Gets or sets a description for the property.
        /// </summary>
        [DataMember]
        public string Description { get; set; }

        /// <summary>
        /// Gets the callback for validation of property value.
        /// </summary>
        /// <remarks>This property is not required at the client side.</remarks>
        [IgnoreDataMember]
        public ValidateValueCallback ValidateValueCallback { get; }

        /// <summary>
        /// Gets or sets the attributes for this property.
        /// </summary>
        [DataMember]
        public TestPropertyAttributes Attributes { get; set; }

        /// <summary>
        /// Gets or sets a string representation of the type for value.
        /// </summary>
        [DataMember]
        public string ValueType { get; set; }

        #endregion Properties

        #region IEquatable

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return base.Equals(obj as TestProperty);
        }

        /// <inheritdoc/>
        public bool Equals(TestProperty other)
        {
            return (other != null) && (this.Id == other.Id);
        }

        #endregion IEquatable

        #region Methods

        /// <inheritdoc/>
        public override string ToString()
        {
            return this.Id;
        }

        /// <summary>
        /// Gets the valueType. 
        /// </summary>
        /// <remarks>Only works for the valueType that is in the currently executing assembly or in Mscorlib.dll. The default valueType is of string valueType.</remarks>
        /// <returns>The valueType of the test property</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This could take a bit time, is not simple enough to be a property.")]
        public Type GetValueType()
        {
            if (this.valueType == null)
            {
                this.valueType = this.GetType(this.ValueType);
            }

            return this.valueType;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Use this in the body in debug mode")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We use the default type whenever exception thrown")]
        private Type GetType(string typeName)
        {
            ValidateArg.NotNull(typeName, "typeName");

            Type type = null;

            try
            {
                // This only works for the type is in the currently executing assembly or in Mscorlib.dll.
                type = Type.GetType(typeName);

                if (type == null)
                {
                    type = Type.GetType(typeName.Replace("Version=4.0.0.0", "Version=2.0.0.0")); // Try 2.0 version as discovery returns version of 4.0 for all cases
                }

                // For UAP the type namespace for System.Uri,System.TimeSpan and System.DateTimeOffset differs from the desktop version.
                if (type == null && typeName.StartsWith("System.Uri"))
                {
                    type = typeof(System.Uri);
                }
                else if (type == null && typeName.StartsWith("System.TimeSpan"))
                {
                    type = typeof(System.TimeSpan);
                }
                else if (type == null && typeName.StartsWith("System.DateTimeOffset"))
                {
                    type = typeof(System.DateTimeOffset);
                }
                else if (type == null && typeName.StartsWith("System.Int16"))
                {
                    // For LineNumber property - Int is required
                    type = typeof(System.Int16);
                }
                else if (type == null && typeName.StartsWith("System.Int32"))
                {
                    type = typeof(System.Int32);
                }
                else if (type == null && typeName.StartsWith("System.Int64"))
                {
                    type = typeof(System.Int64);
                }
            }
            catch (Exception)
            {
#if FullCLR
                // Try to see if the typeName contains Windows Phone PKT in that case load it from
                // desktop side
                if (typeName.Contains(s_windowsPhonePKT))
                {
                    type = this.GetType(typeName.Replace(s_windowsPhonePKT, s_visualStudioPKT));
                }

                if (type == null)
                {
                    System.Diagnostics.Debug.Fail("The test property type " + typeName + " of property " + this.id + "is not supported.");
#else
                System.Diagnostics.Debug.WriteLine("The test property type " + typeName + " of property " + this.Id + "is not supported.");
#endif
#if FullCLR
                }
#endif
            }
            finally
            {
                // default is of string type.
                if (type == null)
                {
                    type = typeof(string);
                }
            }

            return type;
        }

        #endregion Methods

        #region Static Fields

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA908:AvoidTypesThatRequireJitCompilationInPrecompiledAssemblies")]
        private static Dictionary<string, KeyValuePair<TestProperty, HashSet<Type>>> s_properties = new Dictionary<string, KeyValuePair<TestProperty, HashSet<Type>>>();

#if FullCLR
        private static string s_visualStudioPKT = "b03f5f7f11d50a3a";
        private static string s_windowsPhonePKT = "7cec85d7bea7798e";
#endif

        #endregion Static Fields

        #region Static Methods

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA908:AvoidTypesThatRequireJitCompilationInPrecompiledAssemblies")]
        public static void ClearRegisteredProperties()
        {
            lock (s_properties)
            {
                s_properties.Clear();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA908:AvoidTypesThatRequireJitCompilationInPrecompiledAssemblies")]
        public static TestProperty Find(string id)
        {
            ValidateArg.NotNull(id, "id");

            TestProperty result = null;

            KeyValuePair<TestProperty, HashSet<Type>> propertyTypePair;
            lock (s_properties)
            {
                if (s_properties.TryGetValue(id, out propertyTypePair))
                {
                    result = propertyTypePair.Key;
                }
            }

            return result;
        }

        public static TestProperty Register(string id, string label, Type valueType, Type owner)
        {
            ValidateArg.NotNullOrEmpty(id, "id");
            ValidateArg.NotNull(label, "label");
            ValidateArg.NotNull(valueType, "valueType");
            ValidateArg.NotNull(owner, "owner");

            return Register(id, label, string.Empty, string.Empty, valueType, null, TestPropertyAttributes.None, owner);
        }

        public static TestProperty Register(string id, string label, Type valueType, TestPropertyAttributes attributes, Type owner)
        {
            ValidateArg.NotNullOrEmpty(id, "id");
            ValidateArg.NotNull(label, "label");
            ValidateArg.NotNull(valueType, "valueType");
            ValidateArg.NotNull(owner, "owner");

            return Register(id, label, string.Empty, string.Empty, valueType, null, attributes, owner);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA908:AvoidTypesThatRequireJitCompilationInPrecompiledAssemblies")]
        public static TestProperty Register(string id, string label, string category, string description, Type valueType, ValidateValueCallback validateValueCallback, TestPropertyAttributes attributes, Type owner)
        {
            ValidateArg.NotNullOrEmpty(id, "id");
            ValidateArg.NotNull(label, "label");
            ValidateArg.NotNull(category, "category");
            ValidateArg.NotNull(description, "description");
            ValidateArg.NotNull(valueType, "valueType");
            ValidateArg.NotNull(owner, "owner");

            TestProperty result;

            KeyValuePair<TestProperty, HashSet<Type>> propertyTypePair;

            lock (s_properties)
            {
                if (s_properties.TryGetValue(id, out propertyTypePair))
                {
                    // verify the data valueType is valid
                    if (propertyTypePair.Key.ValueType == valueType.AssemblyQualifiedName
                        || propertyTypePair.Key.ValueType == valueType.FullName
                        || propertyTypePair.Key.valueType == valueType)
                    {
                        // add the owner to set of owners for this GraphProperty object
                        propertyTypePair.Value.Add(owner);
                        result = propertyTypePair.Key;
                    }
                    else
                    {
                        // not the data valueType we expect, throw an exception
                        string message = string.Format(
                            CultureInfo.CurrentCulture,
                            Resources.Resources.Exception_RegisteredTestPropertyHasDifferentValueType,
                            id,
                            valueType.ToString(),
                            propertyTypePair.Key.ValueType);

                        throw new InvalidOperationException(message);
                    }
                }
                else
                {
                    // create a new TestProperty object
                    result = new TestProperty(id, label, category, description, valueType, validateValueCallback, attributes);

                    // setup the data pair used to track owners of this GraphProperty
                    propertyTypePair = new KeyValuePair<TestProperty, HashSet<Type>>(result, new HashSet<Type>());
                    propertyTypePair.Value.Add(owner);

                    // add to the dictionary
                    s_properties[id] = propertyTypePair;
                }
            }

            return result;
        }

        public static bool TryUnregister(string id, out KeyValuePair<TestProperty, HashSet<Type>> propertyTypePair)
        {
            ValidateArg.NotNullOrEmpty(id, "id");

            lock (s_properties)
            {
                if (s_properties.TryGetValue(id, out propertyTypePair))
                {
                    return s_properties.Remove(id);
                }
            }
            return false;
        }

        #endregion Static Methods

        public object GetRealObject(StreamingContext context)
        {
            var registeredProperty = TestProperty.Find(this.Id);
            if (registeredProperty == null)
            {
                registeredProperty = TestProperty.Register(
                    this.Id,
                    this.Label,
                    this.Category,
                    this.Description,
                    this.GetValueType(),
                    this.ValidateValueCallback,
                    this.Attributes,
                    typeof(TestObject));
            }

            return registeredProperty;
        }
    }
}
