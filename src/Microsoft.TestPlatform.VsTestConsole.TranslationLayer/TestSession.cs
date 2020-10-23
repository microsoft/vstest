// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer
{
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;

    /// <summary>
    /// 
    /// </summary>
    public class TestSession : ITestSession
    {
        //private string runSettings;
        //private IList<string> sources;
        private TestSessionInfo testSessionInfo;
        private VsTestConsoleWrapper consoleWrapper;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="testSessionInfo"></param>
        /// <param name="consoleWrapper"></param>
        public TestSession(TestSessionInfo testSessionInfo, VsTestConsoleWrapper consoleWrapper)
        {
            //this.runSettings = "";
            //this.sources = null;
            this.testSessionInfo = testSessionInfo;
            this.consoleWrapper = consoleWrapper;
        }

        /// <inheritdoc/>
        public void RunTests(
            IEnumerable<string> sources,
            string runSettings,
            ITestRunEventsHandler testRunEventsHandler)
        {
            this.RunTests(sources, runSettings, options: null, testRunEventsHandler);
        }

        /// <inheritdoc/>
        public void RunTests(
            IEnumerable<string> sources,
            string runSettings,
            TestPlatformOptions options,
            ITestRunEventsHandler testRunEventsHandler)
        {
            this.consoleWrapper.RunTests(sources, runSettings, options, this.testSessionInfo, testRunEventsHandler);
        }

        /// <inheritdoc/>
        public void RunTests(
            IEnumerable<TestCase> testCases,
            string runSettings,
            ITestRunEventsHandler testRunEventsHandler)
        {
            this.RunTests(testCases, runSettings, options: null, testRunEventsHandler);
        }

        /// <inheritdoc/>
        public void RunTests(
            IEnumerable<TestCase> testCases,
            string runSettings,
            TestPlatformOptions options,
            ITestRunEventsHandler testRunEventsHandler)
        {
            this.consoleWrapper.RunTests(testCases, runSettings, options, this.testSessionInfo, testRunEventsHandler);
        }

        /// <inheritdoc/>
        public void RunTestsWithCustomTestHost(
            IEnumerable<string> sources,
            string runSettings,
            ITestRunEventsHandler testRunEventsHandler,
            ITestHostLauncher customTestHostLauncher)
        {
            this.RunTestsWithCustomTestHost(sources, runSettings, options: null, testRunEventsHandler, customTestHostLauncher);
        }

        /// <inheritdoc/>
        public void RunTestsWithCustomTestHost(
            IEnumerable<string> sources,
            string runSettings,
            TestPlatformOptions options,
            ITestRunEventsHandler testRunEventsHandler,
            ITestHostLauncher customTestHostLauncher)
        {
            this.consoleWrapper.RunTestsWithCustomTestHost(sources, runSettings, options, this.testSessionInfo, testRunEventsHandler, customTestHostLauncher);
        }

        /// <inheritdoc/>
        public void RunTestsWithCustomTestHost(
            IEnumerable<TestCase> testCases,
            string runSettings,
            ITestRunEventsHandler testRunEventsHandler,
            ITestHostLauncher customTestHostLauncher)
        {
            this.RunTestsWithCustomTestHost(testCases, runSettings, options: null, testRunEventsHandler, customTestHostLauncher);
        }

        /// <inheritdoc/>
        public void RunTestsWithCustomTestHost(
            IEnumerable<TestCase> testCases,
            string runSettings,
            TestPlatformOptions options,
            ITestRunEventsHandler testRunEventsHandler,
            ITestHostLauncher customTestHostLauncher)
        {
            this.consoleWrapper.RunTestsWithCustomTestHost(testCases, runSettings, options, this.testSessionInfo, testRunEventsHandler, customTestHostLauncher);
        }
    }
}
