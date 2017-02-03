// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;

    /// <summary>
    /// Factory for creating DataCollectionLauncher
    /// </summary>
    internal static class DataCollectionLauncherFactory
    {
        /// <summary>
        /// The get data collector launcher.
        /// </summary>
        /// <param name="frameworkVersion">
        /// .NET framework version.
        /// </param>
        /// <returns>
        /// The <see cref="IDataCollectionLauncher"/>.
        /// </returns>
        internal static IDataCollectionLauncher GetDataCollectorLauncher(string frameworkVersion)
        {
            if (frameworkVersion.IndexOf("netstandard", StringComparison.OrdinalIgnoreCase) >= 0
                || frameworkVersion.IndexOf("netcoreapp", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new DotnetDataCollectionLauncher();
            }

            return new DefaultDataCollectionLauncher();
        }
    }
}
