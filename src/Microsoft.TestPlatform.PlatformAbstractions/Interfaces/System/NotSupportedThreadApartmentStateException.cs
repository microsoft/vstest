// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions
{
    using System;

    /// <summary>
    /// Not supported thread aprtment state exception.
    /// </summary>
    public class NotSupportedThreadApartmentStateException : Exception
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NotSupportedThreadApartmentStateException"/> class.
        /// Initializes with the message.
        /// </summary>
        /// <param name="message">Message for the exception.</param>
        public NotSupportedThreadApartmentStateException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotSupportedThreadApartmentStateException"/> class.
        /// Initializes with the message.
        /// </summary>
        /// <param name="message">Message for the exception.</param>
        public NotSupportedThreadApartmentStateException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotSupportedThreadApartmentStateException"/> class.
        /// Initializes with message and inner exception.
        /// </summary>
        /// <param name="message">Message for the exception.</param>
        /// <param name="innerException">The inner exception.</param>
        public NotSupportedThreadApartmentStateException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        #endregion

    }
}
