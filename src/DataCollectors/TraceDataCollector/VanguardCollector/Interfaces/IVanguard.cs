// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Coverage.Interfaces
{
    using System;
    using TestPlatform.ObjectModel.DataCollection;
    using TraceCollector;

    /// <summary>
    /// The IVanguard interface.
    /// </summary>
    internal interface IVanguard : IDisposable
    {
        /// <summary>
        /// Initialize Vanguard.
        /// </summary>
        /// <param name="sessionName">Session name</param>
        /// <param name="configurationFileName">Configuration file name</param>
        /// <param name="logger">Data collection logger.</param>
        void Initialize(
            string sessionName,
            string configurationFileName,
            IDataCollectionLogger logger);

        /// <summary>
        /// Start a vanguard logger.
        /// </summary>
        /// <param name="outputName">Output file name</param>
        /// <param name="context">Data collection context. </param>
        void Start(string outputName, DataCollectionContext context);

        /// <summary>
        /// Stop vanguard logger.
        /// </summary>
        void Stop();
    }
}