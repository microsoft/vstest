// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollector.InProcDataCollector
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The test session start args.
    /// </summary>
    public class TestSessionStartArgs : InProcDataCollectionArgs
    {
        private IDictionary<string, object> Properties;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestSessionStartArgs"/> class.
        /// </summary>
        public TestSessionStartArgs()
        {
            this.Configuration = String.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestSessionStartArgs"/> class.
        /// </summary>
        /// <param name="properties">
        /// Properties.
        /// </param>
        public TestSessionStartArgs(IDictionary<string, object> properties)
        {
            this.Configuration = String.Empty;
            this.Properties = properties;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestSessionStartArgs"/> class.
        /// </summary>
        /// <param name="configuration">
        /// The configuration.
        /// </param>
        public TestSessionStartArgs(string configuration)
        {
            this.Configuration = configuration;
        }

        /// <summary>
        /// Gets or sets the configuration.
        /// </summary>
        public string Configuration { get; set; }

        /// <summary>
        /// Gets session start properties enumerator
        /// </summary>
        public IEnumerator<KeyValuePair<string, object>> GetProperties()
        {
            return this.Properties.GetEnumerator();
        }

        /// <summary>
        /// Gets property value
        /// </summary>
        /// <param name="property">
        /// Property name
        /// </param>
        public T GetPropertyValue<T>(string property)
        {
            ValidateArg.NotNullOrEmpty(property, nameof(property));

            return this.Properties.ContainsKey(property) ? (T)this.Properties[property] : default(T);
        }

        /// <summary>
        /// Gets property value
        /// </summary>
        /// <param name="property">
        /// Property name
        /// </param>
        public object GetPropertyValue(string property)
        {
            ValidateArg.NotNullOrEmpty(property, nameof(property));

            this.Properties.TryGetValue(property, out var propertyValue);

            return propertyValue;
        }
    }
}
