// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;

    /// <summary>
    /// An implementation of <see cref="IVsTestConsoleWrapperAsync"/> to invoke test operations
    /// via the <c>vstest.console</c> test runner.
    /// </summary>
    public class VsTestConsoleWrapperAsync : IVsTestConsoleWrapperAsync
    {
        #region Private Members

        private const int ConnectionTimeout = 30 * 1000;

        private readonly IProcessManager vstestConsoleProcessManager;

        private readonly ITranslationLayerRequestSenderAsync requestSender;

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
        public VsTestConsoleWrapperAsync(string vstestConsolePath) :
            this(vstestConsolePath, ConsoleParameters.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VsTestConsoleWrapper"/> class.
        /// </summary>
        /// <param name="vstestConsolePath">Path to the test runner <c>vstest.console.exe</c>.</param>
        /// <param name="consoleParameters">The parameters to be passed onto the runner process</param>
        public VsTestConsoleWrapperAsync(string vstestConsolePath, ConsoleParameters consoleParameters) :
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
        internal VsTestConsoleWrapperAsync(ITranslationLayerRequestSenderAsync requestSender, IProcessManager processManager, ConsoleParameters consoleParameters, ITestPlatformEventSource testPlatformEventSource)
        {
            this.requestSender = requestSender;
            this.vstestConsoleProcessManager = processManager;
            this.consoleParameters = consoleParameters;
            this.testPlatformEventSource = testPlatformEventSource;
            this.pathToAdditionalExtensions = new List<string>();

            this.vstestConsoleProcessManager.ProcessExited += (sender, args) => this.requestSender.OnProcessExited();
        }

        #endregion

        #region IVsTestConsoleWrapper

        /// <inheritdoc/>
        public async Task StartSessionAsync()
        {
            this.testPlatformEventSource.TranslationLayerInitializeStart();

            // Start communication
            var port = await this.requestSender.InitializeCommunicationAsync(ConnectionTimeout);

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
                throw new TransationLayerException("Error hosting communication channel and connecting to console");
            }
        }

        /// <inheritdoc/>
        public async Task InitializeExtensionsAsync(IEnumerable<string> pathToAdditionalExtensions)
        {
            await this.EnsureInitializedAsync();
            this.pathToAdditionalExtensions = pathToAdditionalExtensions.ToList();
            this.requestSender.InitializeExtensions(this.pathToAdditionalExtensions);
        }

        /// <inheritdoc/>
        public async Task DiscoverTestsAsync(IEnumerable<string> sources, string discoverySettings, ITestDiscoveryEventsHandler discoveryEventsHandler)
        {
            this.testPlatformEventSource.TranslationLayerDiscoveryStart();
            await this.EnsureInitializedAsync();

            // Converts ITestDiscoveryEventsHandler to ITestDiscoveryEventsHandler2
            var discoveryCompleteEventsHandler2 = new DiscoveryEventsHandleConverter(discoveryEventsHandler);
            await this.requestSender.DiscoverTestsAsync(sources, discoverySettings, options: null, discoveryEventsHandler: discoveryCompleteEventsHandler2);
        }


        /// <inheritdoc/>
        public async Task DiscoverTestsAsync(IEnumerable<string> sources, string discoverySettings, TestPlatformOptions options, ITestDiscoveryEventsHandler2 discoveryEventsHandler)
        {
            this.testPlatformEventSource.TranslationLayerDiscoveryStart();
            await this.EnsureInitializedAsync();
            await this.requestSender.DiscoverTestsAsync(sources, discoverySettings, options, discoveryEventsHandler);
        }

        /// <inheritdoc/>
        public void CancelDiscovery()
        {
            // TODO: Cancel Discovery
            // this.requestSender.CancelDiscovery();
        }

        /// <inheritdoc/>
        public async Task RunTestsAsync(IEnumerable<string> sources, string runSettings, ITestRunEventsHandler testRunEventsHandler)
        {
            await RunTestsAsync(sources, runSettings, null, testRunEventsHandler);
        }

        /// <inheritdoc/>
        public async Task RunTestsAsync(IEnumerable<string> sources, string runSettings, TestPlatformOptions options, ITestRunEventsHandler testRunEventsHandler)
        {
            var sourceList = sources.ToList();
            this.testPlatformEventSource.TranslationLayerExecutionStart(0, sourceList.Count, 0, runSettings ?? string.Empty);

            await this.EnsureInitializedAsync();
            await this.requestSender.StartTestRunAsync(sourceList, runSettings, options, testRunEventsHandler);
        }

        /// <inheritdoc/>
        public async Task RunTestsAsync(IEnumerable<TestCase> testCases, string runSettings, ITestRunEventsHandler testRunEventsHandler)
        {
            var testCaseList = testCases.ToList();
            this.testPlatformEventSource.TranslationLayerExecutionStart(0, 0, testCaseList.Count, runSettings ?? string.Empty);

            await this.EnsureInitializedAsync();
            await this.requestSender.StartTestRunAsync(testCaseList, runSettings, options: null, runEventsHandler: testRunEventsHandler);
        }

        /// <inheritdoc/>
        public async Task RunTestsAsync(IEnumerable<TestCase> testCases, string runSettings, TestPlatformOptions options, ITestRunEventsHandler testRunEventsHandler)
        {
            var testCaseList = testCases.ToList();
            this.testPlatformEventSource.TranslationLayerExecutionStart(0, 0, testCaseList.Count, runSettings ?? string.Empty);

            await this.EnsureInitializedAsync();
            await this.requestSender.StartTestRunAsync(testCaseList, runSettings, options, testRunEventsHandler);
        }

        /// <inheritdoc/>
        public async Task RunTestsWithCustomTestHostAsync(IEnumerable<string> sources, string runSettings, ITestRunEventsHandler testRunEventsHandler, ITestHostLauncher customTestHostLauncher)
        {
            await RunTestsWithCustomTestHostAsync(sources, runSettings, null, testRunEventsHandler, customTestHostLauncher);
        }

        /// <inheritdoc/>
        public async Task RunTestsWithCustomTestHostAsync(IEnumerable<string> sources, string runSettings, TestPlatformOptions options, ITestRunEventsHandler testRunEventsHandler, ITestHostLauncher customTestHostLauncher)
        {
            var sourceList = sources.ToList();
            this.testPlatformEventSource.TranslationLayerExecutionStart(1, sourceList.Count, 0, runSettings ?? string.Empty);

            await this.EnsureInitializedAsync();
            await this.requestSender.StartTestRunWithCustomHostAsync(sourceList, runSettings, options, testRunEventsHandler, customTestHostLauncher);
        }

        /// <inheritdoc/>
        public async Task RunTestsWithCustomTestHostAsync(IEnumerable<TestCase> testCases, string runSettings, ITestRunEventsHandler testRunEventsHandler, ITestHostLauncher customTestHostLauncher)
        {
            var testCaseList = testCases.ToList();
            this.testPlatformEventSource.TranslationLayerExecutionStart(1, 0, testCaseList.Count, runSettings ?? string.Empty);

            await this.EnsureInitializedAsync();
            await this.requestSender.StartTestRunWithCustomHostAsync(testCaseList, runSettings, options: null, runEventsHandler: testRunEventsHandler, customTestHostLauncher: customTestHostLauncher);
        }

        /// <inheritdoc/>
        public async Task RunTestsWithCustomTestHostAsync(IEnumerable<TestCase> testCases, string runSettings, TestPlatformOptions options, ITestRunEventsHandler testRunEventsHandler, ITestHostLauncher customTestHostLauncher)
        {
            var testCaseList = testCases.ToList();
            this.testPlatformEventSource.TranslationLayerExecutionStart(1, 0, testCaseList.Count, runSettings ?? string.Empty);

            await this.EnsureInitializedAsync();
            await this.requestSender.StartTestRunWithCustomHostAsync(testCaseList, runSettings, options, testRunEventsHandler, customTestHostLauncher);
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
        }

        #endregion

        private async Task EnsureInitializedAsync()
        {
            if (!this.vstestConsoleProcessManager.IsProcessInitialized())
            {
                EqtTrace.Info("VsTestConsoleWrapper.EnsureInitializedAsync: Process is not started.");
                await this.StartSessionAsync();

                EqtTrace.Info("VsTestConsoleWrapper.EnsureInitializedAsync: Send a request to initialize extensions.");
                this.requestSender.InitializeExtensions(this.pathToAdditionalExtensions);
            }
        }

    }
}
