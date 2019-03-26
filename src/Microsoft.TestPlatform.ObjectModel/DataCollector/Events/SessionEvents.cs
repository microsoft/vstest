// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.Serialization;

    /// <summary>
    /// Session Start event arguments
    /// </summary>
    [DataContract]
    public sealed class SessionStartEventArgs : DataCollectionEventArgs
    {
        private IDictionary<string, object> Properties;

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionStartEventArgs"/> class. 
        /// </summary>
        /// <remarks>
        /// Default constructor with empty properties and default DataCollectionContext.
        /// DataCollectionContext with empty session signifies that is it irrelevent in the current context.
        /// </remarks>
        public SessionStartEventArgs() : this(new DataCollectionContext(new SessionId(Guid.Empty)), new Dictionary<string, object>())
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionStartEventArgs"/> class. 
        /// </summary>
        /// <remarks>
        /// constructor with properties and default DataCollectionContext.
        /// DataCollectionContext with empty session signifies that is it irrelevent in the current context.
        /// </remarks>
        public SessionStartEventArgs(IDictionary<string, object> properties) : this(new DataCollectionContext(new SessionId(Guid.Empty)), properties)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionStartEventArgs"/> class. 
        /// </summary>
        /// <param name="context">
        /// Context information for the session
        /// </param>
        public SessionStartEventArgs(DataCollectionContext context, IDictionary<string, object> properties)
            : base(context)
        {
            this.Properties = properties;
            Debug.Assert(!context.HasTestCase, "Session event has test a case context");
        }

        #endregion

        #region Public Methods

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
            ValidateArg.NotNullOrEmpty(property, "property");

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
            ValidateArg.NotNullOrEmpty(property, "property");

            this.Properties.TryGetValue(property, out object propertyValue);

            return propertyValue;
        }

        #endregion
    }

    /// <summary>
    /// Session End event arguments
    /// </summary>
    [DataContract]
    public sealed class SessionEndEventArgs : DataCollectionEventArgs
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionEndEventArgs"/> class. 
        /// </summary>
        /// <remarks>
        /// Default constructor with default DataCollectionContext.
        /// DataCollectionContext with empty session signifies that is it irrelevent in the current context.
        /// </remarks>
        public SessionEndEventArgs() : this(new DataCollectionContext(new SessionId(Guid.Empty)))
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionEndEventArgs"/> class. 
        /// </summary>
        /// <param name="context">
        /// Context information for the session
        /// </param>
        public SessionEndEventArgs(DataCollectionContext context)
            : base(context)
        {
            Debug.Assert(!context.HasTestCase, "Session event has test a case context");
        }

        #endregion
    }
}
