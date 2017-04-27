// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer
{
    using System.Collections.Generic;
    using System.Diagnostics;

    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;

    /// <summary>
    /// An implementation of <see cref="IVsTestConsoleWrapper"/> to invoke test operations
    /// via the <c>vstest.console</c> test runner.
    /// </summary>
    public class VsTestConsoleWrapper : IVsTestConsoleWrapper
    {
        #region Private Members

        private const int ConnectionTimeout = 30 * 1000;

        private readonly IProcessManager vstestConsoleProcessManager;

        private readonly ITranslationLayerRequestSender requestSender;

        private bool sessionStarted;

        /// <summary>
        /// Path to additional extensions to reinitialize vstest.console
        /// </summary>
        private IEnumerable<string> pathToAdditionalExtensions;

        /// <summary>
        /// Additional parameters for vstest.console.exe
        /// </summary>
        private readonly ConsoleParameters consoleParameters;

        private readonly ITestPlatformEventSource testPlatformEventSource;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="VsTestConsoleWrapper"/> class.
        /// </summary>
        /// <param name="vstestConsolePath">
        /// Path to the test runner <c>vstest.console.exe</c>.
        /// </param>
        public VsTestConsoleWrapper(string vstestConsolePath) :
            this(vstestConsolePath, ConsoleParameters.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VsTestConsoleWrapper"/> class.
        /// </summary>
        /// <param name="vstestConsolePath">Path to the test runner <c>vstest.console.exe</c>.</param>
        /// <param name="consoleParameters">The parameters to be passed onto the runner process</param>
        public VsTestConsoleWrapper(string vstestConsolePath, ConsoleParameters consoleParameters) :
            this(new VsTestConsoleRequestSender(), new VsTestConsoleProcessManager(vstestConsolePath), consoleParameters, TestPlatformEventSource.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VsTestConsoleWrapper"/> class.
        /// </summary>
        /// <param name="requestSender">Sender for test messages.</param>
        /// <param name="processManager">Process manager.</param>
        /// <param name="consoleParameters">The parameters to be passed onto the runner process</param>
        /// <param name="testPlatformEventSource">Performance event source</param>
        internal VsTestConsoleWrapper(ITranslationLayerRequestSender requestSender, IProcessManager processManager, ConsoleParameters consoleParameters, ITestPlatformEventSource testPlatformEventSource)
        {
            this.requestSender = requestSender;
            this.vstestConsoleProcessManager = processManager;
            this.consoleParameters = consoleParameters;
            this.testPlatformEventSource = testPlatformEventSource;

            this.vstestConsoleProcessManager.ProcessExited += (sender, args) => this.requestSender.OnProcessExited();
            this.sessionStarted = false;
        }

        #endregion

        #region IVsTestConsoleWrapper

        /// <inheritdoc/>
        public void StartSession()
        {
            this.testPlatformEventSource.TranslationLayerInitializeStart();

            // Start communication
            var port = this.requestSender.InitializeCommunication();

            if (port > 0)
            {
                // Fill the parameters
                this.consoleParameters.ParentProcessId = Process.GetCurrentProcess().Id;
                this.consoleParameters.PortNumber = port;

                // Start vstest.console.exe process
                this.vstestConsoleProcessManager.StartProcess(this.consoleParameters);
            }
            else
            {
                // Close the sender as it failed to host server
                this.requestSender.Close();
                throw new TransationLayerException("Error hosting communication channel");
            }
        }

        /// <inheritdoc/>
        public void InitializeExtensions(IEnumerable<string> pathToAdditionalExtensions)
        {
            this.EnsureInitialized();
            this.pathToAdditionalExtensions = pathToAdditionalExtensions.ToList();
            this.requestSender.InitializeExtensions(this.pathToAdditionalExtensions);
        }

        /// <inheritdoc/>
        public void DiscoverTests(IEnumerable<string> sources, string discoverySettings, ITestDiscoveryEventsHandler discoveryEventsHandler)
        {
            this.testPlatformEventSource.TranslationLayerDiscoveryStart();
            this.EnsureInitialized();
            this.requestSender.DiscoverTests(sources, discoverySettings, discoveryEventsHandler);
        }

        /// <inheritdoc/>
        public void CancelDiscovery()
        {
            // TODO: Cancel Discovery
            // this.requestSender.CancelDiscovery();
        }

        /// <inheritdoc/>
        public void RunTests(IEnumerable<string> sources, string runSettings, ITestRunEventsHandler testRunEventsHandler)
        {
            var sourceList = sources.ToList();
            this.testPlatformEventSource.TranslationLayerExecutionStart(0, sourceList.Count, 0, runSettings ?? string.Empty);

            this.EnsureInitialized();
            this.requestSender.StartTestRun(sourceList, runSettings, testRunEventsHandler);
        }

        /// <inheritdoc/>
        public void RunTests(IEnumerable<TestCase> testCases, string runSettings, ITestRunEventsHandler testRunEventsHandler)
        {
            var testCaseList = testCases.ToList();
            this.testPlatformEventSource.TranslationLayerExecutionStart(0, 0, testCaseList.Count, runSettings ?? string.Empty);

            this.EnsureInitialized();
            this.requestSender.StartTestRun(testCaseList, runSettings, testRunEventsHandler);
        }

        /// <inheritdoc/>
        public void RunTestsWithCustomTestHost(IEnumerable<string> sources, string runSettings, ITestRunEventsHandler testRunEventsHandler, ITestHostLauncher customTestHostLauncher)
        {
            var sourceList = sources.ToList();
            this.testPlatformEventSource.TranslationLayerExecutionStart(1, sourceList.Count, 0, runSettings ?? string.Empty);

            this.EnsureInitialized();
            this.requestSender.StartTestRunWithCustomHost(sourceList, runSettings, testRunEventsHandler, customTestHostLauncher);
        }

        /// <inheritdoc/>
        public void RunTestsWithCustomTestHost(IEnumerable<TestCase> testCases, string runSettings, ITestRunEventsHandler testRunEventsHandler, ITestHostLauncher customTestHostLauncher)
        {
            var testCaseList = testCases.ToList();
            this.testPlatformEventSource.TranslationLayerExecutionStart(1, 0, testCaseList.Count, runSettings ?? string.Empty);

            this.EnsureInitialized();
            this.requestSender.StartTestRunWithCustomHost(testCaseList, runSettings, testRunEventsHandler, customTestHostLauncher);
        }

        /// <inheritdoc/>
        public void CancelTestRun()
        {
            this.requestSender.CancelTestRun();
        }

        /// <inheritdoc/>
        public void AbortTestRun()
        {
            this.requestSender.AbortTestRun();
        }

        /// <inheritdoc/>
        public void EndSession()
        {
            this.requestSender.EndSession();
            this.requestSender.Close();
            this.sessionStarted = false;
        }

        #endregion

        private void EnsureInitialized()
        {
            if (!this.vstestConsoleProcessManager.IsProcessInitialized())
            {
                EqtTrace.Info("VsTestConsoleWrapper.EnsureInitialized: Process is not started.");
                this.StartSession();
                this.sessionStarted = this.WaitForConnection();

                if (this.sessionStarted)
                {
                    EqtTrace.Info("VsTestConsoleWrapper.EnsureInitialized: Send a request to initialize extensions.");
                    this.requestSender.InitializeExtensions(this.pathToAdditionalExtensions);
                }
            }

            if (!this.sessionStarted && this.requestSender != null)
            {
                EqtTrace.Info("VsTestConsoleWrapper.EnsureInitialized: Process Started.");
                this.sessionStarted = this.WaitForConnection();
            }

            if (!this.sessionStarted)
            {
                throw new TransationLayerException("Error connecting to Vstest Command Line");
            }
        }

        private bool WaitForConnection()
        {
            EqtTrace.Info("VsTestConsoleWrapper.WaitForConnection: Waiting for connection to command line runner.");
            var connected = this.requestSender.WaitForRequestHandlerConnection(ConnectionTimeout);
            this.testPlatformEventSource.TranslationLayerInitializeStop();

            return connected;
        }
    }
}
