// Copyright (c) Microsoft. All rights reserved.
namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    /// <summary>
    /// A host manager for <c>dotnet</c> core runtime.
    /// </summary>
    public class DotnetTestHostManager : ITestHostManager
    {
        private readonly IProcessHelper processHelper;

        private readonly IFileHelper fileHelper;

        private ITestHostLauncher testHostLauncher;

        /// <summary>
        /// Initializes a new instance of the <see cref="DotnetTestHostManager"/> class.
        /// </summary>
        public DotnetTestHostManager() : this(new DefaultTestHostLauncher(), new ProcessHelper(), new FileHelper())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DotnetTestHostManager"/> class.
        /// </summary>
        /// <param name="testHostLauncher">A test host launcher instance.</param>
        /// <param name="processHelper">Process helper instance.</param>
        /// <param name="fileHelper">File helper instance.</param>
        internal DotnetTestHostManager(ITestHostLauncher testHostLauncher, IProcessHelper processHelper, IFileHelper fileHelper)
        {
            this.testHostLauncher = testHostLauncher;
            this.processHelper = processHelper;
            this.fileHelper = fileHelper;
        }

        /// <summary>
        /// Gets a value indicating if the test host can be shared for multiple sources.
        /// </summary>
        /// <remarks>
        /// Dependency resolution for .net core projects are pivoted by the test project. Hence each test
        /// project must be launched in a separate test host process.
        /// </remarks>
        public bool Shared => false;

        /// <inheritdoc/>
        public void SetCustomLauncher(ITestHostLauncher customLauncher)
        {
            this.testHostLauncher = customLauncher;
        }

        /// <inheritdoc/>
        public int LaunchTestHost(TestProcessStartInfo testHostStartInfo)
        {
            return this.testHostLauncher.LaunchTestHost(testHostStartInfo);
        }

        /// <inheritdoc/>
        public virtual TestProcessStartInfo GetTestHostProcessStartInfo(
            IEnumerable<string> sources,
            IDictionary<string, string> environmentVariables,
            TestRunnerConnectionInfo connectionInfo)
        {
            // This host manager can create process start info for dotnet core targets only.
            // If already running with the dotnet executable, use it; otherwise pick up the dotnet available on path.
            var startInfo = new TestProcessStartInfo { FileName = "dotnet" };

            var testHostExecutable = Path.Combine("NetCore", "testhost.dll");
            var currentProcessPath = this.processHelper.GetCurrentProcessFileName();
            var testRunnerDirectory = Path.GetDirectoryName(currentProcessPath);
            if (currentProcessPath.EndsWith("dotnet", StringComparison.OrdinalIgnoreCase)
                || currentProcessPath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase))
            {
                startInfo.FileName = currentProcessPath;
                testHostExecutable = "testhost.dll";
                testRunnerDirectory = this.processHelper.GetTestEngineDirectory();
            }
            else
            {
                startInfo.FileName = this.GetDotnetHostFullPath();
            }

            // .NET core host manager is not a shared host. It will expect a single test source to be provided.
            var args = "exec";
            var sourcePath = sources.Single();
            var sourceFile = Path.GetFileNameWithoutExtension(sourcePath);
            var sourceDirectory = Path.GetDirectoryName(sourcePath);

            // Probe for runtimeconfig and deps file for the test source
            var runtimeConfigPath = Path.Combine(sourceDirectory, string.Concat(sourceFile, ".runtimeconfig.json"));
            if (this.fileHelper.Exists(runtimeConfigPath))
            {
                args += " --runtimeconfig \"" + runtimeConfigPath + "\"";
            }

            // Use the deps.json for test source
            var depsFilePath = Path.Combine(sourceDirectory, string.Concat(sourceFile, ".deps.json"));
            if (this.fileHelper.Exists(depsFilePath))
            {
                args += " --depsfile \"" + depsFilePath + "\"";
            }

            // Create a additional probing path args with Nuget.Client
            // args += "--additionalprobingpath xxx"
            // TODO this may be required in ASP.net, requires validation

            // Add the testhost path and other arguments
            var testHostPath = Path.Combine(testRunnerDirectory, testHostExecutable);
            args += " \"" + testHostPath + "\" " + CrossPlatEngine.Constants.PortOption + " " + connectionInfo.Port + 
                " " + CrossPlatEngine.Constants.ParentProcessIdOption + " " + this.processHelper.GetCurrentProcessId();

            // Sample command line for the spawned test host
            // "D:\dd\gh\Microsoft\vstest\tools\dotnet\dotnet.exe" exec
            // --runtimeconfig G:\tmp\netcore-test\bin\Debug\netcoreapp1.0\netcore-test.runtimeconfig.json
            // --depsfile G:\tmp\netcore-test\bin\Debug\netcoreapp1.0\netcore-test.deps.json
            // --additionalprobingpath C:\Users\armahapa\.nuget\packages\ 
            // G:\packages\testhost.dll
            // G:\tmp\netcore-test\bin\Debug\netcoreapp1.0\netcore-test.dll
            startInfo.Arguments = args;
            startInfo.EnvironmentVariables = environmentVariables ?? new Dictionary<string, string>();
            startInfo.WorkingDirectory = sourceDirectory;

            return startInfo;
        }

        /// <summary>
        /// Get full path for the dotnet host
        /// </summary>
        /// <returns></returns>
        private string GetDotnetHostFullPath()
        {
            char separator = ';'; 
            var dotnetExeName = "dotnet.exe";

            // Use semicolon(;) as path separator for windows
            // colon(:) for Linux and OSX
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                separator = ':';
                dotnetExeName = "dotnet";
            }

            var pathString = Environment.GetEnvironmentVariable("PATH");
            foreach (string path in pathString.Split(separator))
            {
                string exeFullPath = Path.Combine(path.Trim(), dotnetExeName);
                if (this.fileHelper.Exists(exeFullPath))
                {
                    return exeFullPath;
                }
            }

            EqtTrace.Error("Unable to find path for dotnet host");
            return dotnetExeName;
        }

        /// <inheritdoc/>
        public void RegisterForExitNotification(Action abortCallback)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void DeregisterForExitNotification()
        {
            throw new NotImplementedException();
        }
    }

    public class DefaultTestHostLauncher : ITestHostLauncher
    {
        private readonly IProcessHelper processHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultTestHostLauncher"/> class.
        /// </summary>
        public DefaultTestHostLauncher() : this(new ProcessHelper())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultTestHostLauncher"/> class.
        /// </summary>
        /// <param name="processHelper">Process helper instance.</param>
        internal DefaultTestHostLauncher(IProcessHelper processHelper)
        {
            this.processHelper = processHelper;
        }

        /// <inheritdoc/>
        public bool IsDebug => false;

        /// <inheritdoc/>
        public int LaunchTestHost(TestProcessStartInfo defaultTestHostStartInfo)
        {
            return this.processHelper.LaunchProcess(
                    defaultTestHostStartInfo.FileName,
                    defaultTestHostStartInfo.Arguments,
                    defaultTestHostStartInfo.WorkingDirectory).Id;
        }
    }
}