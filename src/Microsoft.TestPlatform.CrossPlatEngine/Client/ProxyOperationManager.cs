// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    using CrossPlatEngineResources = Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Resources.Resources;
    using System.Reflection;
    using System.Linq;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;

    /// <summary>
    /// Base class for any operations that the client needs to drive through the engine.
    /// </summary>
    public abstract class ProxyOperationManager
    {
        private readonly ITestRuntimeProvider testHostManager;
        private readonly IProcessHelper processHelper;
        private readonly int connectionTimeout;

        private bool initialized;
        private string testHostProcessStdError;
        private readonly string versionCheckPropertyName = "IsVersionCheckRequired";
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyOperationManager"/> class. 
        /// </summary>
        /// <param name="requestSender">Request Sender instance.</param>
        /// <param name="testHostManager">Test host manager instance.</param>
        /// <param name="clientConnectionTimeout">Client Connection Timeout.</param>
        protected ProxyOperationManager(ITestRequestSender requestSender, ITestRuntimeProvider testHostManager, int clientConnectionTimeout)
        {
            this.RequestSender = requestSender;
            this.connectionTimeout = clientConnectionTimeout;
            this.testHostManager = testHostManager;
            this.processHelper = new ProcessHelper();
            this.initialized = false;
        }

        #endregion

        #region Properties

        protected int ErrorLength { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the server for communication.
        /// </summary>
        protected ITestRequestSender RequestSender { get; set; }

        #endregion

        #region IProxyOperationManager implementation.

        /// <summary>
        /// Ensure that the engine is ready for test operations.
        /// Usually includes starting up the test host process.
        /// </summary>
        /// <param name="sources">List of test sources.</param>
        public virtual void SetupChannel(IEnumerable<string> sources)
        {
            var connTimeout = this.connectionTimeout;

            if (!this.initialized)
            {
                this.testHostProcessStdError = string.Empty;

                var portNumber = this.RequestSender.InitializeCommunication();
                var processId = this.processHelper.GetCurrentProcessId();

                var logFileName = this.GetTimestampedLogFile(EqtTrace.LogFile);
                try
                {
                    if (!string.IsNullOrEmpty(logFileName))
                    {
                        // Catch exception(UnauthorizedAccessException, PathTooLongException...) if there is any at time of creating file.
                        using (new FileHelper().Open(logFileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)) { }
                    }
                }
                catch (Exception e)
                {
                    throw new TestPlatformException(e.Message);
                }

                var connectionInfo = new TestRunnerConnectionInfo { Port = portNumber, RunnerProcessId = processId, LogFile = string.IsNullOrWhiteSpace(logFileName) ? null : AddDoubleQuotes(logFileName) };

                // Get the test process start info
                var testHostStartInfo = this.testHostManager.GetTestHostProcessStartInfo(sources, null, connectionInfo);

                // Subscribe to TestHost Event
                this.testHostManager.HostLaunched += this.TestHostManagerHostLaunched;
                this.testHostManager.HostExited += this.TestHostManagerHostExited;

                this.UpdateTestProcessStartInfo(testHostStartInfo);

                // Launch the test host.
                CancellationTokenSource hostLaunchCts = new CancellationTokenSource();
                Task<int> hostLaunchedTask = this.testHostManager.LaunchTestHostAsync(testHostStartInfo);

                try
                {
                    hostLaunchedTask.Wait(hostLaunchCts.Token);
                }
                catch (OperationCanceledException ex)
                {
                    throw new TestPlatformException(string.Format(CultureInfo.CurrentUICulture, ex.Message));
                }

                // Warn the user that execution will wait for debugger attach.
                var hostDebugEnabled = Environment.GetEnvironmentVariable("VSTEST_HOST_DEBUG");
                if (!string.IsNullOrEmpty(hostDebugEnabled) && hostDebugEnabled.Equals("1", StringComparison.Ordinal))
                {
                    ConsoleOutput.Instance.WriteLine(CrossPlatEngineResources.HostDebuggerWarning, OutputLevel.Warning);
                    ConsoleOutput.Instance.WriteLine(
                        string.Format("Process Id: {0}, Name: {1}", hostLaunchedTask.Result, this.processHelper.GetProcessName(hostLaunchedTask.Result)),
                        OutputLevel.Information);

                    // Increase connection timeout when debugging is enabled.
                    connTimeout = 5 * this.connectionTimeout;
                }

                // Wait for a timeout for the client to connect.
                if (!this.RequestSender.WaitForRequestHandlerConnection(connTimeout))
                {
                    var errorMsg = CrossPlatEngineResources.InitializationFailed;

                    if (!string.IsNullOrWhiteSpace(this.testHostProcessStdError.ToString()))
                    {
                        // Testhost failed with error
                        errorMsg = string.Format(CrossPlatEngineResources.TestHostExitedWithError, this.testHostProcessStdError);
                    }

                    throw new TestPlatformException(string.Format(CultureInfo.CurrentUICulture, errorMsg));
                }

                // Handling special case for dotnet core projects with older test hosts
                // Older test hosts are not aware of protocol version check
                // Hence we should not be sending VersionCheck message to these test hosts
                bool checkRequired = true;
                var property = this.testHostManager.GetType().GetRuntimeProperties().FirstOrDefault(p => string.Equals(p.Name, versionCheckPropertyName, StringComparison.OrdinalIgnoreCase));
                if (property != null)
                {
                    checkRequired = (bool)property.GetValue(this.testHostManager);
                }

                if (checkRequired)
                {
                    this.RequestSender.CheckVersionWithTestHost();
                }

                this.initialized = true;
            }
        }

        /// <summary>
        /// Closes the channel, terminate test host process.
        /// </summary>
        public virtual void Close()
        {
            // TODO dispose the testhost process
            try
            {
                this.RequestSender.EndSession();
            }
            finally
            {
                this.initialized = false;
                this.testHostManager.HostExited -= this.TestHostManagerHostExited;
                this.testHostManager.HostLaunched -= this.TestHostManagerHostLaunched;
            }
        }

        #endregion

        /// <summary>
        /// This method is exposed to enable drived classes to modify TestProcessStartInfo. E.g. DataCollection need additional environment variables to be passed, etc.  
        /// </summary>
        /// <param name="testProcessStartInfo">
        /// The sources.
        /// </param>        
        /// <returns>
        /// The <see cref="TestProcessStartInfo"/>.
        /// </returns>
        protected virtual TestProcessStartInfo UpdateTestProcessStartInfo(TestProcessStartInfo testProcessStartInfo)
        {
            // do nothing. 
            return testProcessStartInfo;
        }

        protected string GetTimestampedLogFile(string logFile)
        {
            return Path.ChangeExtension(
                logFile,
                string.Format(
                    "host.{0}_{1}{2}",
                    DateTime.Now.ToString("yy-MM-dd_HH-mm-ss_fffff"),
                    Thread.CurrentThread.ManagedThreadId,
                    Path.GetExtension(logFile)));
        }

        private void TestHostManagerHostLaunched(object sender, HostProviderEventArgs e)
        {
            EqtTrace.Verbose(e.Data);
        }

        private void TestHostManagerHostExited(object sender, HostProviderEventArgs e)
        {
            this.testHostProcessStdError = e.Data;

            this.RequestSender.OnClientProcessExit(this.testHostProcessStdError);
        }

        private string AddDoubleQuotes(string x)
        {
            return "\"" + x + "\"";
        }
    }
}
