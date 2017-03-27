// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Helpers;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Helpers.Interfaces;

    /// <summary>
    /// The datacollection launcher.
    /// This works for Desktop local scenarios
    /// </summary>
    internal class DefaultDataCollectionLauncher : IDataCollectionLauncher
    {
        private const string DataCollectorProcessName = "datacollector.exe";
        private IProcessHelper processHelper;

        /// <summary>
        /// The constructor.
        /// </summary>
        public DefaultDataCollectionLauncher()
            : this(new ProcessHelper())
        {
        }

        /// <summary>
        /// Gets the data collector process info.
        /// </summary>
        public Process DataCollectorProcess
        {
            get; private set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DotnetDataCollectionLauncher"/> class.
        /// </summary>
        /// <param name="processHelper">
        /// The process helper. 
        /// </param>
        internal DefaultDataCollectionLauncher(IProcessHelper processHelper)
        {
            this.processHelper = processHelper;
            this.DataCollectorProcess = null;
        }

        /// <summary>
        /// Launches the test host for discovery/execution.
        /// </summary>
        /// <param name="environmentVariables">Environment variables for the process.</param>
        /// <param name="commandLineArguments">The command line arguments to pass to the process.</param>
        /// <returns>ProcessId of launched Process. 0 means not launched.</returns>
        public virtual int LaunchDataCollector(IDictionary<string, string> environmentVariables, IList<string> commandLineArguments)
        {
            string dataCollectorProcessPath = null, processWorkingDirectory = null;
            var currentWorkingDirectory = Path.GetDirectoryName(typeof(DefaultDataCollectionLauncher).GetTypeInfo().Assembly.Location);

            dataCollectorProcessPath = Path.Combine(currentWorkingDirectory, DataCollectorProcessName);
            processWorkingDirectory = Directory.GetCurrentDirectory();

            var argumentsString = string.Join(" ", commandLineArguments);

            this.DataCollectorProcess = this.processHelper.LaunchProcess(dataCollectorProcessPath, argumentsString, processWorkingDirectory, environmentVariables, null, null) as Process;
            return this.DataCollectorProcess != null? this.DataCollectorProcess.Id : 0;
        }
    }
}