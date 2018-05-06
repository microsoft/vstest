// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Coverage.Interfaces
{
    /// <summary>
    ///  Commands for vanguard process.
    /// </summary>
    public enum VangurdCommand
    {
        /// <summary>
        /// Start the collecting code coverage for given session.
        /// </summary>
        Collect,

        /// <summary>
        /// Stop the collecting code coverage for given session.
        /// </summary>
        Shutdown
    }
}