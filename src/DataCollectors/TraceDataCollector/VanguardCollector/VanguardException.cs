// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Coverage
{
    using System;

    /// <summary>
    /// VanguardException class
    /// </summary>
    internal class VanguardException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VanguardException"/> class.
        /// Constructor
        /// </summary>
        /// <param name="message">Error message</param>
        internal VanguardException(string message, bool isCritical = false)
            : base(message)
        {
            this.IsCritical = isCritical;
        }

        /// <summary>
        /// Gets a value indicating whether whether it's a critical exception. Critical exception cannot be caught and will stop the data collector from running.
        /// Non-critical exception will be caught and be logged as a warning message.
        /// </summary>
        public bool IsCritical { get; private set; }
    }
}