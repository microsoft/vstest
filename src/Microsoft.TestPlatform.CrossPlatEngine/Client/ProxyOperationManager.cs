// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Extensions;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    using CrossPlatEngineResources = Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Resources.Resources;

    /// <summary>
    /// Base class for any operations that the client needs to drive through the engine.
    /// </summary>
    public abstract class ProxyOperationManager
    {
        private readonly ITestRuntimeProvider testHostManager;
        private readonly IProcessHelper processHelper;
        private readonly int connectionTimeout;
        private readonly string versionCheckPropertyName = "IsVersionCheckRequired";
        private readonly ManualResetEventSlim testHostExited = new ManualResetEventSlim(false);

        private bool channelConnected;
        private bool initialized;
        private string testHostProcessStdError;
        private bool testHostLaunched;

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
            this.testHostLaunched = false;
            this.channelConnected = false;
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

        // ToDo: change SetupChannel to return bool, which will specify that a successfull connection with testhost was established or not

        /// <summary>
        /// Ensure that the engine is ready for test operations.
        /// Usually includes starting up the test host process.
        /// </summary>
        /// <param name="sources">List of test sources.</param>
        /// <param name="cancellationToken"></param>
        public virtual void SetupChannel(IEnumerable<string> sources, CancellationToken cancellationToken)
        {
            if (!this.initialized)
            {
                this.testHostProcessStdError = string.Empty;

                var portNumber = this.RequestSender.InitializeCommunication();
                var processId = this.processHelper.GetCurrentProcessId();

                var connectionInfo = new TestRunnerConnectionInfo { Port = portNumber, RunnerProcessId = processId, LogFile = this.GetTimestampedLogFile(EqtTrace.LogFile) };

                // Subscribe to TestHost Event
                this.testHostManager.HostLaunched += this.TestHostManagerHostLaunched;
                this.testHostManager.HostExited += this.TestHostManagerHostExited;
                
                // Get the test process start info
                var testHostStartInfo = this.UpdateTestProcessStartInfo(this.testHostManager.GetTestHostProcessStartInfo(sources, null, connectionInfo));
                try
                {
                    // Launch the test host.
                    var hostLaunchedTask = this.testHostManager.LaunchTestHostAsync(testHostStartInfo, cancellationToken);
                    this.testHostLaunched = hostLaunchedTask.Result;
                }
                catch (Exception ex)
                {
                    throw new TestPlatformException(string.Format(CultureInfo.CurrentUICulture, ex.Message));
                }

                // test host launched failed because of user cancellation or any other runtime exception
                // or connection with test host did not happen
                if (!this.testHostLaunched || !this.channelConnected)
                {
                    var errorMsg = CrossPlatEngineResources.InitializationFailed;

                    if (!string.IsNullOrWhiteSpace(this.testHostProcessStdError))
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
            try
            {
                // do not send message if host did not launch
                if (this.testHostLaunched)
                {
                    this.RequestSender.EndSession();

                    // We want to give test host a chance to safely close.
                    // The upper bound for wait should be 100ms.
                    this.testHostExited.Wait(100);
                }
            }
            catch (Exception ex)
            {
                // Error in sending an end session is not necessarily a failure. Discovery and execution should be already
                // complete at this time.
                EqtTrace.Warning("ProxyOperationManager: Failed to end session: " + ex);
            }
            finally
            {
                this.initialized = false;

                EqtTrace.Warning("ProxyOperationManager: Timed out waiting for test host to exit. Will terminate process.");
                
                // please clean up test host. 
                this.testHostManager.CleanTestHostAsync(CancellationToken.None).Wait();

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
            if (string.IsNullOrWhiteSpace(logFile))
            {
                return null;
            }

            return Path.ChangeExtension(
                logFile,
                string.Format(
                    "host.{0}_{1}{2}",
                    DateTime.Now.ToString("yy-MM-dd_HH-mm-ss_fffff"),
                    Thread.CurrentThread.ManagedThreadId,
                    Path.GetExtension(logFile))).AddDoubleQuote();
        }

        private void TestHostManagerHostLaunched(object sender, HostProviderEventArgs e)
        {
            var connTimeout = this.connectionTimeout;

            // Warn the user that execution will wait for debugger attach.
            var hostDebugEnabled = Environment.GetEnvironmentVariable("VSTEST_HOST_DEBUG");
            if (!string.IsNullOrEmpty(hostDebugEnabled) && hostDebugEnabled.Equals("1", StringComparison.Ordinal))
            {
                ConsoleOutput.Instance.WriteLine(CrossPlatEngineResources.HostDebuggerWarning, OutputLevel.Warning);
                ConsoleOutput.Instance.WriteLine(
                    string.Format("Process Id: {0}, Name: {1}", e.ProcessId, this.processHelper.GetProcessName(e.ProcessId)),
                    OutputLevel.Information);

                // Increase connection timeout when debugging is enabled.
                connTimeout = 5 * this.connectionTimeout;
            }

            // Wait for a timeout for the client to connect.
            if (!this.RequestSender.WaitForRequestHandlerConnection(connTimeout))
            {
                this.channelConnected = false;
                return;
            }

            this.channelConnected = true;
        }

        private void TestHostManagerHostExited(object sender, HostProviderEventArgs e)
        {
            this.testHostProcessStdError = e.Data;

            this.RequestSender.OnClientProcessExit(this.testHostProcessStdError);

            this.testHostExited.Set();
        }
    }
}
