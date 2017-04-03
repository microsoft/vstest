// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

    /// <summary>
    /// Factory for creating DataCollectionLauncher
    /// </summary>
    internal static class DataCollectionLauncherFactory
    {
        /// <summary>
        /// The get data collector launcher.
        /// </summary>
        /// <returns>
        /// The <see cref="IDataCollectionLauncher"/>.
        /// </returns>
        internal static IDataCollectionLauncher GetDataCollectorLauncher(IProcessHelper processHelper)
        {
            // Target Framework of DataCollection process and Runner should be same.
            var currentProcessPath = processHelper.GetCurrentProcessFileName();

            if (currentProcessPath.EndsWith("dotnet", StringComparison.OrdinalIgnoreCase)
                 || currentProcessPath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase))
            {
                return new DotnetDataCollectionLauncher();
            }

            return new DefaultDataCollectionLauncher();
        }
    }
}
