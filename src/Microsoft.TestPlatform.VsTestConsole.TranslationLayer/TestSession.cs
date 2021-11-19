﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;

    /// <summary>
    /// Defines a test session object that can be used to make calls to the vstest.console
    /// process.
    /// </summary>
    [Obsolete("This API is not final yet and is subject to changes.", false)]
    public class TestSession : ITestSession
    {
        private bool disposed = false;

        private readonly ITestSessionEventsHandler eventsHandler;
        private readonly IVsTestConsoleWrapper consoleWrapper;

        #region Properties
        /// <inheritdoc/>
        [Obsolete("This API is not final yet and is subject to changes.", false)]
        public TestSessionInfo TestSessionInfo { get; private set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TestSession"/> class.
        /// </summary>
        /// 
        /// <param name="testSessionInfo">The test session info object.</param>
        /// <param name="eventsHandler">The session event handler.</param>
        /// <param name="consoleWrapper">The encapsulated console wrapper.</param>
        public TestSession(
            TestSessionInfo testSessionInfo,
            ITestSessionEventsHandler eventsHandler,
            IVsTestConsoleWrapper consoleWrapper)
        {
            this.TestSessionInfo = testSessionInfo;
            this.eventsHandler = eventsHandler;
            this.consoleWrapper = consoleWrapper;
        }

        /// <summary>
        /// Destroys the current instance of the <see cref="TestSession"/> class.
        /// </summary>
        ~TestSession() => this.Dispose(false);

        /// <summary>
        /// Disposes of the current instance of the <see cref="TestSession"/> class.
        /// </summary>
        [Obsolete("This API is not final yet and is subject to changes.", false)]
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of the current instance of the <see cref="TestSession"/> class.
        /// </summary>
        /// 
        /// <param name="disposing">Indicates if managed resources should be disposed.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.StopTestSession();
            this.disposed = true;
        }
        #endregion

        #region ITestSession
        /// <inheritdoc/>
        [Obsolete("This API is not final yet and is subject to changes.", false)]
        public void AbortTestRun()
        {
            this.consoleWrapper.AbortTestRun();
        }

        /// <inheritdoc/>
        [Obsolete("This API is not final yet and is subject to changes.", false)]
        public void CancelDiscovery()
        {
            this.consoleWrapper.CancelDiscovery();
        }

        /// <inheritdoc/>
        [Obsolete("This API is not final yet and is subject to changes.", false)]
        public void CancelTestRun()
        {
            this.consoleWrapper.CancelTestRun();
        }

        /// <inheritdoc/>
        [Obsolete("This API is not final yet and is subject to changes.", false)]
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
        [Obsolete("This API is not final yet and is subject to changes.", false)]
        public void DiscoverTests(
            IEnumerable<string> sources,
            string discoverySettings,
            TestPlatformOptions options,
            ITestDiscoveryEventsHandler2 discoveryEventsHandler)
        {
            this.consoleWrapper.DiscoverTests(
                sources,
                discoverySettings,
                options,
                this.TestSessionInfo,
                discoveryEventsHandler);
        }

        /// <inheritdoc/>
        [Obsolete("This API is not final yet and is subject to changes.", false)]
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
        [Obsolete("This API is not final yet and is subject to changes.", false)]
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
                this.TestSessionInfo,
                testRunEventsHandler);
        }

        /// <inheritdoc/>
        [Obsolete("This API is not final yet and is subject to changes.", false)]
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
        [Obsolete("This API is not final yet and is subject to changes.", false)]
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
                this.TestSessionInfo,
                testRunEventsHandler);
        }

        /// <inheritdoc/>
        [Obsolete("This API is not final yet and is subject to changes.", false)]
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
        [Obsolete("This API is not final yet and is subject to changes.", false)]
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
                this.TestSessionInfo,
                testRunEventsHandler,
                customTestHostLauncher);
        }

        /// <inheritdoc/>
        [Obsolete("This API is not final yet and is subject to changes.", false)]
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
        [Obsolete("This API is not final yet and is subject to changes.", false)]
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
                this.TestSessionInfo,
                testRunEventsHandler,
                customTestHostLauncher);
        }

        /// <inheritdoc/>
        [Obsolete("This API is not final yet and is subject to changes.", false)]
        public bool StopTestSession()
        {
            return this.StopTestSession(this.eventsHandler);
        }

        /// <inheritdoc/>
        [Obsolete("This API is not final yet and is subject to changes.", false)]
        public bool StopTestSession(ITestSessionEventsHandler eventsHandler)
        {
            if (this.TestSessionInfo == null)
            {
                return true;
            }

            try
            {
                return this.consoleWrapper.StopTestSession(
                    this.TestSessionInfo,
                    eventsHandler);
            }
            finally
            {
                this.TestSessionInfo = null;
            }
        }
        #endregion

        #region ITestSessionAsync
        /// <inheritdoc/>
        [Obsolete("This API is not final yet and is subject to changes.", false)]
        public async Task DiscoverTestsAsync(
            IEnumerable<string> sources,
            string discoverySettings,
            ITestDiscoveryEventsHandler discoveryEventsHandler)
        {
            await this.DiscoverTestsAsync(
                    sources,
                    discoverySettings,
                    options: null,
                    discoveryEventsHandler:
                        new DiscoveryEventsHandleConverter(discoveryEventsHandler))
                .ConfigureAwait(false);
        }

        /// <inheritdoc/>
        [Obsolete("This API is not final yet and is subject to changes.", false)]
        public async Task DiscoverTestsAsync(
            IEnumerable<string> sources,
            string discoverySettings,
            TestPlatformOptions options,
            ITestDiscoveryEventsHandler2 discoveryEventsHandler)
        {
            await this.consoleWrapper.DiscoverTestsAsync(
                sources,
                discoverySettings,
                options,
                this.TestSessionInfo,
                discoveryEventsHandler).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        [Obsolete("This API is not final yet and is subject to changes.", false)]
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
        [Obsolete("This API is not final yet and is subject to changes.", false)]
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
                this.TestSessionInfo,
                testRunEventsHandler).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        [Obsolete("This API is not final yet and is subject to changes.", false)]
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
        [Obsolete("This API is not final yet and is subject to changes.", false)]
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
                this.TestSessionInfo,
                testRunEventsHandler).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        [Obsolete("This API is not final yet and is subject to changes.", false)]
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
        [Obsolete("This API is not final yet and is subject to changes.", false)]
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
                this.TestSessionInfo,
                testRunEventsHandler,
                customTestHostLauncher).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        [Obsolete("This API is not final yet and is subject to changes.", false)]
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
        [Obsolete("This API is not final yet and is subject to changes.", false)]
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
                this.TestSessionInfo,
                testRunEventsHandler,
                customTestHostLauncher).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        [Obsolete("This API is not final yet and is subject to changes.", false)]
        public async Task<bool> StopTestSessionAsync()
        {
            return await this.StopTestSessionAsync(this.eventsHandler).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        [Obsolete("This API is not final yet and is subject to changes.", false)]
        public async Task<bool> StopTestSessionAsync(ITestSessionEventsHandler eventsHandler)
        {
            if (this.TestSessionInfo == null)
            {
                return true;
            }

            try
            {
                return await this.consoleWrapper.StopTestSessionAsync(
                    this.TestSessionInfo,
                    eventsHandler).ConfigureAwait(false);
            }
            finally
            {
                this.TestSessionInfo = null;
            }
        }
        #endregion
    }
}
