// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollector.Interfaces
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Loads datacollector.
    /// </summary>
    internal interface IDataCollectorLoader
    {
        /// <summary>
        /// Creates instance of data collector.
        /// </summary>
        /// <param name="type">
        /// Data collector type.
        /// </param>
        /// <returns>
        /// The <see cref="DataCollector"/>.
        /// </returns>
        DataCollector CreateInstance(Type type);

        /// <summary>
        /// Finds DataCollectors in a given assembly.
        /// </summary>
        /// <param name="assemblyLocation">
        /// Location of data collector assembly.
        /// </param>
        /// <returns>List of data collector friendly name and type.</returns>
        IEnumerable<Tuple<string, Type>> FindDataCollectors(string assemblyLocation);

        /// <summary>
        /// Gets the Type Uri for the data collector.
        /// </summary>
        /// <param name="dataCollectorType">The data collector to get the Type URI for.</param>
        /// <returns>Type Uri of the data collector.</returns>
        Uri GetTypeUri(Type dataCollectorType);

        /// <summary>
        /// Gets the friendly name for the data collector.
        /// </summary>
        /// <param name="dataCollectorType">The data collector to get the Type URI for.</param>
        /// <returns>Friendly name of the data collector.</returns>
        string GetFriendlyName(Type dataCollectorType);
    }
}
