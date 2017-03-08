// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Threading;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    using CrossPlatEngineResources = Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Resources.Resources;
    using System.Threading.Tasks;

    /// <summary>
    /// Base class for any operations that the client needs to drive through the engine.
    /// </summary>
    public abstract class ProxyOperationManager
    {
        private readonly ITestRunTimeProvider testHostManager;

        private bool initialized;

        private readonly int connectionTimeout;

        private StringBuilder testHostProcessStdError;

        private readonly IProcessHelper processHelper;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyOperationManager"/> class. 
        /// </summary>
        /// <param name="requestSender">Request Sender instance.</param>
        /// <param name="testHostManager">Test host manager instance.</param>
        /// <param name="clientConnectionTimeout">Client Connection Timeout.</param>
        protected ProxyOperationManager(ITestRequestSender requestSender, ITestRunTimeProvider testHostManager, int clientConnectionTimeout)
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
                this.testHostProcessStdError = new StringBuilder(this.ErrorLength, this.ErrorLength);

                var portNumber = this.RequestSender.InitializeCommunication();
                var processId = this.processHelper.GetCurrentProcessId();
                var connectionInfo = new TestRunnerConnectionInfo { Port = portNumber, RunnerProcessId = processId, LogFile = this.GetTimestampedLogFile(EqtTrace.LogFile) };

                // Get the test process start info
                var testHostStartInfo = this.testHostManager.GetTestHostProcessStartInfo(sources, null, connectionInfo);
                
                //Subscribe to TestHost Event
                this.testHostManager.HostLaunched += TestHostManager_HostLaunched;
                this.testHostManager.HostError += TestHostManager_HostError;

                this.UpdateTestProcessStartInfo(testHostStartInfo);

                // Launch the test host.
                CancellationTokenSource hostLaunchCTS = new CancellationTokenSource();
                Task<int> hostLaunchedTask = this.testHostManager.LaunchTestHost(testHostStartInfo);

                try
                {
                    hostLaunchedTask.Wait(hostLaunchCTS.Token);
                }
                catch(OperationCanceledException ex)
                {
                    throw new TestPlatformException(string.Format(CultureInfo.CurrentUICulture, ex.Message));
                }

                // Warn the user that execution will wait for debugger attach.
                var hostDebugEnabled = Environment.GetEnvironmentVariable("VSTEST_HOST_DEBUG");
                if (!string.IsNullOrEmpty(hostDebugEnabled) && hostDebugEnabled.Equals("1", StringComparison.Ordinal))
                {
                    ConsoleOutput.Instance.WriteLine(CrossPlatEngineResources.HostDebuggerWarning, OutputLevel.Warning);
                    ConsoleOutput.Instance.WriteLine(
                        string.Format("Process Id: {0}, Name: {1}", processId, this.processHelper.GetProcessName(hostLaunchedTask.Result)),
                        OutputLevel.Information);

                    // Increase connection timeout when debugging is enabled.
                    connTimeout = 5 * this.connectionTimeout;
                }
                
                this.initialized = true;
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
        }

        private void TestHostManager_HostLaunched(object sender, HostProviderEventArgs e)
        {
            EqtTrace.Verbose(e.Data);
        }

        private void TestHostManager_HostError(object sender, HostProviderEventArgs e)
        {
            this.testHostProcessStdError.Clear();
            this.testHostProcessStdError.Append(e.Data);
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
                this.testHostManager.HostError -= TestHostManager_HostError;
                this.testHostManager.HostLaunched -= TestHostManager_HostLaunched;
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
            return Path.ChangeExtension(logFile,
                string.Format("host.{0}_{1}{2}", DateTime.Now.ToString("yy-MM-dd_HH-mm-ss_fffff"),
                    Thread.CurrentThread.ManagedThreadId, Path.GetExtension(logFile)));
        }

        /// <summary>
        /// Returns the current error data in stream
        /// Written purely for UT as of now.
        /// </summary>
        protected virtual string GetStandardError()
        {
            return testHostProcessStdError.ToString();
        }
    }
}
