// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.TestUtilities
{
    using System;
    /// <summary>
    /// Assembly utility to perform assembly related functions.
    /// </summary>
    public class FileUtility
    {
        /// <summary>
        /// Gets the base directory for application.
        /// </summary>
        public static string GetAppDomainBaseDir()
        {
#if NET451
        return AppDomain.CurrentDomain.BaseDirectory;
#else
            return AppContext.BaseDirectory;
#endif
        }
    }
}