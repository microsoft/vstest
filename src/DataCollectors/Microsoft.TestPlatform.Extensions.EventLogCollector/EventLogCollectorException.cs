// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.EventLogCollector
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Private Exception class used for event log exceptions
    /// </summary>
    [Serializable]
    internal class EventLogCollectorException : Exception
    {
        /// <summary>
        /// Constructs a new EventLogCollectorException
        /// </summary>
        /// <param name="localizedMessage">the localized exception message</param>
        /// <param name="innerException">the inner exception</param>
        public EventLogCollectorException(string localizedMessage, Exception innerException)
            : base(localizedMessage, innerException)
        {
        }

        protected EventLogCollectorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
