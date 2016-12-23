// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    using CrossPlatEngineResources = Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Resources.Resources;

    /// <summary>
    /// Base class for any operations that the client needs to drive through the engine.
    /// </summary>
    public abstract class ProxyOperationManager
    {
        private readonly ITestHostManager testHostManager;

        private bool initialized;

        private readonly int connectionTimeout;

        private readonly IProcessHelper processHelper;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyOperationManager"/> class. 
        /// </summary>
        /// <param name="requestSender">Request Sender instance.</param>
        /// <param name="testHostManager">Test host manager instance.</param>
        /// <param name="clientConnectionTimeout">Client Connection Timeout.</param>
        protected ProxyOperationManager(ITestRequestSender requestSender, ITestHostManager testHostManager, int clientConnectionTimeout)
        {
            this.RequestSender = requestSender;
            this.connectionTimeout = clientConnectionTimeout;
            this.testHostManager = testHostManager;
            this.processHelper = new ProcessHelper();
            this.initialized = false;
        }

        #endregion

        #region Properties

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
            if (!this.initialized)
            {
                var portNumber = this.RequestSender.InitializeCommunication();
                var processId = this.processHelper.GetCurrentProcessId();
                var connectionInfo = new TestRunnerConnectionInfo { Port = portNumber, RunnerProcessId = processId, LogFile = this.GetTimestampedLogFile(EqtTrace.LogFile) };

                // Get the test process start info
                // TODO: Fix the environment variables usage
                var testHostStartInfo = this.testHostManager.GetTestHostProcessStartInfo(sources, null, connectionInfo);

                // Warn the user that execution will wait for debugger attach.
                var hostDebugEnabled = Environment.GetEnvironmentVariable("VSTEST_HOST_DEBUG");
                if (!string.IsNullOrEmpty(hostDebugEnabled) && hostDebugEnabled.Equals("1", StringComparison.Ordinal))
                {
                    ConsoleOutput.Instance.WriteLine(CrossPlatEngineResources.HostDebuggerWarning, OutputLevel.Warning);
                }

                // Launch the test host and monitor exit.
                this.testHostManager.LaunchTestHost(testHostStartInfo);
                this.testHostManager.RegisterForExitNotification(() => this.RequestSender.OnClientProcessExit());

                this.initialized = true;
            }

            // Wait for a timeout for the client to connect.
            if (!this.RequestSender.WaitForRequestHandlerConnection(this.connectionTimeout))
            {
                throw new TestPlatformException(string.Format(CultureInfo.CurrentUICulture, CrossPlatEngineResources.InitializationFailed));
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
            }
        }

        #endregion

        private string GetTimestampedLogFile(string logFile)
        {
            return Path.ChangeExtension(logFile,
                string.Format("host.{0}_{1}{2}", DateTime.Now.ToString("yy-MM-dd_hh-mm-ss_fffff"),
                    Thread.CurrentThread.ManagedThreadId, Path.GetExtension(logFile)));
        }
    }
}
