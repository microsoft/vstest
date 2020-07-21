// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;

    /// <summary>
    /// Asynchronous equivalent of <see cref="IVsTestConsoleWrapper"/>
    /// </summary>
    public interface IVsTestConsoleWrapperAsync
    {
        /// <summary>
        /// Asynchronous equivalent of <see cref="IVsTestConsoleWrapper.StartSession"/>.
        /// </summary>
        /// <returns></returns>
        Task StartSessionAsync();

        /// <summary>
        /// Asynchronous equivalent of <see cref="IVsTestConsoleWrapper.InitializeExtensions(IEnumerable{string})"/>.
        /// </summary>
        Task InitializeExtensionsAsync(IEnumerable<string> pathToAdditionalExtensions);

        /// <summary>
        /// Asynchronous equivalent of <see cref="IVsTestConsoleWrapper.DiscoverTests(IEnumerable{string}, string, ITestDiscoveryEventsHandler)"/>
        /// </summary>
        Task DiscoverTestsAsync(IEnumerable<string> sources, string discoverySettings, ITestDiscoveryEventsHandler discoveryEventsHandler);

        /// <summary>
        /// Asynchronous equivalent of <see cref="IVsTestConsoleWrapper.DiscoverTests(IEnumerable{string}, string, TestPlatformOptions, ITestDiscoveryEventsHandler2)"/>
        /// </summary>
        Task DiscoverTestsAsync(IEnumerable<string> sources, string discoverySettings, TestPlatformOptions options, ITestDiscoveryEventsHandler2 discoveryEventsHandler);

        /// <summary>
        /// See <see cref="IVsTestConsoleWrapper.CancelDiscovery"/>.
        /// </summary>
        void CancelDiscovery();

        /// <summary>
        /// Asynchronous equivalent of <see cref="IVsTestConsoleWrapper.RunTests(IEnumerable{string}, string, ITestRunEventsHandler)"/>.
        /// </summary>
        Task RunTestsAsync(IEnumerable<string> sources, string runSettings, ITestRunEventsHandler testRunEventsHandler);

        /// <summary>
        /// Asynchronous equivalent of <see cref="IVsTestConsoleWrapper.RunTests(IEnumerable{string}, string, TestPlatformOptions, ITestRunEventsHandler)"/>.
        /// </summary>
        Task RunTestsAsync(IEnumerable<string> sources, string runSettings, TestPlatformOptions options, ITestRunEventsHandler testRunEventsHandler);

        /// <summary>
        /// Asynchronous equivalent of <see cref="IVsTestConsoleWrapper.RunTests(IEnumerable{TestCase}, string, ITestRunEventsHandler)"/>.
        /// </summary>
        Task RunTestsAsync(IEnumerable<TestCase> testCases, string runSettings, ITestRunEventsHandler testRunEventsHandler);

        /// <summary>
        /// Asynchronous equivalent of <see cref="IVsTestConsoleWrapper.RunTests(IEnumerable{TestCase}, string, TestPlatformOptions, ITestRunEventsHandler)"/>.
        /// </summary>
        Task RunTestsAsync(IEnumerable<TestCase> testCases, string runSettings, TestPlatformOptions options, ITestRunEventsHandler testRunEventsHandler);

        /// <summary>
        /// Asynchronous equivalent of <see cref="IVsTestConsoleWrapper.RunTestsWithCustomTestHost(IEnumerable{string}, string, ITestRunEventsHandler, ITestHostLauncher)"/>.
        /// </summary>
        Task RunTestsWithCustomTestHostAsync(IEnumerable<string> sources, string runSettings, ITestRunEventsHandler testRunEventsHandler, ITestHostLauncher customTestHostLauncher);

        /// <summary>
        /// Asynchronous equivalent of <see cref="IVsTestConsoleWrapper.RunTestsWithCustomTestHost(IEnumerable{string}, string, TestPlatformOptions, ITestRunEventsHandler, ITestHostLauncher)"/>.
        /// </summary>
        Task RunTestsWithCustomTestHostAsync(IEnumerable<string> sources, string runSettings, TestPlatformOptions options, ITestRunEventsHandler testRunEventsHandler, ITestHostLauncher customTestHostLauncher);

        /// <summary>
        /// Asynchronous equivalent of <see cref="IVsTestConsoleWrapper.RunTestsWithCustomTestHost(IEnumerable{TestCase}, string, ITestRunEventsHandler, ITestHostLauncher)"/>.
        /// </summary>
        Task RunTestsWithCustomTestHostAsync(IEnumerable<TestCase> testCases, string runSettings, ITestRunEventsHandler testRunEventsHandler, ITestHostLauncher customTestHostLauncher);

        /// <summary>
        /// Asynchronous equivalent of <see cref="IVsTestConsoleWrapper.RunTestsWithCustomTestHost(IEnumerable{TestCase}, string, TestPlatformOptions, ITestRunEventsHandler, ITestHostLauncher)"/>.
        /// </summary>
        Task RunTestsWithCustomTestHostAsync(IEnumerable<TestCase> testCases, string runSettings, TestPlatformOptions options, ITestRunEventsHandler testRunEventsHandler, ITestHostLauncher customTestHostLauncher);

        /// <summary>
        /// See <see cref="IVsTestConsoleWrapper.CancelTestRun"/>.
        /// </summary>
        void CancelTestRun();

        /// <summary>
        /// See <see cref="IVsTestConsoleWrapper.AbortTestRun"/>.
        /// </summary>
        void AbortTestRun();

        /// <summary>
        /// Provides back all attachments to TestPlatform for additional processing (for example merging)
        /// </summary>
        /// <param name="attachments">Collection of attachments</param>
        /// <param name="processingSettings">XML processing settings</param>
        /// <param name="isLastBatch">Indicates that all test executions are done and all data is provided</param>
        /// <param name="collectMetrics">Enables metrics collection (used for telemetry)</param>
        /// <param name="eventsHandler">EventHandler to receive session complete event</param>
        /// <param name="cancellationToken">Cancellation token</param>        
        Task ProcessTestRunAttachmentsAsync(IEnumerable<AttachmentSet> attachments, string processingSettings, bool isLastBatch, bool collectMetrics, ITestRunAttachmentsProcessingEventsHandler eventsHandler, CancellationToken cancellationToken);

        /// <summary>
        /// See <see cref="IVsTestConsoleWrapper.EndSession"/>.
        /// </summary>
        void EndSession();
    }
}
