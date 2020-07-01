// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;

    /// <summary>
    /// Asynchronous equivalent of <see cref="ITranslationLayerRequestSender"/>.
    /// </summary>
    internal interface ITranslationLayerRequestSenderAsync : IDisposable
    {
        /// <summary>
        /// Asynchronous equivalent of <see cref="ITranslationLayerRequestSender.InitializeCommunication"/>
        /// and <see cref="ITranslationLayerRequestSender.WaitForRequestHandlerConnection(int)"/>.
        /// </summary>
        Task<int> InitializeCommunicationAsync(int clientConnectionTimeout);

        /// <summary>
        /// Asynchronous equivalent of ITranslationLayerRequestSender.DiscoverTests/>.
        /// </summary>
        Task DiscoverTestsAsync(IEnumerable<string> sources, string runSettings, TestPlatformOptions options, ITestDiscoveryEventsHandler2 discoveryEventsHandler);

        /// <summary>
        /// Asynchronous equivalent of <see cref="ITranslationLayerRequestSender.StartTestRun(IEnumerable{string}, string, TestPlatformOptions, ITestRunEventsHandler)"/>.
        /// </summary>
        Task StartTestRunAsync(IEnumerable<string> sources, string runSettings, TestPlatformOptions options, ITestRunEventsHandler runEventsHandler);

        /// <summary>
        /// Asynchronous equivalent of <see cref="ITranslationLayerRequestSender.StartTestRun(System.Collections.Generic.IEnumerable{Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase},string,Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.TestPlatformOptions,Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.ITestRunEventsHandler)"/>.
        /// </summary>
        Task StartTestRunAsync(IEnumerable<TestCase> testCases, string runSettings, TestPlatformOptions options, ITestRunEventsHandler runEventsHandler);

        /// <summary>
        /// Asynchronous equivalent of <see cref="ITranslationLayerRequestSender.StartTestRunWithCustomHost(IEnumerable{string}, string, TestPlatformOptions, ITestRunEventsHandler, ITestHostLauncher)"/>.
        /// </summary>
        Task StartTestRunWithCustomHostAsync(IEnumerable<string> sources, string runSettings, TestPlatformOptions options, ITestRunEventsHandler runEventsHandler, ITestHostLauncher customTestHostLauncher);

        /// <summary>
        /// Asynchronous equivalent of <see cref="ITranslationLayerRequestSender.StartTestRunWithCustomHost(System.Collections.Generic.IEnumerable{Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase},string,Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.TestPlatformOptions,Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.ITestRunEventsHandler,Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces.ITestHostLauncher)"/>.
        /// </summary>
        Task StartTestRunWithCustomHostAsync(IEnumerable<TestCase> testCases, string runSettings, TestPlatformOptions options, ITestRunEventsHandler runEventsHandler, ITestHostLauncher customTestHostLauncher);

        /// <summary>
        /// Provides back all attachments to TestPlatform for additional processing (for example merging)
        /// </summary>
        /// <param name="attachments">Collection of attachments</param>
        /// <param name="collectMetrics">Enables metrics collection</param>
        /// <param name="testRunAttachmentsProcessingCompleteEventsHandler">Events handler</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task ProcessTestRunAttachmentsAsync(IEnumerable<AttachmentSet> attachments, bool collectMetrics, ITestRunAttachmentsProcessingEventsHandler testRunAttachmentsProcessingCompleteEventsHandler, CancellationToken cancellationToken);
    }
}
