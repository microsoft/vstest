// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests.TranslationLayerTests
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    /// <inheritdoc />
    public class RunEventHandler : ITestRunEventsHandler
    {
        public List<TestResult> TestResults { get; private set; }

        public IDictionary<string, object> Metrics { get; private set; }

        public string LogMessage { get; private set; }

        public TestMessageLevel TestMessageLevel { get; private set; }

        public RunEventHandler()
        {
            this.TestResults = new List<TestResult>();
        }

        public void HandleLogMessage(TestMessageLevel level, string message)
        {
            this.LogMessage = message;
            this.TestMessageLevel = level;
        }

        public void HandleTestRunComplete(
            TestRunCompleteEventArgs testRunCompleteArgs,
            TestRunChangedEventArgs lastChunkArgs,
            ICollection<AttachmentSet> runContextAttachments,
            ICollection<string> executorUris)
        {
            if (lastChunkArgs != null && lastChunkArgs.NewTestResults != null)
            {
                this.TestResults.AddRange(lastChunkArgs.NewTestResults);
                this.Metrics = testRunCompleteArgs.Metrics;
            }
        }

        public void HandleTestRunStatsChange(TestRunChangedEventArgs testRunChangedArgs)
        {
            if (testRunChangedArgs != null && testRunChangedArgs.NewTestResults != null)
            {
                this.TestResults.AddRange(testRunChangedArgs.NewTestResults);
            }
        }

        public void HandleRawMessage(string rawMessage)
        {
            // No op
        }

        public int LaunchProcessWithDebuggerAttached(TestProcessStartInfo testProcessStartInfo)
        {
            // No op
            return -1;
        }
    }
}
