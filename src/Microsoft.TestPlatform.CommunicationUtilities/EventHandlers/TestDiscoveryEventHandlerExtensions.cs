// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.EventHandlers
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    public static class TestDiscoveryEventHandlerExtensions
    {
        public static void OnAbort(this ITestDiscoveryEventsHandler discoveryEventsHandler, IDataSerializer dataSerializer, Exception exception)
        {
            ValidateArg.NotNull(dataSerializer, nameof(dataSerializer));

            EqtTrace.Error("Server: TestExecution: Aborting test discovery because {0}: {1}", exception?.Message, exception?.StackTrace);

            var reason = string.Format(Resources.Resources.AbortedTestDiscovery, exception?.Message);

            // Log to vstest console.
            discoveryEventsHandler.HandleLogMessage(TestMessageLevel.Error, reason);

            // Log to vs ide test output.
            var testMessagePayload = new TestMessagePayload { MessageLevel = TestMessageLevel.Error, Message = reason };
            var rawMessage = dataSerializer.SerializePayload(MessageType.TestMessage, testMessagePayload);
            discoveryEventsHandler.HandleRawMessage(rawMessage);

            // Notify discovery abort to IDE test output.
            var payload = new DiscoveryCompletePayload()
            {
                IsAborted = true,
                LastDiscoveredTests = null,
                TotalTests = -1
            };
            rawMessage = dataSerializer.SerializePayload(MessageType.DiscoveryComplete, payload);
            discoveryEventsHandler.HandleRawMessage(rawMessage);

            // Complete discovery.
            discoveryEventsHandler.HandleDiscoveryComplete(-1, null, true);
        }

    }
}
