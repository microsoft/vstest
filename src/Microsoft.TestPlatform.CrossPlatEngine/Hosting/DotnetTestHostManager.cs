// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;

    using Microsoft.VisualStudio.TestPlatform;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Microsoft.Extensions.DependencyModel;
    using Common.Logging;
    using ObjectModel.Logging;

    /// <summary>
    /// A host manager for <c>dotnet</c> core runtime.
    /// </summary>
    /// <remarks>
    /// Note that some functionality of this entity overlaps with that of <see cref="DefaultTestHostManager"/>. That is
    /// intentional since we want to move this to a separate assembly (with some runtime extensibility discovery).
    /// </remarks>
    public class DotnetTestHostManager : ITestHostManager
    {
        private readonly IProcessHelper processHelper;

        private readonly IFileHelper fileHelper;

        private ITestHostLauncher testHostLauncher;

        private Process testHostProcess;

        private EventHandler registeredExitHandler;

        private TestSessionMessageLogger logger = TestSessionMessageLogger.Instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="DotnetTestHostManager"/> class.
        /// </summary>
        public DotnetTestHostManager()
            : this(new DefaultTestHostLauncher(), new ProcessHelper(), new FileHelper())
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
            IFileHelper fileHelper)
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
            // This host manager can create process start info for dotnet core targets only.
            // If already running with the dotnet executable, use it; otherwise pick up the dotnet available on path.
            var startInfo = new TestProcessStartInfo();

            var currentProcessPath = this.processHelper.GetCurrentProcessFileName();
            if (currentProcessPath.EndsWith("dotnet", StringComparison.OrdinalIgnoreCase)
                || currentProcessPath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase))
            {
                startInfo.FileName = currentProcessPath;
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

                var runtimeConfigDevPath = Path.Combine(sourceDirectory, string.Concat(sourceFile, ".runtimeconfig.dev.json"));
                if (this.fileHelper.Exists(runtimeConfigDevPath))
                {
                    var testHostPath = GetTestHostPath(runtimeConfigDevPath, depsFilePath);
                    args += " \"" + testHostPath + "\" " + connectionInfo.ToCommandLineOptions();
                }
                else
                {
                    logger.SendMessage(TestMessageLevel.Error, string.Format(CrossPlatEngine.Resources.JsonFileDoesNotExist, runtimeConfigDevPath));
                }
            }
            else
            {
                logger.SendMessage(TestMessageLevel.Error, string.Format(CrossPlatEngine.Resources.JsonFileDoesNotExist, depsFilePath));
            }

            // Create a additional probing path args with Nuget.Client
            // args += "--additionalprobingpath xxx"
            // TODO this may be required in ASP.net, requires validation

            // Sample command line for the spawned test host
            // "D:\dd\gh\Microsoft\vstest\tools\dotnet\dotnet.exe" exec
            // --runtimeconfig G:\tmp\netcore-test\bin\Debug\netcoreapp1.0\netcore-test.runtimeconfig.json
            // --depsfile G:\tmp\netcore-test\bin\Debug\netcoreapp1.0\netcore-test.deps.json
            // --additionalprobingpath C:\Users\armahapa\.nuget\packages\ 
            // G:\nuget-package-path\microsoft.testplatform.testhost\version\**\testhost.dll
            // G:\tmp\netcore-test\bin\Debug\netcoreapp1.0\netcore-test.dll
            startInfo.Arguments = args;
            startInfo.EnvironmentVariables = environmentVariables ?? new Dictionary<string, string>();
            startInfo.WorkingDirectory = Directory.GetCurrentDirectory();

            return startInfo;
        }

        private string GetTestHostPath(string runtimeConfigDevPath, string depsFilePath)
        {
            string testHostPackageName = "microsoft.testplatform.testhost";
            string testHostPath = string.Empty;

            // Get testhost relative path
            using (var stream = this.fileHelper.GetStream(depsFilePath, FileMode.Open))
            {
                var context = new DependencyContextJsonReader().Read(stream);
                var testhostPackage = context.RuntimeLibraries.Where(lib => lib.Name.Equals(testHostPackageName)).FirstOrDefault();

                foreach (var runtimeAssemblyGroup in testhostPackage?.RuntimeAssemblyGroups)
                {
                    foreach (var path in runtimeAssemblyGroup.AssetPaths)
                    {
                        if (path.Contains("testhost.dll"))
                        {
                            testHostPath = path;
                        }
                    }
                }

                testHostPath = Path.Combine(testhostPackage?.Path, testHostPath);
            }

            // Get probing path
            List<string> prabingPath = new List<string>();

            using (StreamReader file = File.OpenText(runtimeConfigDevPath))
            using (JsonTextReader reader = new JsonTextReader(file))
            {
                JObject context = (JObject)JToken.ReadFrom(reader);
                JObject runtimeOptions = (JObject)context.GetValue("runtimeOptions");
                JToken additionalProbingPaths = runtimeOptions.GetValue("additionalProbingPaths");
                foreach (var x in additionalProbingPaths)
                {
                    prabingPath.Add(x.ToString());
                }
            }

            foreach (string pb in prabingPath)
            {
                string testHostFullPath = Path.Combine(pb, testHostPath);
                if (this.fileHelper.Exists(testHostFullPath))
                {
                    return testHostFullPath;
                }
            }

            logger.SendMessage(TestMessageLevel.Error, CrossPlatEngine.Resources.NoTestHostFileExist);
            return string.Empty;
        }

        /// <inheritdoc/>
        public IEnumerable<string> GetTestPlatformExtensions(IEnumerable<string> sources)
        {
            var sourceDirectory = Path.GetDirectoryName(sources.Single());

            if (!string.IsNullOrEmpty(sourceDirectory) && this.fileHelper.DirectoryExists(sourceDirectory))
            {
                return this.fileHelper.EnumerateFiles(sourceDirectory, "*.TestAdapter.dll", SearchOption.TopDirectoryOnly);
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

        /// <summary>
        /// Get full path for the .net host
        /// </summary>
        /// <returns>Full path to <c>dotnet</c> executable</returns>
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
