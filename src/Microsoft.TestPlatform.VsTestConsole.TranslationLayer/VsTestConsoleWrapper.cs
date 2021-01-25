// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;

    using CommunicationUtilitiesResources = Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Resources.Resources;
    using CoreUtilitiesConstants = Microsoft.VisualStudio.TestPlatform.CoreUtilities.Constants;

    /// <summary>
    /// An implementation of <see cref="IVsTestConsoleWrapper"/> to invoke test operations
    /// via the <c>vstest.console</c> test runner.
    /// </summary>
    public class VsTestConsoleWrapper : IVsTestConsoleWrapper
    {
        #region Private Members

        private readonly IProcessManager vstestConsoleProcessManager;

        private readonly ITranslationLayerRequestSender requestSender;

        private readonly IProcessHelper processHelper;

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
        /// 
        /// <param name="vstestConsolePath">
        /// Path to the test runner <c>vstest.console.exe</c>.
        /// </param>
        public VsTestConsoleWrapper(
            string vstestConsolePath)
            : this(
                  vstestConsolePath,
                  ConsoleParameters.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VsTestConsoleWrapper"/> class.
        /// </summary>
        /// 
        /// <param name="vstestConsolePath">Path to the test runner <c>vstest.console.exe</c>.</param>
        /// <param name="consoleParameters">The parameters to be passed onto the runner process.</param>
        public VsTestConsoleWrapper(
            string vstestConsolePath,
            ConsoleParameters consoleParameters)
            : this(
                  new VsTestConsoleRequestSender(),
                  new VsTestConsoleProcessManager(vstestConsolePath),
                  consoleParameters,
                  TestPlatformEventSource.Instance,
                  new ProcessHelper())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VsTestConsoleWrapper"/> class.
        /// </summary>
        /// 
        /// <remarks>Defined for testing purposes.</remarks>
        /// 
        /// <param name="vstestConsolePath">Path to the test runner <c>vstest.console.exe</c>.</param>
        /// <param name="dotnetExePath">Path to dotnet exe, needed for CI builds.</param>
        /// <param name="consoleParameters">The parameters to be passed onto the runner process.</param>
        internal VsTestConsoleWrapper(
            string vstestConsolePath,
            string dotnetExePath,
            ConsoleParameters consoleParameters)
            : this(
                  new VsTestConsoleRequestSender(),
                  new VsTestConsoleProcessManager(vstestConsolePath, dotnetExePath),
                  consoleParameters,
                  TestPlatformEventSource.Instance,
                  new ProcessHelper())
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VsTestConsoleWrapper"/> class.
        /// </summary>
        /// 
        /// <param name="requestSender">Sender for test messages.</param>
        /// <param name="processManager">Process manager.</param>
        /// <param name="consoleParameters">The parameters to be passed onto the runner process.</param>
        /// <param name="testPlatformEventSource">Performance event source.</param>
        /// <param name="processHelper">Helper for process related utilities.</param>
        internal VsTestConsoleWrapper(
            ITranslationLayerRequestSender requestSender,
            IProcessManager processManager,
            ConsoleParameters consoleParameters,
            ITestPlatformEventSource testPlatformEventSource,
            IProcessHelper processHelper)
        {
            this.requestSender = requestSender;
            this.vstestConsoleProcessManager = processManager;
            this.consoleParameters = consoleParameters;
            this.testPlatformEventSource = testPlatformEventSource;
            this.processHelper = processHelper;
            this.pathToAdditionalExtensions = new List<string>();

            this.vstestConsoleProcessManager.ProcessExited += (sender, args) => this.requestSender.OnProcessExited();
            this.sessionStarted = false;
        }

        #endregion

        #region IVsTestConsoleWrapper

        /// <inheritdoc/>
        public void StartSession()
        {
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("VsTestConsoleWrapper.StartSession: Starting VsTestConsoleWrapper session.");
            }

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
        public ITestSession StartTestSession(
            IList<string> sources,
            string runSettings,
            ITestSessionEventsHandler eventsHandler)
        {
            return this.StartTestSession(
                sources,
                runSettings,
                options: null,
                eventsHandler);
        }

        /// <inheritdoc/>
        public ITestSession StartTestSession(
            IList<string> sources,
            string runSettings,
            TestPlatformOptions options,
            ITestSessionEventsHandler eventsHandler)
        {
            return this.StartTestSession(
                sources,
                runSettings,
                options,
                eventsHandler,
                testHostLauncher: null);
        }

        /// <inheritdoc/>
        public ITestSession StartTestSession(
            IList<string> sources,
            string runSettings,
            TestPlatformOptions options,
            ITestSessionEventsHandler eventsHandler,
            ITestHostLauncher testHostLauncher)
        {
            this.testPlatformEventSource.TranslationLayerStartTestSessionStart();

            this.EnsureInitialized();
            return new TestSession(
                this.requestSender.StartTestSession(
                    sources,
                    runSettings,
                    options,
                    eventsHandler,
                    testHostLauncher),
                eventsHandler,
                this);
        }

        /// <inheritdoc/>
        public bool StopTestSession(
            TestSessionInfo testSessionInfo,
            ITestSessionEventsHandler eventsHandler)
        {
            this.testPlatformEventSource.TranslationLayerStopTestSessionStart();

            this.EnsureInitialized();
            return this.requestSender.StopTestSession(
                testSessionInfo,
                eventsHandler);
        }

        /// <inheritdoc/>
        public void InitializeExtensions(IEnumerable<string> pathToAdditionalExtensions)
        {
            this.EnsureInitialized();

            this.pathToAdditionalExtensions = pathToAdditionalExtensions.ToList();
            this.requestSender.InitializeExtensions(this.pathToAdditionalExtensions);
        }

        /// <inheritdoc/>
        public void DiscoverTests(
            IEnumerable<string> sources,
            string discoverySettings,
            ITestDiscoveryEventsHandler discoveryEventsHandler)
        {
            this.DiscoverTests(
                sources,
                discoverySettings,
                options: null,
                discoveryEventsHandler: new DiscoveryEventsHandleConverter(discoveryEventsHandler));
        }

        /// <inheritdoc/>
        public void DiscoverTests(
            IEnumerable<string> sources,
            string discoverySettings,
            TestPlatformOptions options,
            ITestDiscoveryEventsHandler2 discoveryEventsHandler)
        {
            this.DiscoverTests(
                sources,
                discoverySettings,
                options,
                testSessionInfo: null,
                discoveryEventsHandler);
        }

        /// <inheritdoc/>
        public void DiscoverTests(
            IEnumerable<string> sources,
            string discoverySettings,
            TestPlatformOptions options,
            TestSessionInfo testSessionInfo,
            ITestDiscoveryEventsHandler2 discoveryEventsHandler)
        {
            this.testPlatformEventSource.TranslationLayerDiscoveryStart();

            this.EnsureInitialized();
            this.requestSender.DiscoverTests(
                sources,
                discoverySettings,
                options,
                testSessionInfo,
                discoveryEventsHandler);
        }

        /// <inheritdoc/>
        public void CancelDiscovery()
        {
            this.requestSender.CancelDiscovery();
        }

        /// <inheritdoc/>
        public void RunTests(
            IEnumerable<string> sources,
            string runSettings,
            ITestRunEventsHandler testRunEventsHandler)
        {
            this.RunTests(
                sources,
                runSettings,
                options: null,
                testRunEventsHandler: testRunEventsHandler);
        }

        /// <inheritdoc/>
        public void RunTests(
            IEnumerable<string> sources,
            string runSettings,
            TestPlatformOptions options,
            ITestRunEventsHandler testRunEventsHandler)
        {
            this.RunTests(
                sources,
                runSettings,
                options,
                testSessionInfo: null,
                testRunEventsHandler);
        }

        /// <inheritdoc/>
        public void RunTests(
            IEnumerable<string> sources,
            string runSettings,
            TestPlatformOptions options,
            TestSessionInfo testSessionInfo,
            ITestRunEventsHandler testRunEventsHandler)
        {
            var sourceList = sources.ToList();
            this.testPlatformEventSource.TranslationLayerExecutionStart(
                0,
                sourceList.Count,
                0,
                runSettings ?? string.Empty);

            this.EnsureInitialized();
            this.requestSender.StartTestRun(
                sourceList,
                runSettings,
                options,
                testSessionInfo,
                testRunEventsHandler);
        }

        /// <inheritdoc/>
        public void RunTests(
            IEnumerable<TestCase> testCases,
            string runSettings,
            ITestRunEventsHandler testRunEventsHandler)
        {
            this.RunTests(
                testCases,
                runSettings,
                options: null,
                testRunEventsHandler);
        }

        /// <inheritdoc/>
        public void RunTests(
            IEnumerable<TestCase> testCases,
            string runSettings,
            TestPlatformOptions options,
            ITestRunEventsHandler testRunEventsHandler)
        {
            this.RunTests(
                testCases,
                runSettings,
                options,
                testSessionInfo: null,
                testRunEventsHandler);
        }

        /// <inheritdoc/>
        public void RunTests(
            IEnumerable<TestCase> testCases,
            string runSettings,
            TestPlatformOptions options,
            TestSessionInfo testSessionInfo,
            ITestRunEventsHandler testRunEventsHandler)
        {
            var testCaseList = testCases.ToList();
            this.testPlatformEventSource.TranslationLayerExecutionStart(
                0,
                0,
                testCaseList.Count,
                runSettings ?? string.Empty);

            this.EnsureInitialized();
            this.requestSender.StartTestRun(
                testCaseList,
                runSettings,
                options,
                testSessionInfo,
                testRunEventsHandler);
        }

        /// <inheritdoc/>
        public void RunTestsWithCustomTestHost(
            IEnumerable<string> sources,
            string runSettings,
            ITestRunEventsHandler testRunEventsHandler,
            ITestHostLauncher customTestHostLauncher)
        {
            this.RunTestsWithCustomTestHost(
                sources,
                runSettings,
                options: null,
                testRunEventsHandler: testRunEventsHandler,
                customTestHostLauncher: customTestHostLauncher);
        }

        /// <inheritdoc/>
        public void RunTestsWithCustomTestHost(
            IEnumerable<string> sources,
            string runSettings,
            TestPlatformOptions options,
            ITestRunEventsHandler testRunEventsHandler,
            ITestHostLauncher customTestHostLauncher)
        {
            this.RunTestsWithCustomTestHost(
                sources,
                runSettings,
                options,
                testSessionInfo: null,
                testRunEventsHandler,
                customTestHostLauncher);
        }

        /// <inheritdoc/>
        public void RunTestsWithCustomTestHost(
            IEnumerable<string> sources,
            string runSettings,
            TestPlatformOptions options,
            TestSessionInfo testSessionInfo,
            ITestRunEventsHandler testRunEventsHandler,
            ITestHostLauncher customTestHostLauncher)
        {
            var sourceList = sources.ToList();
            this.testPlatformEventSource.TranslationLayerExecutionStart(
                1,
                sourceList.Count,
                0,
                runSettings ?? string.Empty);

            this.EnsureInitialized();
            this.requestSender.StartTestRunWithCustomHost(
                sourceList,
                runSettings,
                options,
                testSessionInfo,
                testRunEventsHandler,
                customTestHostLauncher);
        }

        /// <inheritdoc/>
        public void RunTestsWithCustomTestHost(
            IEnumerable<TestCase> testCases,
            string runSettings,
            ITestRunEventsHandler testRunEventsHandler,
            ITestHostLauncher customTestHostLauncher)
        {
            this.RunTestsWithCustomTestHost(
                testCases,
                runSettings,
                options: null,
                testRunEventsHandler,
                customTestHostLauncher);
        }

        /// <inheritdoc/>
        public void RunTestsWithCustomTestHost(
            IEnumerable<TestCase> testCases,
            string runSettings,
            TestPlatformOptions options,
            ITestRunEventsHandler testRunEventsHandler,
            ITestHostLauncher customTestHostLauncher)
        {
            this.RunTestsWithCustomTestHost(
                testCases,
                runSettings,
                options,
                testSessionInfo: null,
                testRunEventsHandler,
                customTestHostLauncher);
        }

        /// <inheritdoc/>
        public void RunTestsWithCustomTestHost(
            IEnumerable<TestCase> testCases,
            string runSettings,
            TestPlatformOptions options,
            TestSessionInfo testSessionInfo,
            ITestRunEventsHandler testRunEventsHandler,
            ITestHostLauncher customTestHostLauncher)
        {
            var testCaseList = testCases.ToList();
            this.testPlatformEventSource.TranslationLayerExecutionStart(
                1,
                0,
                testCaseList.Count,
                runSettings ?? string.Empty);

            this.EnsureInitialized();
            this.requestSender.StartTestRunWithCustomHost(
                testCaseList,
                runSettings,
                options,
                testSessionInfo,
                testRunEventsHandler,
                customTestHostLauncher);
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
            EqtTrace.Info("VsTestConsoleWrapper.EndSession: Ending VsTestConsoleWrapper session");

            this.requestSender.EndSession();
            this.requestSender.Close();

            // If vstest.console is still hanging around, it should be explicitly killed.
            this.vstestConsoleProcessManager.ShutdownProcess();

            this.sessionStarted = false;
        }

        #endregion

        #region IVsTestConsoleWrapperAsync

        /// <inheritdoc/>
        public async Task StartSessionAsync()
        {
            EqtTrace.Info("VsTestConsoleWrapperAsync.StartSessionAsync: Starting VsTestConsoleWrapper session");

            this.testPlatformEventSource.TranslationLayerInitializeStart();

            var timeout = EnvironmentHelper.GetConnectionTimeout();
            // Start communication
            var port = await this.requestSender.InitializeCommunicationAsync(timeout * 1000).ConfigureAwait(false);

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
        public async Task<ITestSession> StartTestSessionAsync(
            IList<string> sources,
            string runSettings,
            ITestSessionEventsHandler eventsHandler)
        {
            return await this.StartTestSessionAsync(
                sources,
                runSettings,
                options: null,
                eventsHandler).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ITestSession> StartTestSessionAsync(
            IList<string> sources,
            string runSettings,
            TestPlatformOptions options,
            ITestSessionEventsHandler eventsHandler)
        {
            return await this.StartTestSessionAsync(
                sources,
                runSettings,
                options: null,
                eventsHandler,
                testHostLauncher: null).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ITestSession> StartTestSessionAsync(
            IList<string> sources,
            string runSettings,
            TestPlatformOptions options,
            ITestSessionEventsHandler eventsHandler,
            ITestHostLauncher testHostLauncher)
        {
            this.testPlatformEventSource.TranslationLayerStartTestSessionStart();

            await this.EnsureInitializedAsync().ConfigureAwait(false);
            return new TestSession(
                await this.requestSender.StartTestSessionAsync(
                    sources,
                    runSettings,
                    options,
                    eventsHandler,
                    testHostLauncher).ConfigureAwait(false),
                eventsHandler,
                this);
        }

        /// <inheritdoc/>
        public async Task<bool> StopTestSessionAsync(
            TestSessionInfo testSessionInfo,
            ITestSessionEventsHandler eventsHandler)
        {
            this.testPlatformEventSource.TranslationLayerStopTestSessionStart();

            await this.EnsureInitializedAsync().ConfigureAwait(false);
            return await this.requestSender.StopTestSessionAsync(
                testSessionInfo,
                eventsHandler).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task InitializeExtensionsAsync(IEnumerable<string> pathToAdditionalExtensions)
        {
            await this.EnsureInitializedAsync().ConfigureAwait(false);
            this.pathToAdditionalExtensions = pathToAdditionalExtensions.ToList();
            this.requestSender.InitializeExtensions(this.pathToAdditionalExtensions);
        }

        /// <inheritdoc/>
        public async Task DiscoverTestsAsync(
            IEnumerable<string> sources,
            string discoverySettings,
            ITestDiscoveryEventsHandler discoveryEventsHandler)
        {
            await this.DiscoverTestsAsync(
                    sources,
                    discoverySettings,
                    options: null,
                    discoveryEventsHandler: new DiscoveryEventsHandleConverter(discoveryEventsHandler))
                .ConfigureAwait(false);
        }


        /// <inheritdoc/>
        public async Task DiscoverTestsAsync(
            IEnumerable<string> sources,
            string discoverySettings,
            TestPlatformOptions options,
            ITestDiscoveryEventsHandler2 discoveryEventsHandler)
        {
            await this.DiscoverTestsAsync(
                sources,
                discoverySettings,
                options,
                testSessionInfo: null,
                discoveryEventsHandler).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task DiscoverTestsAsync(
            IEnumerable<string> sources,
            string discoverySettings,
            TestPlatformOptions options,
            TestSessionInfo testSessionInfo,
            ITestDiscoveryEventsHandler2 discoveryEventsHandler)
        {
            this.testPlatformEventSource.TranslationLayerDiscoveryStart();

            await this.EnsureInitializedAsync().ConfigureAwait(false);
            await this.requestSender.DiscoverTestsAsync(
                sources,
                discoverySettings,
                options,
                testSessionInfo,
                discoveryEventsHandler).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task RunTestsAsync(
            IEnumerable<string> sources,
            string runSettings,
            ITestRunEventsHandler testRunEventsHandler)
        {
            await this.RunTestsAsync(
                sources,
                runSettings,
                options: null,
                testRunEventsHandler).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task RunTestsAsync(
            IEnumerable<string> sources,
            string runSettings,
            TestPlatformOptions options,
            ITestRunEventsHandler testRunEventsHandler)
        {
            await this.RunTestsAsync(
                sources,
                runSettings,
                options,
                testSessionInfo: null,
                testRunEventsHandler).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task RunTestsAsync(
            IEnumerable<string> sources,
            string runSettings,
            TestPlatformOptions options,
            TestSessionInfo testSessionInfo,
            ITestRunEventsHandler testRunEventsHandler)
        {
            var sourceList = sources.ToList();
            this.testPlatformEventSource.TranslationLayerExecutionStart(
                0,
                sourceList.Count,
                0,
                runSettings ?? string.Empty);

            await this.EnsureInitializedAsync().ConfigureAwait(false);
            await this.requestSender.StartTestRunAsync(
                sourceList,
                runSettings,
                options,
                testSessionInfo,
                testRunEventsHandler).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task RunTestsAsync(
            IEnumerable<TestCase> testCases,
            string runSettings,
            ITestRunEventsHandler testRunEventsHandler)
        {
            await this.RunTestsAsync(
                testCases,
                runSettings,
                options: null,
                testRunEventsHandler).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task RunTestsAsync(
            IEnumerable<TestCase> testCases,
            string runSettings,
            TestPlatformOptions options,
            ITestRunEventsHandler testRunEventsHandler)
        {
            await this.RunTestsAsync(
                testCases,
                runSettings,
                options,
                testSessionInfo: null,
                testRunEventsHandler).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task RunTestsAsync(
            IEnumerable<TestCase> testCases,
            string runSettings,
            TestPlatformOptions options,
            TestSessionInfo testSessionInfo,
            ITestRunEventsHandler testRunEventsHandler)
        {
            var testCaseList = testCases.ToList();
            this.testPlatformEventSource.TranslationLayerExecutionStart(
                0,
                0,
                testCaseList.Count,
                runSettings ?? string.Empty);

            await this.EnsureInitializedAsync().ConfigureAwait(false);
            await this.requestSender.StartTestRunAsync(
                testCaseList,
                runSettings,
                options,
                testSessionInfo,
                testRunEventsHandler).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task RunTestsWithCustomTestHostAsync(
            IEnumerable<string> sources,
            string runSettings,
            ITestRunEventsHandler testRunEventsHandler,
            ITestHostLauncher customTestHostLauncher)
        {
            await this.RunTestsWithCustomTestHostAsync(
                sources,
                runSettings,
                options: null,
                testRunEventsHandler,
                customTestHostLauncher).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task RunTestsWithCustomTestHostAsync(
            IEnumerable<string> sources,
            string runSettings,
            TestPlatformOptions options,
            ITestRunEventsHandler testRunEventsHandler,
            ITestHostLauncher customTestHostLauncher)
        {
            await this.RunTestsWithCustomTestHostAsync(
                sources,
                runSettings,
                options,
                testSessionInfo: null,
                testRunEventsHandler,
                customTestHostLauncher).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task RunTestsWithCustomTestHostAsync(
            IEnumerable<string> sources,
            string runSettings,
            TestPlatformOptions options,
            TestSessionInfo testSessionInfo,
            ITestRunEventsHandler testRunEventsHandler,
            ITestHostLauncher customTestHostLauncher)
        {
            var sourceList = sources.ToList();
            this.testPlatformEventSource.TranslationLayerExecutionStart(
                1,
                sourceList.Count,
                0,
                runSettings ?? string.Empty);

            await this.EnsureInitializedAsync().ConfigureAwait(false);
            await this.requestSender.StartTestRunWithCustomHostAsync(
                sourceList,
                runSettings,
                options,
                testSessionInfo,
                testRunEventsHandler,
                customTestHostLauncher).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task RunTestsWithCustomTestHostAsync(
            IEnumerable<TestCase> testCases,
            string runSettings,
            ITestRunEventsHandler testRunEventsHandler,
            ITestHostLauncher customTestHostLauncher)
        {
            await this.RunTestsWithCustomTestHostAsync(
                testCases,
                runSettings,
                options: null,
                testRunEventsHandler,
                customTestHostLauncher).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task RunTestsWithCustomTestHostAsync(
            IEnumerable<TestCase> testCases,
            string runSettings, 
            TestPlatformOptions options,
            ITestRunEventsHandler testRunEventsHandler,
            ITestHostLauncher customTestHostLauncher)
        {
            await this.RunTestsWithCustomTestHostAsync(
                testCases,
                runSettings,
                options,
                testSessionInfo: null,
                testRunEventsHandler,
                customTestHostLauncher).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task RunTestsWithCustomTestHostAsync(
            IEnumerable<TestCase> testCases,
            string runSettings,
            TestPlatformOptions options,
            TestSessionInfo testSessionInfo,
            ITestRunEventsHandler testRunEventsHandler,
            ITestHostLauncher customTestHostLauncher)
        {
            var testCaseList = testCases.ToList();
            this.testPlatformEventSource.TranslationLayerExecutionStart(
                1,
                0,
                testCaseList.Count,
                runSettings ?? string.Empty);

            await this.EnsureInitializedAsync().ConfigureAwait(false);
            await this.requestSender.StartTestRunWithCustomHostAsync(
                testCaseList,
                runSettings,
                options,
                testSessionInfo,
                testRunEventsHandler,
                customTestHostLauncher).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task ProcessTestRunAttachmentsAsync(
            IEnumerable<AttachmentSet> attachments,
            string processingSettings,
            bool isLastBatch,
            bool collectMetrics,
            ITestRunAttachmentsProcessingEventsHandler testSessionEventsHandler,
            CancellationToken cancellationToken)
        {
            this.testPlatformEventSource.TranslationLayerTestRunAttachmentsProcessingStart();

            await this.EnsureInitializedAsync().ConfigureAwait(false);
            await requestSender.ProcessTestRunAttachmentsAsync(
                attachments,
                collectMetrics,
                testSessionEventsHandler,
                cancellationToken).ConfigureAwait(false);
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
        }

        private async Task EnsureInitializedAsync()
        {
            if (!this.vstestConsoleProcessManager.IsProcessInitialized())
            {
                EqtTrace.Info("VsTestConsoleWrapper.EnsureInitializedAsync: Process is not started.");
                await this.StartSessionAsync().ConfigureAwait(false);

                EqtTrace.Info("VsTestConsoleWrapper.EnsureInitializedAsync: Send a request to initialize extensions.");
                this.requestSender.InitializeExtensions(this.pathToAdditionalExtensions);
            }
        }

        private bool WaitForConnection()
        {
            EqtTrace.Info("VsTestConsoleWrapper.WaitForConnection: Waiting for connection to command line runner.");

            var timeout = EnvironmentHelper.GetConnectionTimeout();
            if (!this.requestSender.WaitForRequestHandlerConnection(timeout * 1000))
            {
                var processName = this.processHelper.GetCurrentProcessFileName();
                throw new TransationLayerException(
                    string.Format(
                        CultureInfo.CurrentUICulture,
                        CommunicationUtilitiesResources.ConnectionTimeoutErrorMessage,
                        processName,
                        CoreUtilitiesConstants.VstestConsoleProcessName,
                        timeout,
                        EnvironmentHelper.VstestConnectionTimeout)
                    );
            }

            this.testPlatformEventSource.TranslationLayerInitializeStop();
            return true;
        }
    }
}
