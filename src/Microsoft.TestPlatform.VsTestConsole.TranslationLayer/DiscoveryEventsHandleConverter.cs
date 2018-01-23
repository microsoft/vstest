// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    /// <summary>
    /// The Discovery Events Handler Converter.
    /// Converts the ITestDiscoveryEventsHandler to ITestDiscoveryEventsHandler2
    /// </summary>
    public class DiscoveryEventsHandleConverter : ITestDiscoveryEventsHandler2
    {
        private ITestDiscoveryEventsHandler testDiscoveryEventsHandler;

        /// <summary>
        /// The Discovery Complete Handler.
        /// Converts the ITestDiscoveryEventsHandler to ITestDiscoveryEventsHandler2
        /// </summary>
        /// <param name="testDiscoveryEventsHandler"></param>
        public DiscoveryEventsHandleConverter(ITestDiscoveryEventsHandler testDiscoveryEventsHandler)
        {
            this.testDiscoveryEventsHandler = testDiscoveryEventsHandler ?? throw new ArgumentNullException(nameof(testDiscoveryEventsHandler));
        }

        /// <summary>
        /// Handles Raw Message
        /// </summary>
        /// <param name="rawMessage"></param>
        public void HandleRawMessage(string rawMessage)
        {
            this.testDiscoveryEventsHandler.HandleRawMessage(rawMessage);
        }

        /// <summary>
        /// Handles Log Message
        /// </summary>
        /// <param name="level"></param>
        /// <param name="message"></param>
        public void HandleLogMessage(TestMessageLevel level, string message)
        {
            this.testDiscoveryEventsHandler.HandleLogMessage(level, message);
        }

        /// <summary>
        /// Handle Discovery Complete
        /// </summary>
        /// <param name="discoveryCompleteEventArgs"></param>
        /// <param name="lastChunk"></param>
        public void HandleDiscoveryComplete(DiscoveryCompleteEventArgs discoveryCompleteEventArgs, IEnumerable<TestCase> lastChunk)
        {
            this.testDiscoveryEventsHandler.HandleDiscoveryComplete(discoveryCompleteEventArgs.TotalCount, lastChunk, discoveryCompleteEventArgs.IsAborted);
        }

        /// <summary>
        /// Handles Discovery Tests
        /// </summary>
        /// <param name="discoveredTestCases"></param>
        public void HandleDiscoveredTests(IEnumerable<TestCase> discoveredTestCases)
        {
            this.testDiscoveryEventsHandler.HandleDiscoveredTests(discoveredTestCases);
        }
    }
}