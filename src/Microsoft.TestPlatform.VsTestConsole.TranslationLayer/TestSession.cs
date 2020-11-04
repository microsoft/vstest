// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;

    /// <summary>
    /// Defines a test session object that can be used to make calls to the vstest.console
    /// process.
    /// </summary>
    public class TestSession : ITestSession
    {
        private TestSessionInfo testSessionInfo;
        private VsTestConsoleWrapper consoleWrapper;
        private IList<string> sources;
        private string runSettings;

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TestSession"/> class.
        /// </summary>
        /// 
        /// <param name="testSessionInfo">The test session info object.</param>
        /// <param name="consoleWrapper">The encapsulated console wrapper.</param>
        public TestSession(
            TestSessionInfo testSessionInfo,
            VsTestConsoleWrapper consoleWrapper)
            : this(testSessionInfo, consoleWrapper, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestSession"/> class.
        /// </summary>
        /// 
        /// <param name="testSessionInfo">The test session info object.</param>
        /// <param name="consoleWrapper">The encapsulated console wrapper.</param>
        /// <param name="sources">The list of source files used to initialize the session.</param>
        /// <param name="runSettings">The run settings used to initialize the session.</param>
        public TestSession(
            TestSessionInfo testSessionInfo,
            VsTestConsoleWrapper consoleWrapper,
            IList<string> sources,
            string runSettings)
        {
            this.testSessionInfo = testSessionInfo;
            this.consoleWrapper = consoleWrapper;
            this.sources = sources;
            this.runSettings = runSettings;
        }
        #endregion

        #region ITestSession
        /// <inheritdoc/>
        public void AbortTestRun()
        {
            this.consoleWrapper.AbortTestRun();
        }

        /// <inheritdoc/>
        public void CancelDiscovery()
        {
            this.consoleWrapper.CancelDiscovery();
        }

        /// <inheritdoc/>
        public void CancelTestRun()
        {
            this.consoleWrapper.CancelTestRun();
        }

        /// <inheritdoc/>
        public void DiscoverTests(
            ITestDiscoveryEventsHandler discoveryEventsHandler)
        {
            this.DiscoverTests(
                options: null,
                discoveryEventsHandler);
        }

        /// <inheritdoc/>
        public void DiscoverTests(
            ITestDiscoveryEventsHandler2 discoveryEventsHandler)
        {
            this.DiscoverTests(
                options: null,
                discoveryEventsHandler);
        }

        /// <inheritdoc/>
        public void DiscoverTests(
            TestPlatformOptions options,
            ITestDiscoveryEventsHandler discoveryEventsHandler)
        {
            this.DiscoverTests(
                this.sources,
                this.runSettings,
                options,
                discoveryEventsHandler: new DiscoveryEventsHandleConverter(discoveryEventsHandler));
        }

        /// <inheritdoc/>
        public void DiscoverTests(
            TestPlatformOptions options,
            ITestDiscoveryEventsHandler2 discoveryEventsHandler)
        {
            this.DiscoverTests(
                this.sources,
                this.runSettings,
                options,
                discoveryEventsHandler);
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
            // TODO (copoiena): Hook into the wrapper and pass session info here.
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        public void RunTests(
            ITestRunEventsHandler testRunEventsHandler)
        {
            this.RunTests(
                this.sources,
                this.runSettings,
                testRunEventsHandler);
        }

        /// <inheritdoc/>
        public void RunTests(
            TestPlatformOptions options,
            ITestRunEventsHandler testRunEventsHandler)
        {
            this.RunTests(
                this.sources,
                this.runSettings,
                options,
                testRunEventsHandler);
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
                testRunEventsHandler);
        }

        /// <inheritdoc/>
        public void RunTests(
            IEnumerable<string> sources,
            string runSettings,
            TestPlatformOptions options,
            ITestRunEventsHandler testRunEventsHandler)
        {
            this.consoleWrapper.RunTests(
                sources,
                runSettings,
                options,
                this.testSessionInfo,
                testRunEventsHandler);
        }

        /// <inheritdoc/>
        public void RunTests(
            IEnumerable<TestCase> testCases,
            ITestRunEventsHandler testRunEventsHandler)
        {
            this.RunTests(
                testCases,
                this.runSettings,
                testRunEventsHandler);
        }

        /// <inheritdoc/>
        public void RunTests(
            IEnumerable<TestCase> testCases,
            TestPlatformOptions options,
            ITestRunEventsHandler testRunEventsHandler)
        {
            this.RunTests(
                testCases,
                this.runSettings,
                options,
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
            this.consoleWrapper.RunTests(
                testCases,
                runSettings,
                options,
                this.testSessionInfo,
                testRunEventsHandler);
        }

        /// <inheritdoc/>
        public void RunTestsWithCustomTestHost(
            ITestRunEventsHandler testRunEventsHandler,
            ITestHostLauncher customTestHostLauncher)
        {
            this.RunTestsWithCustomTestHost(
                options: null,
                testRunEventsHandler,
                customTestHostLauncher);
        }

        /// <inheritdoc/>
        public void RunTestsWithCustomTestHost(
            TestPlatformOptions options,
            ITestRunEventsHandler testRunEventsHandler,
            ITestHostLauncher customTestHostLauncher)
        {
            this.RunTestsWithCustomTestHost(
                this.sources,
                this.runSettings,
                options,
                testRunEventsHandler,
                customTestHostLauncher);
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
                testRunEventsHandler,
                customTestHostLauncher);
        }

        /// <inheritdoc/>
        public void RunTestsWithCustomTestHost(
            IEnumerable<string> sources,
            string runSettings,
            TestPlatformOptions options,
            ITestRunEventsHandler testRunEventsHandler,
            ITestHostLauncher customTestHostLauncher)
        {
            this.consoleWrapper.RunTestsWithCustomTestHost(
                sources,
                runSettings,
                options,
                this.testSessionInfo,
                testRunEventsHandler,
                customTestHostLauncher);
        }

        /// <inheritdoc/>
        public void RunTestsWithCustomTestHost(
            IEnumerable<TestCase> testCases,
            ITestRunEventsHandler testRunEventsHandler,
            ITestHostLauncher customTestHostLauncher)
        {
            this.RunTestsWithCustomTestHost(
                testCases,
                options: null,
                testRunEventsHandler,
                customTestHostLauncher);
        }

        /// <inheritdoc/>
        public void RunTestsWithCustomTestHost(
            IEnumerable<TestCase> testCases,
            TestPlatformOptions options,
            ITestRunEventsHandler testRunEventsHandler,
            ITestHostLauncher customTestHostLauncher)
        {
            this.RunTestsWithCustomTestHost(
                testCases,
                this.runSettings,
                options,
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
            this.consoleWrapper.RunTestsWithCustomTestHost(
                testCases,
                runSettings,
                options,
                this.testSessionInfo,
                testRunEventsHandler,
                customTestHostLauncher);
        }

        /// <inheritdoc/>
        public bool StopTestSession(ITestSessionEventsHandler eventsHandler)
        {
            return this.consoleWrapper.StopTestSession(
                this.testSessionInfo,
                eventsHandler);
        }
        #endregion

        #region ITestSessionAsync
        /// <inheritdoc/>
        public async Task DiscoverTestsAsync(
            ITestDiscoveryEventsHandler discoveryEventsHandler)
        {
            await this.DiscoverTestsAsync(
                options: null,
                discoveryEventsHandler);
        }

        /// <inheritdoc/>
        public async Task DiscoverTestsAsync(
            ITestDiscoveryEventsHandler2 discoveryEventsHandler)
        {
            await this.DiscoverTestsAsync(
                options: null,
                discoveryEventsHandler);
        }

        /// <inheritdoc/>
        public async Task DiscoverTestsAsync(
            TestPlatformOptions options,
            ITestDiscoveryEventsHandler discoveryEventsHandler)
        {
            await this.DiscoverTestsAsync(
                this.sources,
                this.runSettings,
                options,
                discoveryEventsHandler: new DiscoveryEventsHandleConverter(discoveryEventsHandler));
        }

        /// <inheritdoc/>
        public async Task DiscoverTestsAsync(
            TestPlatformOptions options,
            ITestDiscoveryEventsHandler2 discoveryEventsHandler)
        {
            await this.DiscoverTestsAsync(
                this.sources,
                this.runSettings,
                options,
                discoveryEventsHandler);
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
                discoveryEventsHandler: new DiscoveryEventsHandleConverter(discoveryEventsHandler));
        }

        /// <inheritdoc/>
        public Task DiscoverTestsAsync(
            IEnumerable<string> sources,
            string discoverySettings,
            TestPlatformOptions options,
            ITestDiscoveryEventsHandler2 discoveryEventsHandler)
        {
            // TODO (copoiena): Hook into the wrapper and pass session info here.
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task RunTestsAsync(
            ITestRunEventsHandler testRunEventsHandler)
        {
            await this.RunTestsAsync(
                options: null,
                testRunEventsHandler);
        }

        /// <inheritdoc/>
        public async Task RunTestsAsync(
            TestPlatformOptions options,
            ITestRunEventsHandler testRunEventsHandler)
        {
            await this.RunTestsAsync(
                this.sources,
                this.runSettings,
                options,
                testRunEventsHandler);
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
                testRunEventsHandler);
        }

        /// <inheritdoc/>
        public async Task RunTestsAsync(
            IEnumerable<string> sources,
            string runSettings,
            TestPlatformOptions options,
            ITestRunEventsHandler testRunEventsHandler)
        {
            await this.consoleWrapper.RunTestsAsync(
                sources,
                runSettings,
                options,
                this.testSessionInfo,
                testRunEventsHandler);
        }

        /// <inheritdoc/>
        public async Task RunTestsAsync(
            IEnumerable<TestCase> testCases,
            ITestRunEventsHandler testRunEventsHandler)
        {
            await this.RunTestsAsync(
                testCases,
                options: null,
                testRunEventsHandler);
        }

        /// <inheritdoc/>
        public async Task RunTestsAsync(
            IEnumerable<TestCase> testCases,
            TestPlatformOptions options,
            ITestRunEventsHandler testRunEventsHandler)
        {
            await this.RunTestsAsync(
                testCases,
                this.runSettings,
                options,
                testRunEventsHandler);
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
                testRunEventsHandler);
        }

        /// <inheritdoc/>
        public async Task RunTestsAsync(
            IEnumerable<TestCase> testCases,
            string runSettings,
            TestPlatformOptions options,
            ITestRunEventsHandler testRunEventsHandler)
        {
            await this.consoleWrapper.RunTestsAsync(
                testCases,
                runSettings,
                options,
                this.testSessionInfo,
                testRunEventsHandler);
        }

        /// <inheritdoc/>
        public async Task RunTestsWithCustomTestHostAsync(
            ITestRunEventsHandler testRunEventsHandler,
            ITestHostLauncher customTestHostLauncher)
        {
            await this.RunTestsWithCustomTestHostAsync(
                options: null,
                testRunEventsHandler,
                customTestHostLauncher);
        }

        /// <inheritdoc/>
        public async Task RunTestsWithCustomTestHostAsync(
            TestPlatformOptions options,
            ITestRunEventsHandler testRunEventsHandler,
            ITestHostLauncher customTestHostLauncher)
        {
            await this.RunTestsWithCustomTestHostAsync(
                this.sources,
                this.runSettings,
                options,
                testRunEventsHandler,
                customTestHostLauncher);
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
                customTestHostLauncher);
        }

        /// <inheritdoc/>
        public async Task RunTestsWithCustomTestHostAsync(
            IEnumerable<string> sources,
            string runSettings,
            TestPlatformOptions options,
            ITestRunEventsHandler testRunEventsHandler,
            ITestHostLauncher customTestHostLauncher)
        {
            await this.consoleWrapper.RunTestsWithCustomTestHostAsync(
                sources,
                runSettings,
                options,
                this.testSessionInfo,
                testRunEventsHandler,
                customTestHostLauncher);
        }

        /// <inheritdoc/>
        public async Task RunTestsWithCustomTestHostAsync(
            IEnumerable<TestCase> testCases,
            ITestRunEventsHandler testRunEventsHandler,
            ITestHostLauncher customTestHostLauncher)
        {
            await this.RunTestsWithCustomTestHostAsync(
                testCases,
                options: null,
                testRunEventsHandler,
                customTestHostLauncher);
        }

        /// <inheritdoc/>
        public async Task RunTestsWithCustomTestHostAsync(
            IEnumerable<TestCase> testCases,
            TestPlatformOptions options,
            ITestRunEventsHandler testRunEventsHandler,
            ITestHostLauncher customTestHostLauncher)
        {
            await this.RunTestsWithCustomTestHostAsync(
                testCases,
                this.runSettings,
                options,
                testRunEventsHandler,
                customTestHostLauncher);
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
                customTestHostLauncher);
        }

        /// <inheritdoc/>
        public async Task RunTestsWithCustomTestHostAsync(
            IEnumerable<TestCase> testCases,
            string runSettings,
            TestPlatformOptions options,
            ITestRunEventsHandler testRunEventsHandler,
            ITestHostLauncher customTestHostLauncher)
        {
            await this.consoleWrapper.RunTestsWithCustomTestHostAsync(
                testCases,
                runSettings,
                options,
                this.testSessionInfo,
                testRunEventsHandler,
                customTestHostLauncher);
        }

        /// <inheritdoc/>
        public async Task<bool> StopTestSessionAsync(ITestSessionEventsHandler eventsHandler)
        {
            return await this.consoleWrapper.StopTestSessionAsync(
                this.testSessionInfo,
                eventsHandler);
        }
        #endregion
    }
}
