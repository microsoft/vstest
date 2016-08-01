// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    /// <summary>
    /// The DataCollectionLauncher interface.
    /// </summary>
    internal interface IDataCollectionLauncher
    {
        /// <summary>
        /// The launch data collector.
        /// </summary>
        /// <param name="environmentVariables">
        /// The environment variables.
        /// </param>
        /// <param name="commandLineArguments">
        /// The command line arguments.
        /// </param>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        int LaunchDataCollector(IDictionary<string, string> environmentVariables, IList<string> commandLineArguments);

        /// <summary>
        /// The initialize.
        /// </summary>
        /// <param name="architecture">
        /// The architecture.
        /// </param>
        void Initialize(Architecture architecture);
    }
}