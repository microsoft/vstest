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
        internal VanguardException(string message)
            : base(message)
        {
        }
    }
}