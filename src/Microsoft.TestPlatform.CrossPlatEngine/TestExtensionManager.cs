// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine;

using System.Collections.Generic;

using Common.ExtensionFramework;

using ObjectModel.Engine;

/// <summary>
/// Orchestrates extensions for this engine.
/// </summary>
public class TestExtensionManager : ITestExtensionManager
{
    /// <inheritdoc />
    public void ClearExtensions()
    {
        TestPluginCache.Instance.ClearExtensions();
    }

    /// <inheritdoc />
    public void UseAdditionalExtensions(IEnumerable<string> pathToAdditionalExtensions, bool skipExtensionFilters)
    {
        TestPluginCache.Instance.UpdateExtensions(pathToAdditionalExtensions, skipExtensionFilters);
    }
}