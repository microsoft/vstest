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
        {
            this.testSessionInfo = testSessionInfo;
            this.consoleWrapper = consoleWrapper;
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
            this.consoleWrapper.DiscoverTests(
                sources,
                discoverySettings,
                options,
                discoveryEventsHandler);
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
        public async Task DiscoverTestsAsync(
            IEnumerable<string> sources,
            string discoverySettings,
            TestPlatformOptions options,
            ITestDiscoveryEventsHandler2 discoveryEventsHandler)
        {
            // TODO (copoiena): Hook into the wrapper and pass session info here.
            await this.consoleWrapper.DiscoverTestsAsync(
                sources,
                discoverySettings,
                options,
                discoveryEventsHandler);
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
