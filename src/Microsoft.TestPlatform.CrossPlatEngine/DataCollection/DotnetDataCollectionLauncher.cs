// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection
{
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Extensions;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    /// <summary>
    /// The datacollection launcher.
    /// This works for Desktop local scenarios
    /// </summary>
    internal class DotnetDataCollectionLauncher : DataCollectionLauncher
    {
        private const string DataCollectorProcessName = "datacollector.dll";

        private IFileHelper fileHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="DotnetDataCollectionLauncher"/> class. 
        /// </summary>
        public DotnetDataCollectionLauncher()
            : this(new ProcessHelper(), new FileHelper(), TestSessionMessageLogger.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DotnetDataCollectionLauncher"/> class.
        /// </summary>
        /// <param name="processHelper">
        /// The process helper. 
        /// </param>
        /// <param name="fileHelper">
        /// The file Helper.
        /// </param>
        /// <param name="messageLogger">
        /// The message Logger.
        /// </param>
        internal DotnetDataCollectionLauncher(IProcessHelper processHelper, IFileHelper fileHelper, IMessageLogger messageLogger) : base(processHelper, messageLogger)
        {
            this.processHelper = processHelper;
            this.fileHelper = fileHelper;
            this.DataCollectorProcessId = -1;
        }

        /// <summary>
        /// Launches the test host for discovery/execution.
        /// </summary>
        /// <param name="environmentVariables">Environment variables for the process.</param>
        /// <param name="commandLineArguments">The command line arguments to pass to the process.</param>
        /// <returns>ProcessId of launched Process. 0 means not launched.</returns>
        public override int LaunchDataCollector(IDictionary<string, string> environmentVariables, IList<string> commandLineArguments)
        {
            string dataCollectorFileName = null;
            var currentWorkingDirectory = Path.GetDirectoryName(typeof(DefaultDataCollectionLauncher).GetTypeInfo().Assembly.GetAssemblyLocation());
            var currentProcessFileName = this.processHelper.GetCurrentProcessFileName();

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("DotnetDataCollectionLauncher: Full path of dotnet.exe is {0}", currentProcessFileName);
            }

            var dataCollectorAssemblyPath = Path.Combine(currentWorkingDirectory, DataCollectorProcessName);

            dataCollectorFileName = Path.GetFileNameWithoutExtension(dataCollectorAssemblyPath);

            var args = "exec";

            // Probe for runtimeconfig and deps file for the test source
            var runtimeConfigPath = Path.Combine(currentWorkingDirectory, string.Concat(dataCollectorFileName, ".runtimeconfig.json"));

            if (this.fileHelper.Exists(runtimeConfigPath))
            {
                var argsToAdd = " --runtimeconfig " + runtimeConfigPath.AddDoubleQuote();
                args += argsToAdd;
                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("DotnetDataCollectionLauncher: Adding {0} in args", argsToAdd);
                }
            }
            else
            {
                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("DotnetDataCollectionLauncher: File {0}, doesnot exist", runtimeConfigPath);
                }
            }

            // Use the deps.json for test source
            var depsFilePath = Path.Combine(currentWorkingDirectory, string.Concat(dataCollectorFileName, ".deps.json"));
            if (this.fileHelper.Exists(depsFilePath))
            {
                var argsToAdd = " --depsfile " + depsFilePath.AddDoubleQuote();
                args += argsToAdd;
                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("DotnetDataCollectionLauncher: Adding {0} in args", argsToAdd);
                }
            }
            else
            {
                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("DotnetDataCollectionLauncher: File {0}, doesnot exist", depsFilePath);
                }
            }

            var cliArgs = string.Join(" ", commandLineArguments);
            var argumentsString = string.Format("{0} \"{1}\" {2} ", args, dataCollectorAssemblyPath, cliArgs);

            var dataCollectorProcess = this.processHelper.LaunchProcess(currentProcessFileName, argumentsString, currentWorkingDirectory, environmentVariables, this.ErrorReceivedCallback, this.ExitCallBack);
            this.DataCollectorProcessId = this.processHelper.GetProcessId(dataCollectorProcess);
            return this.DataCollectorProcessId;
        }
    }
}