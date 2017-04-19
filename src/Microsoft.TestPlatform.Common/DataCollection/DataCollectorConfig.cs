// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollector
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    /// <summary>
    /// The data collector config.
    /// </summary>
    internal class DataCollectorConfig : TestExtensionPluginInformation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectorConfig"/> class.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        public DataCollectorConfig(Type type)
            : base(type)
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

        /// <inheritdoc />
        public override string IdentifierData
        {
            get
            {
                return this.TypeUri?.ToString();
            }
        }

        /// <inheritdoc />
        public override ICollection<Object> Metadata
        {
            get
            {
                return new object[] { this.TypeUri.ToString(), this.FriendlyName };
            }
        }

        /// <summary>
        /// Gets the Type Uri for the data collector.
        /// </summary>
        /// <param name="dataCollectorType">The data collector to get the Type URI for.</param>
        /// <returns>Type Uri of the data collector.</returns>
        private static Uri GetTypeUri(Type dataCollectorType)
        {
            Uri typeUri = null;
            var typeUriAttributes = GetAttributes(dataCollectorType, typeof(DataCollectorTypeUriAttribute));
            if (typeUriAttributes != null && typeUriAttributes.Length > 0)
            {
                var typeUriAttribute = (DataCollectorTypeUriAttribute)typeUriAttributes[0];
                if (!string.IsNullOrWhiteSpace(typeUriAttribute.TypeUri))
                {
                    typeUri = new Uri(typeUriAttribute.TypeUri);
                }
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
            string friendlyName = string.Empty;

            // Get the friendly name from the attribute.
            var friendlyNameAttributes = GetAttributes(dataCollectorType, typeof(DataCollectorFriendlyNameAttribute));
            if (friendlyNameAttributes != null && friendlyNameAttributes.Length > 0)
            {
                var friendlyNameAttribute = (DataCollectorFriendlyNameAttribute)friendlyNameAttributes[0];
                if (!string.IsNullOrEmpty(friendlyNameAttribute.FriendlyName))
                {
                    friendlyName = friendlyNameAttribute.FriendlyName;
                }
            }

            return friendlyName;
        }
        
        /// <summary>
        /// Gets the attributes of the specified type from the data collector type.
        /// </summary>
        /// <param name="dataCollectorType">
        /// Data collector type to get attribute from.
        /// </param>
        /// <param name="attributeType">
        /// The type of attribute to look for.
        /// </param>
        /// <returns>
        /// Array of attributes matching the type provided.  Will be an empty array if none were found.
        /// </returns>
        private static object[] GetAttributes(Type dataCollectorType, Type attributeType)
        {
            Debug.Assert(dataCollectorType != null, "null dataCollectorType");
            Debug.Assert(attributeType != null, "null attributeType");

            // If any attribute constructor on the type throws, the exception will bubble up through
            // the "GetCustomAttributes" method.
            return dataCollectorType.GetTypeInfo().GetCustomAttributes(attributeType, true).ToArray<object>();
        }
    }
}
