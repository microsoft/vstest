// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Hosting;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;

    /// <summary>
    /// Orchestrates extensions for this engine.
    /// </summary>
    public class TestExtensionManager : ITestExtensionManager
    {
        public void UseAdditionalExtensions(IEnumerable<string> pathToAdditionalExtensions, bool loadOnlyWellKnownExtensions)
        {
            TestPluginCache.Instance.UpdateAdditionalExtensions(pathToAdditionalExtensions, loadOnlyWellKnownExtensions);
        }
    }
}
