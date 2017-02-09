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
    public static class TestRunEventsHandlerExtensions
    {
        public static void OnAbort(this ITestRunEventsHandler testRunEventsHandler, IDataSerializer dataSerializer, Exception exception)
        {
            ValidateArg.NotNull(dataSerializer ,nameof(dataSerializer));

            EqtTrace.Error("Server: TestExecution: Aborting test run because {0}:{1}", exception?.Message, exception?.StackTrace);

            var reason = string.Format(Resources.Resources.AbortedTestRun, exception?.Message);
            // Log console message to vstest console.
            testRunEventsHandler.HandleLogMessage(TestMessageLevel.Error, reason);

            // Log console message to IDE.
            var testMessagePayload = new TestMessagePayload { MessageLevel = TestMessageLevel.Error, Message = reason };
            var rawMessage = dataSerializer.SerializePayload(MessageType.TestMessage, testMessagePayload);
            testRunEventsHandler.HandleRawMessage(rawMessage);

            // Notify test run abort to IDE.
            var completeArgs = new TestRunCompleteEventArgs(null, false, true, exception, null, TimeSpan.Zero);
            var payload = new TestRunCompletePayload { TestRunCompleteArgs = completeArgs };
            rawMessage = dataSerializer.SerializePayload(MessageType.ExecutionComplete, payload);
            testRunEventsHandler.HandleRawMessage(rawMessage);

            // Notify of a test run complete and bail out.
            testRunEventsHandler.HandleTestRunComplete(completeArgs, null, null, null);
        }
    }
}
