// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer
{
    using System.Collections.Generic;
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

        private const string PortArgument = "/port:{0}";

        private readonly IProcessManager vstestConsoleProcessManager;

        private readonly ITranslationLayerRequestSender requestSender;

        private bool sessionStarted;

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
                // Start Vstest.console.exe
                string args = string.Format(CultureInfo.InvariantCulture, PortArgument, port);
                this.vstestConsoleProcessManager.StartProcess(new[] { args });
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
            if (!this.sessionStarted && this.requestSender != null)
            {
                this.sessionStarted = this.requestSender.WaitForRequestHandlerConnection(ConnectionTimeout);
            }

            if (!this.sessionStarted)
            {
                throw new TransationLayerException("Error connecting to Vstest Command Line");
            }
        }
    }
}
