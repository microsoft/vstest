// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces
{
    using System;
    using System.Collections.Generic;
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
        /// See <see cref="ITranslationLayerRequestSender.Close"/>
        /// </summary>
        void Close();

        /// <summary>
        /// See <see cref="ITranslationLayerRequestSender.InitializeExtensions"/>
        /// </summary>
        void InitializeExtensions(IEnumerable<string> pathToAdditionalExtensions);

        /// <summary>
        /// Asynchronous equivalent of <see cref="ITranslationLayerRequestSender.DiscoverTests(IEnumerable{string}, string, ITestDiscoveryEventsHandler)"/>.
        /// </summary>
        Task DiscoverTestsAsync(IEnumerable<string> sources, string runSettings, ITestDiscoveryEventsHandler discoveryEventsHandler);

        /// <summary>
        /// Asynchronous equivalent of <see cref="ITranslationLayerRequestSender.StartTestRun(IEnumerable{string}, string, TestPlatformOptions, ITestRunEventsHandler)"/>.
        /// </summary>
        Task StartTestRunAsync(IEnumerable<string> sources, string runSettings, ITestRunEventsHandler runEventsHandler);

        /// <summary>
        /// Asynchronous equivalent of <see cref="ITranslationLayerRequestSender.StartTestRun(IEnumerable{TestCase}, string, ITestRunEventsHandler)"/>.
        /// </summary>
        Task StartTestRunAsync(IEnumerable<TestCase> testCases, string runSettings, ITestRunEventsHandler runEventsHandler);

        /// <summary>
        /// Asynchronous equivalent of <see cref="ITranslationLayerRequestSender.StartTestRunWithCustomHost(IEnumerable{string}, string, TestPlatformOptions, ITestRunEventsHandler, ITestHostLauncher)"/>.
        /// </summary>
        Task StartTestRunWithCustomHostAsync(IEnumerable<string> sources, string runSettings, ITestRunEventsHandler runEventsHandler, ITestHostLauncher customTestHostLauncher);

        /// <summary>
        /// Asynchronous equivalent of <see cref="ITranslationLayerRequestSender.StartTestRunWithCustomHost(IEnumerable{TestCase}, string, ITestRunEventsHandler, ITestHostLauncher)"/>.
        /// </summary>
        Task StartTestRunWithCustomHostAsync(IEnumerable<TestCase> testCases, string runSettings, ITestRunEventsHandler runEventsHandler, ITestHostLauncher customTestHostLauncher);

        /// <summary>
        /// See <see cref="ITranslationLayerRequestSender.EndSession"/>.
        /// </summary>
        void EndSession();

        /// <summary>
        /// See <see cref="ITranslationLayerRequestSender.CancelTestRun"/>.
        /// </summary>
        void CancelTestRun();

        /// <summary>
        /// See <see cref="ITranslationLayerRequestSender.AbortTestRun"/>.
        /// </summary>
        void AbortTestRun();

        /// <summary>
        /// See <see cref="ITranslationLayerRequestSender.OnProcessExited"/>.
        /// </summary>
        void OnProcessExited();
    }
}
