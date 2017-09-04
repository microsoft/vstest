// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Interfaces
{
    using System.Collections.Generic;

    public interface IVSExtensionManager
    {
        /// <summary>
        /// Get the unit test extensions installed via vsix.
        /// </summary>
        IEnumerable<string> GetUnitTestExtensions();
    }
}
