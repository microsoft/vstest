// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Text;

    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

    /// <summary>
    /// The datacollection launcher.
    /// This works for Desktop local scenarios
    /// </summary>
    internal class DefaultDataCollectionLauncher : DataCollectionLauncher
    {
        private const string DataCollectorProcessName = "datacollector.exe";

        /// <summary>
        /// The constructor.
        /// </summary>
        public DefaultDataCollectionLauncher()
            : this(new ProcessHelper())
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultDataCollectionLauncher"/> class. 
        /// </summary>
        /// <param name="processHelper">
        /// The process helper. 
        /// </param>
        internal DefaultDataCollectionLauncher(IProcessHelper processHelper) : base(processHelper, TestSessionMessageLogger.Instance)
        {
            this.processHelper = processHelper;
            this.DataCollectorProcess = null;
        }

        /// <summary>
        /// Gets the data collector process info.
        /// </summary>
        public override sealed Process DataCollectorProcess
        {
            get; protected set;
        }

        /// <summary>
        /// Launches the test host for discovery/execution.
        /// </summary>
        /// <param name="environmentVariables">Environment variables for the process.</param>
        /// <param name="commandLineArguments">The command line arguments to pass to the process.</param>
        /// <returns>ProcessId of launched Process. 0 means not launched.</returns>
        public override int LaunchDataCollector(IDictionary<string, string> environmentVariables, IList<string> commandLineArguments)
        {
            this.dcExitedEventRaised = false;
            this.testHostProcessStdError = new StringBuilder(this.ErrorLength, this.ErrorLength);

            string dataCollectorProcessPath = null, processWorkingDirectory = null;
            var currentWorkingDirectory = Path.GetDirectoryName(typeof(DefaultDataCollectionLauncher).GetTypeInfo().Assembly.Location);

            var currentProcessPath = this.processHelper.GetCurrentProcessFileName();

            // If current process is dotnet/dotnet.exe and you are here, datacollector.exe is present in TestHost folder.
            if (currentProcessPath.EndsWith("dotnet", StringComparison.OrdinalIgnoreCase)
                || currentProcessPath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase))
            {
                dataCollectorProcessPath = Path.Combine(currentWorkingDirectory, "TestHost", DataCollectorProcessName);
            }
            else
            {
                dataCollectorProcessPath = Path.Combine(currentWorkingDirectory, DataCollectorProcessName);
            }

            processWorkingDirectory = Directory.GetCurrentDirectory();

            var argumentsString = string.Join(" ", commandLineArguments);

            this.DataCollectorProcess = this.processHelper.LaunchProcess(dataCollectorProcessPath, argumentsString, processWorkingDirectory, environmentVariables, this.ErrorReceivedCallback, this.ExitCallBack) as Process;
            return this.DataCollectorProcess?.Id ?? 0;
        }
    }
}