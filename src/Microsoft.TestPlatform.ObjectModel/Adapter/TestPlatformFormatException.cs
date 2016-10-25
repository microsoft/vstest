// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter
{
    using System;
    using System.Runtime.Serialization;
#if NET46
using System.Security.Permissions;
#endif
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    /// <summary>
    /// Exception thrown on parsing error in user provided filter expression.
    /// This can happen when filter has invalid format or has unsupported properties.
    /// </summary>
#if NET46
    [Serializable]
#endif
    public class TestPlatformFormatException : Exception
    {
        #region Constructors

        /// <summary>
        /// Creates a new TestPlatformFormatException
        /// </summary>
        public TestPlatformFormatException()
            : base()
        {
        }

        /// <summary>
        /// Initializes with the message.
        /// </summary>
        /// <param name="message">Message for the exception.</param>
        public TestPlatformFormatException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes with the message and filter string.
        /// </summary>
        /// <param name="message">Message for the exception.</param>
        /// <param name="filterValue">Filter expression.</param>
        public TestPlatformFormatException(string message, string filterValue)
            : base(message)
        {
            FilterValue = filterValue;
        }

        /// <summary>
        /// Initializes with message and inner exception.
        /// </summary>
        /// <param name="message">Message for the exception.</param>
        /// <param name="innerException">The inner exception.</param>
        public TestPlatformFormatException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

#if NET46
        /// <summary>
        /// Seralization constructor.
        /// </summary>
        protected TestPlatformFormatException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            ValidateArg.NotNull(info, "info");
            // Save the basic properties.
            this.FilterValue = info.GetString("FilterValue");
        }

#endif
        #endregion

        /// <summary>
        /// Filter expression.
        /// </summary>
        public string FilterValue
        {
            get;
            private set;
        }

#if NET46
        /// <summary>
        /// Serialization helper.
        /// </summary>
        /// <param name="info">Serialization info to add to</param>
        /// <param name="context">not used</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            base.GetObjectData(info, context);
            info.AddValue("FilterValue", this.FilterValue);
        }
#endif
    }
}
