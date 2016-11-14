// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;

    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
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
        /// Port number for communicating with Vstest CLI
        /// </summary>
        private const string PORT_ARGUMENT = "/port:{0}";

        /// <summary>
        /// Process Id of the Current Process which is launching Vstest CLI
        /// Helps Vstest CLI in auto-exit if current process dies without notifying it
        /// </summary>
        private const string PARENT_PROCESSID_ARGUMENT = "/parentprocessid:{0}";

        /// <summary>
        /// Path to additional extensions to reinitialize vstest.console
        /// </summary>
        private IEnumerable<string> pathToAdditionalExtensions;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="VsTestConsoleWrapper"/> class.
        /// </summary>
        /// <param name="vstestConsolePath">
        /// Path to the test runner <c>vstest.console.exe</c>.
        /// </param>
        public VsTestConsoleWrapper(string vstestConsolePath) : 
            this(new VsTestConsoleRequestSender(), new VsTestConsoleProcessManager(vstestConsolePath))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VsTestConsoleWrapper"/> class.
        /// </summary>
        /// <param name="requestSender">Sender for test messages.</param>
        /// <param name="processManager">Process manager.</param>
        internal VsTestConsoleWrapper(ITranslationLayerRequestSender requestSender, IProcessManager processManager)
        {
            this.requestSender = requestSender;
            this.vstestConsoleProcessManager = processManager;
            this.vstestConsoleProcessManager.ProcessExited += (sender, args) => this.requestSender.OnProcessExited();
            this.sessionStarted = false;
        }

        #endregion

        #region IVsTestConsoleWrapper

        /// <inheritdoc/>
        public void StartSession()
        {
            // Start communication
            var port = this.requestSender.InitializeCommunication();

            if (port > 0)
            {
                // Start Vstest.console.exe with args: --parentProcessId|/parentprocessid:<ppid> --port|/port:<port>
                string parentProcessIdArgs = string.Format(CultureInfo.InvariantCulture, PARENT_PROCESSID_ARGUMENT, Process.GetCurrentProcess().Id);
                string portArgs = string.Format(CultureInfo.InvariantCulture, PORT_ARGUMENT, port);
                this.vstestConsoleProcessManager.StartProcess(new string[2] { parentProcessIdArgs, portArgs });
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
            this.requestSender.InitializeExtensions(pathToAdditionalExtensions);
            this.pathToAdditionalExtensions = pathToAdditionalExtensions;
        }

        /// <inheritdoc/>
        public void DiscoverTests(IEnumerable<string> sources, string discoverySettings, ITestDiscoveryEventsHandler discoveryEventsHandler)
        {
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
            this.EnsureInitialized();
            this.requestSender.StartTestRun(sources, runSettings, testRunEventsHandler);
        }

        /// <inheritdoc/>
        public void RunTests(IEnumerable<TestCase> testCases, string runSettings, ITestRunEventsHandler testRunEventsHandler)
        {
            this.EnsureInitialized();
            this.requestSender.StartTestRun(testCases, runSettings, testRunEventsHandler);
        }

        /// <inheritdoc/>
        public void RunTestsWithCustomTestHost(IEnumerable<string> sources, string runSettings, ITestRunEventsHandler testRunEventsHandler, ITestHostLauncher customTestHostLauncher)
        {
            this.EnsureInitialized();
            this.requestSender.StartTestRunWithCustomHost(sources, runSettings, testRunEventsHandler, customTestHostLauncher);
        }

        /// <inheritdoc/>
        public void RunTestsWithCustomTestHost(IEnumerable<TestCase> testCases, string runSettings, ITestRunEventsHandler testRunEventsHandler, ITestHostLauncher customTestHostLauncher)
        {
            this.EnsureInitialized();
            this.requestSender.StartTestRunWithCustomHost(testCases, runSettings, testRunEventsHandler, customTestHostLauncher);
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
                this.sessionStarted = this.requestSender.WaitForRequestHandlerConnection(ConnectionTimeout);

                if (this.sessionStarted)
                {
                    EqtTrace.Info("VsTestConsoleWrapper.EnsureInitialized: Send a request to initialize extensions.");
                    this.requestSender.InitializeExtensions(this.pathToAdditionalExtensions);
                }
            }

            if (!this.sessionStarted && this.requestSender != null)
            {
                EqtTrace.Info("VsTestConsoleWrapper.EnsureInitialized: Process Started. Waiting for connection to command line runner.");
                this.sessionStarted = this.requestSender.WaitForRequestHandlerConnection(ConnectionTimeout);
            }
            
            if (!this.sessionStarted)
            {
                throw new TransationLayerException("Error connecting to Vstest Command Line");
            }
        }
    }
}
