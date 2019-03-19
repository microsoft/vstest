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
        /// Default constructor with default DataCollectionContext.
        /// DataCollectionContext with empty session signifies that is it irrelevent in the current context.
        /// </remarks>
        public SessionStartEventArgs() : this(new DataCollectionContext(new SessionId(Guid.Empty)))
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionStartEventArgs"/> class. 
        /// </summary>
        /// <param name="context">
        /// Context information for the session
        /// </param>
        public SessionStartEventArgs(DataCollectionContext context)
            : base(context)
        {
            this.Properties = new Dictionary<string, object>();
            Debug.Assert(!context.HasTestCase, "Session event has test a case context");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns the Session Start Properties currently specified.
        /// </summary>
        public IEnumerator<KeyValuePair<string, object>> GetProperties()
        {
            return this.Properties.GetEnumerator();
        }

        /// <summary>
        /// Returns the property value
        /// </summary>
        /// <param name="value">
        /// Value of the property
        /// </param>
        /// <param name="property">
        /// Name of the property
        /// </param>
        public T GetPropertyValue<T>(string property)
        {
            ValidateArg.NotNullOrEmpty(property, "property");

            T value;
            if (this.Properties.TryGetValue(property, out object propertyValue) && propertyValue != null)
            {
                value = (T)propertyValue;
            }
            else
            {
                value = default(T);
            }

            return value;
        }

        /// <summary>
        /// Returns the property value
        /// </summary>
        /// <param name="value">
        /// Value of the property
        /// </param>
        /// <param name="property">
        /// Name of the property
        /// </param>
        public object GetPropertyValue(string property)
        {
            ValidateArg.NotNullOrEmpty(property, "property");

            this.Properties.TryGetValue(property, out object propertyValue);

            return propertyValue;
        }

        /// <summary>
        /// Sets the property value
        /// </summary>
        /// <param name="property">
        /// Name of the property
        /// </param>
        /// <param name="value">
        /// Value of the property
        /// </param>
        internal void SetPropertyValue(string property, object value)
        {
            ValidateArg.NotNull(property, "property");

            if (value != null)
            {
                this.Properties.Add(property, value);
            }
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
