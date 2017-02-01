// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Resources;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    /// <summary>
    /// The datacollection launcher.
    /// This works for Desktop local scenarios
    /// </summary>
    internal class DotnetDataCollectionLauncher : IDataCollectionLauncher
    {
        private const string DataCollectorProcessName = "datacollector.dll";
        private const string DotnetProcessName = "dotnet.exe";
        private const string DotnetProcessNameXPlat = "dotnet";

        private IProcessHelper processHelper;

        private IFileHelper fileHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="DotnetDataCollectionLauncher"/> class. 
        /// </summary>
        public DotnetDataCollectionLauncher()
            : this(new ProcessHelper(), new FileHelper())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DotnetDataCollectionLauncher"/> class.
        /// </summary>
        /// <param name="processHelper">
        /// The process helper. 
        /// </param>
        internal DotnetDataCollectionLauncher(IProcessHelper processHelper, IFileHelper fileHelper)
        {
            this.processHelper = processHelper;
            this.DataCollectorProcess = null;
            this.fileHelper = fileHelper;
        }

        /// <summary>
        /// Gets the data collector process.
        /// </summary>
        internal Process DataCollectorProcess
        {
            get; private set;
        }

        /// <summary>
        /// Launches the test host for discovery/execution.
        /// </summary>
        /// <param name="environmentVariables">Environment variables for the process.</param>
        /// <param name="commandLineArguments">The command line arguments to pass to the process.</param>
        /// <returns>ProcessId of launched Process. 0 means not launched.</returns>
        public virtual int LaunchDataCollector(IDictionary<string, string> environmentVariables, IList<string> commandLineArguments)
        {
            string dataCollectorFileName = null;
            var currentWorkingDirectory = Path.GetDirectoryName(typeof(DataCollectionLauncher).GetTypeInfo().Assembly.Location);
            var currentProcessPath = this.processHelper.GetCurrentProcessFileName();

            // TODO: DRY: Move this code to a common place
            // If we are running in the dotnet.exe context we do not want to launch dataCollector.exe but dotnet.exe with the dataCollector assembly. 
            // Since dotnet.exe is already built for multiple platforms this would avoid building dataCollector.exe also in multiple platforms.
            var currentProcessFileName = this.processHelper.GetCurrentProcessFileName();

            // This host manager can create process start info for dotnet core targets only.
            // If already running with the dotnet executable, use it; otherwise pick up the dotnet available on path.
            // Wrap the paths with quotes in case dotnet executable is installed on a path with whitespace.
            if (currentProcessPath.EndsWith(DotnetProcessNameXPlat, StringComparison.OrdinalIgnoreCase)
                || currentProcessPath.EndsWith(DotnetProcessName, StringComparison.OrdinalIgnoreCase))
            {
                currentProcessFileName = currentProcessPath;
            }
            else
            {
                currentProcessFileName = this.GetDotnetHostFullPath();
            }

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("DotnetDataCollectionLauncher: Full path of dotnet.exe is {0}", currentProcessFileName);
            }

            var dataCollectorAssemblyPath = Path.Combine(currentWorkingDirectory, DataCollectorProcessName);
            commandLineArguments.Insert(0, dataCollectorAssemblyPath);

            dataCollectorFileName = Path.GetFileNameWithoutExtension(dataCollectorAssemblyPath);

            // .NET core host manager is not a shared host. It will expect a single test source to be provided.
            var args = "exec";

            // Probe for runtimeconfig and deps file for the test source
            var runtimeConfigPath = Path.Combine(currentWorkingDirectory, string.Concat(dataCollectorFileName, ".runtimeconfig.json"));

            if (this.fileHelper.Exists(runtimeConfigPath))
            {
                var argsToAdd = " --runtimeconfig \"" + runtimeConfigPath + "\"";
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
                var argsToAdd = " --depsfile \"" + depsFilePath + "\"";
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

            commandLineArguments.Insert(0, args);

            var argumentsString = string.Join(" ", commandLineArguments);

            this.DataCollectorProcess = this.processHelper.LaunchProcess(currentProcessFileName, argumentsString, currentWorkingDirectory);
            return this.DataCollectorProcess.Id;
        }

        /// <summary>
        /// Get full path for the .net host
        /// </summary>
        /// <returns>Full path to <c>dotnet</c> executable</returns>
        /// <remarks>Debuggers require the full path of executable to launch it.</remarks>
        private string GetDotnetHostFullPath()
        {
            var separator = ';';
            var dotnetExeName = "dotnet.exe";

#if !NET46
            // Use semicolon(;) as path separator for windows
            // colon(:) for Linux and OSX
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                separator = ':';
                dotnetExeName = "dotnet";
            }
#endif

            var pathString = Environment.GetEnvironmentVariable("PATH");
            foreach (string path in pathString.Split(separator))
            {
                var exeFullPath = Path.Combine(path.Trim(), dotnetExeName);
                if (File.Exists(exeFullPath))
                {
                    return exeFullPath;
                }
            }

            var errorMessage = string.Format(Resources.NoDotnetExeFound, dotnetExeName);
            if (EqtTrace.IsErrorEnabled)
            {
                EqtTrace.Error(errorMessage);
            }

            throw new FileNotFoundException(errorMessage);
        }
    }
}