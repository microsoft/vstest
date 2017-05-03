// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;

    /// <summary>
    /// The DataCollectionLauncher interface.
    /// </summary>
    internal interface IDataCollectionLauncher
    {
        #region events
        /// <summary>
        /// Raised when data collector is launched successfully
        /// </summary>
        event EventHandler<HostProviderEventArgs> DataCollectorLaunched;

        /// <summary>
        /// Raised when data collector is reports Error
        /// </summary>
        event EventHandler<HostProviderEventArgs> DataCollectorExited;

        #endregion

        /// <summary>
        /// Gets the data collector process info.
        /// </summary>
        Process DataCollectorProcess
        {
            get;
        }

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
    }
}