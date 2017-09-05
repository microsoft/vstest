// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

    /// <summary>
    /// Orchestrates extensions for this engine.
    /// </summary>
    public interface ITestExtensionManager
    {
        /// <summary>
        /// Update the extensions data
        /// </summary>
        void UseAdditionalExtensions(IEnumerable<string> pathToAdditionalExtensions, bool loadOnlyWellKnownExtensions);

        /// <summary>
        /// Clear the extensions data
        /// </summary>
        void ClearExtensions();
    }
}
