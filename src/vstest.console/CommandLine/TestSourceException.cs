// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine
{
    using System;

    /// <summary>
    /// Exception thrown by argument processors when they encounter an error with test source
    /// arguments.
    /// </summary>
    public class TestSourceException : Exception
    {
        #region Constructors

        /// <summary>
        /// Creates a new TestSourceException
        /// </summary>
        public TestSourceException()
        {
        }

        /// <summary>
        /// Initializes with the message.
        /// </summary>
        /// <param name="message">Message for the exception.</param>
        public TestSourceException(string message)
            : base(message)
        {
        }

        #endregion
    }
}
