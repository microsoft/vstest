// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    /// <summary>
    /// The datacollection launcher.
    /// This works for Desktop local scenarios
    /// </summary>
    internal class DataCollectionLauncher : IDataCollectionLauncher
    {
        private const string DataCollectorProcessName = "datacollector.exe";
        private const string DotnetProcessName = "dotnet.exe";
        private const string DotnetProcessNameXPlat = "dotnet";

        private string dataCollectorProcessName;
        private Process dataCollectorProcess;
        private IProcessHelper processHelper;
        
        /// <summary>
        /// The constructor.
        /// </summary>
        public DataCollectionLauncher()
            : this(new ProcessHelper())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionLauncher"/> class.
        /// </summary>
        /// <param name="processHelper">
        /// The process helper. 
        /// </param>
        internal DataCollectionLauncher(IProcessHelper processHelper)
        {
            this.processHelper = processHelper;
            this.dataCollectorProcess = null;
        }

        /// <summary>
        /// Initialize with desired architecture for the host
        /// </summary>
        /// <param name="architecture">architecture for the host</param>
        public void Initialize(Architecture architecture)
        {
            this.dataCollectorProcessName = DataCollectorProcessName;
        }

        /// <summary>
        /// Launches the test host for discovery/execution.
        /// </summary>
        /// <param name="environmentVariables">Environment variables for the process.</param>
        /// <param name="commandLineArguments">The command line arguments to pass to the process.</param>
        /// <returns>ProcessId of launched Process. 0 means not launched.</returns>
        public virtual int LaunchDataCollector(IDictionary<string, string> environmentVariables, IList<string> commandLineArguments)
        {
            var currentWorkingDirectory = Path.GetDirectoryName(typeof(DataCollectionLauncher).GetTypeInfo().Assembly.Location);
            string dataCollectorProcessPath, processWorkingDirectory = null;

            // TODO: DRY: Move this code to a common place
            // If we are running in the dotnet.exe context we do not want to launch dataCollector.exe but dotnet.exe with the dataCollector assembly. 
            // Since dotnet.exe is already built for multiple platforms this would avoid building dataCollector.exe also in multiple platforms.
            var currentProcessFileName = this.processHelper.GetCurrentProcessFileName();
            if (currentProcessFileName.EndsWith(DotnetProcessName) || currentProcessFileName.EndsWith(DotnetProcessNameXPlat))
            {
                dataCollectorProcessPath = currentProcessFileName;
                var dataCollectorAssemblyPath = Path.Combine(currentWorkingDirectory, this.dataCollectorProcessName.Replace("exe", "dll"));
                commandLineArguments.Insert(0, dataCollectorAssemblyPath);
                processWorkingDirectory = Path.GetDirectoryName(currentProcessFileName);
            }
            else
            {
                dataCollectorProcessPath = Path.Combine(currentWorkingDirectory, this.dataCollectorProcessName);
                // For IDEs and other scenario - Current directory should be the working directory - not the vstest.console.exe location
                // For VS - this becomes the solution directory for example
                // "TestResults" directory will be created at "current directory" of test host
                processWorkingDirectory = Directory.GetCurrentDirectory();
            }

            var argumentsString = string.Join(" ", commandLineArguments);

            this.dataCollectorProcess = this.processHelper.LaunchProcess(dataCollectorProcessPath, argumentsString, processWorkingDirectory, null);
            return this.dataCollectorProcess.Id;
        }

    }
}