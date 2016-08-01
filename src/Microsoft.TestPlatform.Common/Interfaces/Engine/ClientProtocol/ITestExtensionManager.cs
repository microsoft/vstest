// Copyright (c) Microsoft. All rights reserved.

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
    }
}
