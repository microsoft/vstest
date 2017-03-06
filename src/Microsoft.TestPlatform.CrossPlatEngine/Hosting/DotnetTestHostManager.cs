// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    using Microsoft.Extensions.DependencyModel;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Resources;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// A host manager for <c>dotnet</c> core runtime.
    /// </summary>
    /// <remarks>
    /// Note that some functionality of this entity overlaps with that of <see cref="DefaultTestHostManager"/>. That is
    /// intentional since we want to move this to a separate assembly (with some runtime extensibility discovery).
    /// </remarks>
    public class DotnetTestHostManager : ITestHostProvider
    {
        private readonly IProcessHelper processHelper;

        private readonly IFileHelper fileHelper;

        private ITestHostLauncher testHostLauncher;

        private Process testHostProcess;

        private EventHandler registeredExitHandler;

        private IDotnetHostHelper dotnetHostHelper;

        private TestSessionMessageLogger logger = TestSessionMessageLogger.Instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="DotnetTestHostManager"/> class.
        /// </summary>
        public DotnetTestHostManager()
            : this(new DefaultTestHostLauncher(), new ProcessHelper(), new FileHelper(), new DotnetHostHelper())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DotnetTestHostManager"/> class.
        /// </summary>
        /// <param name="testHostLauncher">A test host launcher instance.</param>
        /// <param name="processHelper">Process helper instance.</param>
        /// <param name="fileHelper">File helper instance.</param>
        internal DotnetTestHostManager(
            ITestHostLauncher testHostLauncher,
            IProcessHelper processHelper,
            IFileHelper fileHelper, IDotnetHostHelper dotnetHostHelper)
        {
            this.testHostLauncher = testHostLauncher;
            this.processHelper = processHelper;
            this.fileHelper = fileHelper;
            this.dotnetHostHelper = dotnetHostHelper;
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
            var processId = this.testHostLauncher.LaunchTestHost(testHostStartInfo);
            this.testHostProcess = Process.GetProcessById(processId);
            return processId;
        }

        /// <inheritdoc/>
        public virtual TestProcessStartInfo GetTestHostProcessStartInfo(
            IEnumerable<string> sources,
            IDictionary<string, string> environmentVariables,
            TestRunnerConnectionInfo connectionInfo)
        {
            var startInfo = new TestProcessStartInfo();

            var currentProcessPath = this.processHelper.GetCurrentProcessFileName();

            // This host manager can create process start info for dotnet core targets only.
            // If already running with the dotnet executable, use it; otherwise pick up the dotnet available on path.
            // Wrap the paths with quotes in case dotnet executable is installed on a path with whitespace.
            if (currentProcessPath.EndsWith("dotnet", StringComparison.OrdinalIgnoreCase)
                || currentProcessPath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase))
            {
                startInfo.FileName = currentProcessPath;
            }
            else
            {
                startInfo.FileName = this.dotnetHostHelper.GetDotnetHostFullPath();
            }

            EqtTrace.Verbose("DotnetTestHostmanager: Full path of dotnet.exe is {0}", startInfo.FileName);

            // .NET core host manager is not a shared host. It will expect a single test source to be provided.
            var args = "exec";
            var sourcePath = sources.Single();
            var sourceFile = Path.GetFileNameWithoutExtension(sourcePath);
            var sourceDirectory = Path.GetDirectoryName(sourcePath);

            // Probe for runtimeconfig and deps file for the test source
            var runtimeConfigPath = Path.Combine(sourceDirectory, string.Concat(sourceFile, ".runtimeconfig.json"));
            if (this.fileHelper.Exists(runtimeConfigPath))
            {
                string argsToAdd = " --runtimeconfig \"" + runtimeConfigPath + "\"";
                args += argsToAdd;
                EqtTrace.Verbose("DotnetTestHostmanager: Adding {0} in args", argsToAdd);
            }
            else
            {
                EqtTrace.Verbose("DotnetTestHostmanager: File {0}, doesnot exist", runtimeConfigPath);
            }

            // Use the deps.json for test source
            var depsFilePath = Path.Combine(sourceDirectory, string.Concat(sourceFile, ".deps.json"));
            if (this.fileHelper.Exists(depsFilePath))
            {
                string argsToAdd = " --depsfile \"" + depsFilePath + "\"";
                args += argsToAdd;
                EqtTrace.Verbose("DotnetTestHostmanager: Adding {0} in args", argsToAdd);
            }
            else
            {
                EqtTrace.Verbose("DotnetTestHostmanager: File {0}, doesnot exist", depsFilePath);
            }

            var runtimeConfigDevPath = Path.Combine(sourceDirectory, string.Concat(sourceFile, ".runtimeconfig.dev.json"));
            var testHostPath = this.GetTestHostPath(runtimeConfigDevPath, depsFilePath, sourceDirectory);

            if (this.fileHelper.Exists(testHostPath))
            {
                EqtTrace.Verbose("DotnetTestHostmanager: Full path of testhost.dll is {0}", testHostPath);
                args += " \"" + testHostPath + "\" " + connectionInfo.ToCommandLineOptions();
            }
            else
            {
                string message = string.Format(Resources.NoTestHostFileExist, sourcePath);
                EqtTrace.Verbose("DotnetTestHostmanager: " + message);
                throw new FileNotFoundException(message);
            }

            // Create a additional probing path args with Nuget.Client
            // args += "--additionalprobingpath xxx"
            // TODO this may be required in ASP.net, requires validation

            // Sample command line for the spawned test host
            // "D:\dd\gh\Microsoft\vstest\tools\dotnet\dotnet.exe" exec
            // --runtimeconfig G:\tmp\netcore-test\bin\Debug\netcoreapp1.0\netcore-test.runtimeconfig.json
            // --depsfile G:\tmp\netcore-test\bin\Debug\netcoreapp1.0\netcore-test.deps.json
            // --additionalprobingpath C:\Users\username\.nuget\packages\ 
            // G:\nuget-package-path\microsoft.testplatform.testhost\version\**\testhost.dll
            // G:\tmp\netcore-test\bin\Debug\netcoreapp1.0\netcore-test.dll
            startInfo.Arguments = args;
            startInfo.EnvironmentVariables = environmentVariables ?? new Dictionary<string, string>();
            startInfo.WorkingDirectory = sourceDirectory;

            return startInfo;
        }

        /// <inheritdoc/>
        public IEnumerable<string> GetTestPlatformExtensions(IEnumerable<string> sources)
        {
            var sourceDirectory = Path.GetDirectoryName(sources.Single());

            if (!string.IsNullOrEmpty(sourceDirectory) && this.fileHelper.DirectoryExists(sourceDirectory))
            {
                return this.fileHelper.EnumerateFiles(sourceDirectory, TestPlatformConstants.TestAdapterRegexPattern, SearchOption.TopDirectoryOnly);
            }

            return Enumerable.Empty<string>();
        }

        /// <inheritdoc/>
        public void RegisterForExitNotification(Action abortCallback)
        {
            if (this.testHostProcess != null && abortCallback != null)
            {
                this.registeredExitHandler = (sender, args) => abortCallback();
                this.testHostProcess.Exited += this.registeredExitHandler;
            }
        }

        /// <inheritdoc/>
        public void DeregisterForExitNotification()
        {
            if (this.testHostProcess != null && this.registeredExitHandler != null)
            {
                this.testHostProcess.Exited -= this.registeredExitHandler;
            }
        }

        private string GetTestHostPath(string runtimeConfigDevPath, string depsFilePath, string sourceDirectory)
        {
            string testHostPackageName = "microsoft.testplatform.testhost";
            string testHostPath = string.Empty;

            if (this.fileHelper.Exists(runtimeConfigDevPath) && this.fileHelper.Exists(depsFilePath))
            {
                EqtTrace.Verbose("DotnetTestHostmanager: Reading file {0} to get path of testhost.dll", depsFilePath);

                // Get testhost relative path
                using (var stream = this.fileHelper.GetStream(depsFilePath, FileMode.Open))
                {
                    var context = new DependencyContextJsonReader().Read(stream);
                    var testhostPackage = context.RuntimeLibraries.Where(lib => lib.Name.Equals(testHostPackageName, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();

                    if (testhostPackage != null)
                    {
                        foreach (var runtimeAssemblyGroup in testhostPackage.RuntimeAssemblyGroups)
                        {
                            foreach (var path in runtimeAssemblyGroup.AssetPaths)
                            {
                                if (path.EndsWith("testhost.dll", StringComparison.CurrentCultureIgnoreCase))
                                {
                                    testHostPath = path;
                                    break;
                                }
                            }
                        }

                        testHostPath = Path.Combine(testhostPackage.Path, testHostPath);
                        EqtTrace.Verbose("DotnetTestHostmanager: Relative path of testhost.dll with respect to package folder is {0}", testHostPath);
                    }
                }

                // Get probing path
                using (StreamReader file = new StreamReader(this.fileHelper.GetStream(runtimeConfigDevPath, FileMode.Open)))
                using (JsonTextReader reader = new JsonTextReader(file))
                {
                    JObject context = (JObject)JToken.ReadFrom(reader);
                    JObject runtimeOptions = (JObject)context.GetValue("runtimeOptions");
                    JToken additionalProbingPaths = runtimeOptions.GetValue("additionalProbingPaths");
                    foreach (var x in additionalProbingPaths)
                    {
                        EqtTrace.Verbose("DotnetTestHostmanager: Looking for path {0} in folder {1}", testHostPath, x.ToString());
                        string testHostFullPath = Path.Combine(x.ToString(), testHostPath);
                        if (this.fileHelper.Exists(testHostFullPath))
                        {
                            return testHostFullPath;
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(testHostPath))
            {
                // Try resolving testhost from output directory of test project. This is required if user has published the test project
                // and is running tests in an isolated machine. A second scenario is self test: test platform unit tests take a project
                // dependency on testhost (instead of nuget dependency), this drops testhost to output path.
                testHostPath = Path.Combine(sourceDirectory, "testhost.dll");
                EqtTrace.Verbose("DotnetTestHostManager: Assume published test project, with test host path = {0}.", testHostPath);
            }

            return testHostPath;
        }

        public bool CanExecuteCurrentRunConfiguration(RunConfiguration runConfiguration)
        {
            var framework = runConfiguration.TargetFrameworkVersion;

            // This is expected to be called once every run so returning a new instance every time.
            if (framework.Name.IndexOf("netstandard", StringComparison.OrdinalIgnoreCase) >= 0
                || framework.Name.IndexOf("netcoreapp", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return false;
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
                    defaultTestHostStartInfo.WorkingDirectory,
                    defaultTestHostStartInfo.ErrorReceivedCallback).Id;
        }
    }
}
