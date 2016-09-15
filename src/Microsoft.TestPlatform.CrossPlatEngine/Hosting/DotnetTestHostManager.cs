// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

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
        private ITestHostLauncher testHostLauncher;

        private readonly IProcessHelper processHelper;

        private readonly IFileHelper fileHelper;

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
        /// <param name="processHelper"></param>
        internal DotnetTestHostManager(ITestHostLauncher testHostLauncher, IProcessHelper processHelper, IFileHelper fileHelper)
        {
            this.testHostLauncher = testHostLauncher;
            this.processHelper = processHelper;
            this.fileHelper = fileHelper;
        }

        /// <inheritdoc/>
        public void SetCustomLauncher(ITestHostLauncher customLauncher)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public int LaunchTestHost(IDictionary<string, string> environmentVariables, IList<string> commandLineArguments)
        {
            var args = commandLineArguments ?? new List<string>();
            var variables = environmentVariables ?? new Dictionary<string, string>();
            var startInfo = new TestProcessStartInfo { Arguments = string.Join(" ", args), EnvironmentVariables = variables };

            return this.testHostLauncher.LaunchTestHost(startInfo);
        }

        /// <inheritdoc/>
        public TestProcessStartInfo GetTestHostProcessStartInfo(IDictionary<string, string> environmentVariables, IList<string> commandLineArguments)
        {
            // This host manager can create process start info for dotnet core targets only.
            // If already running with the dotnet executable, use it; otherwise pick up the dotnet available on path.
            var startInfo = new TestProcessStartInfo { FileName = "dotnet" };
            var testHostExecutable = Path.Combine("NetCore", "testhost.dll");
            var currentProcessPath = this.processHelper.GetCurrentProcessFileName();
            if (currentProcessPath.EndsWith("dotnet", StringComparison.OrdinalIgnoreCase)
                || currentProcessPath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase))
            {
                startInfo.FileName = currentProcessPath;
                testHostExecutable = "testhost.dll";
            }

            // Use the deps.json for test source
            // "D:\dd\gh\Microsoft\vstest\tools\dotnet\dotnet.exe" exec
            // --runtimeconfig G:\tmp\netcore-test\bin\Debug\netcoreapp1.0\netcore-test.runtimeconfig.json
            // --depsfile G:\tmp\netcore-test\bin\Debug\netcoreapp1.0\netcore-test.deps.json
            // --additionalprobingpath C:\Users\armahapa\.nuget\packages\ 
            // C:\Users\armahapa\.nuget\packages\dotnet-test-xunit\2.2.0-preview2-build1029\lib\netcoreapp1.0\dotnet-test-xunit.dll
            // G:\tmp\netcore-test\bin\Debug\netcoreapp1.0\netcore-test.dll
            // Probe for runtimeconfig and deps file for the test source
            var args = "exec";
            string sourcePath = "test";

            var runtimeConfigPath = string.Concat(sourcePath, ".runtimeconfig.json");
            if (this.fileHelper.Exists(runtimeConfigPath))
            {
                args += " --runtimeconfig " + runtimeConfigPath;
            }

            var depsFilePath = string.Concat(sourcePath, ".deps.json");
            if (this.fileHelper.Exists(depsFilePath))
            {
                args += " --depsfile " + depsFilePath;
            }

            // Create a additional probing path args with Nuget.Client
            //args += "--additionalprobingpath xxx"

            // Add the testhost path and other arguments
            var testHostPath = Path.Combine(Path.GetDirectoryName(currentProcessPath), testHostExecutable);
            args += " " + testHostPath;
            if (commandLineArguments != null && commandLineArguments.Count > 0)
            {
                args += " " + string.Join(" ", commandLineArguments);
            }

            startInfo.Arguments = args;

            return startInfo;
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