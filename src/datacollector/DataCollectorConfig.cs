// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollector;

    /// <summary>
    /// The data collector config.
    /// </summary>
    public class DataCollectorConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectorConfig"/> class.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        public DataCollectorConfig(Type type)
        {
            ValidateArg.NotNull(type, nameof(type));

            this.DataCollectorType = type;
            this.TypeUri = GetTypeUri(type);
            this.FriendlyName = GetFriendlyName(type);
        }

        /// <summary>
        /// Gets the data collector type.
        /// </summary>
        public Type DataCollectorType { get; private set; }

        /// <summary>
        /// Gets the type uri.
        /// </summary>
        public Uri TypeUri { get; private set; }

        /// <summary>
        /// Gets the friendly name.
        /// </summary>
        public string FriendlyName { get; private set; }

        /// <summary>
        /// Gets the Type Uri for the data collector.
        /// </summary>
        /// <param name="dataCollectorType">The data collector to get the Type URI for.</param>
        /// <returns>Type Uri of the data collector.</returns>
        private static Uri GetTypeUri(Type dataCollectorType)
        {
            var typeUri = default(Uri);
            var typeUriAttributes = GetAttributes(dataCollectorType, typeof(DataCollectorTypeUriAttribute), true);

            var typeUriAttribute = (DataCollectorTypeUriAttribute)typeUriAttributes[0];

            // The type uri can not be null or empty.
            if (string.IsNullOrEmpty(typeUriAttribute.TypeUri))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Resources.DataCollector_TypeIsNull, dataCollectorType.FullName));
            }

            // Ensure the format of the URI is valid.
            try
            {
                typeUri = new Uri(typeUriAttribute.TypeUri);
            }
            catch (UriFormatException e)
            {
                // The type uri is not a valid URI.
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Resources.DataCollectorTypeUriFormatInvalid, typeUriAttribute.TypeUri, dataCollectorType.FullName), e);
            }

            return typeUri;
        }

        /// <summary>
        /// Gets the friendly name for the data collector.
        /// </summary>
        /// <param name="dataCollectorType">The data collector to get the Type URI for.</param>
        /// <returns>Friendly name of the data collector.</returns>
        private static string GetFriendlyName(Type dataCollectorType)
        {
            Debug.Assert(dataCollectorType != null, "null dataCollectorType");

            // Get the friendly name from the attribute.
            var friendlyNameAttributes = GetAttributes(
                dataCollectorType,
                typeof(DataCollectorFriendlyNameAttribute),
                true);

            var friendlyNameAttribute = (DataCollectorFriendlyNameAttribute)friendlyNameAttributes[0];

            // Verify that the friendly name provided is not null or empty.
            if (string.IsNullOrEmpty(friendlyNameAttribute.FriendlyName))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Resources.FriendlyNameIsNullOrEmpty, dataCollectorType.FullName));
            }

            return friendlyNameAttribute.FriendlyName;
        }

        /// <summary>
        /// Gets the attributes of the specified type from the data collector type.
        /// </summary>
        /// <param name="dataCollectorType">Data collector type to get attribute from.</param>
        /// <param name="attributeType">The type of attribute to look for.</param>
        /// <param name="isRequired">Indicates if the attribute is required.</param>
        /// <returns>Array of attributes matching the type provided.  Will be an empty array if none were found.</returns>
        private static object[] GetAttributes(Type dataCollectorType, Type attributeType, bool isRequired)
        {
            Debug.Assert(dataCollectorType != null, "null dataCollectorType");
            Debug.Assert(attributeType != null, "null attributeType");

            var attributes = default(object[]);

            // If any attribute constructor on the type throws, the exception will bubble up through
            // the "GetCustomAttributes" method.
            try
            {
                attributes = dataCollectorType.GetTypeInfo().GetCustomAttributes(attributeType, false).ToArray<object>();
            }
            catch (Exception e)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Resources.AttributeRetrievalError, dataCollectorType.AssemblyQualifiedName), e);
            }

            // If the attribute is required and was not found, then throw.
            if (isRequired && (attributes.Length < 1))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.Resources.DataCollectorRequiredAttributeMissing,
                        attributeType.FullName,
                        dataCollectorType.AssemblyQualifiedName));
            }

            return attributes;
        }
    }
}