// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector.Interfaces
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    /// <summary>
    /// Loads datacollector.
    /// </summary>
    internal interface IDataCollectorLoader
    {
        /// <summary>
        /// Loads datacollector from specified assembly using assembly qualified name. 
        /// </summary>
        /// <param name="location">
        /// Location of datacollector assembly.
        /// </param>
        /// <param name="assemblyQualifiedName">
        /// The assembly qualified name of datacollector.
        /// </param>
        /// <returns>
        /// The <see cref="DataCollector"/>.
        /// </returns>
        DataCollector Load(string location, string assemblyQualifiedName);
    }
}
