// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests.TranslationLayerTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    /// <inheritdoc />
    public class RunEventHandler : ITestRunEventsHandler2
    {
        /// <summary>
        /// Gets the test results.
        /// </summary>
        public List<TestResult> TestResults { get; private set; }

        /// <summary>
        /// Gets the attachments.
        /// </summary>
        public List<AttachmentSet> Attachments { get; private set; }

        /// <summary>
        /// Gets the metrics.
        /// </summary>
        public IDictionary<string, object> Metrics { get; private set; }

        /// <summary>
        /// Gets the log message.
        /// </summary>
        public string LogMessage { get; private set; }

        public List<string> Errors { get; set; }

        /// <summary>
        /// Gets the test message level.
        /// </summary>
        public TestMessageLevel TestMessageLevel { get; private set; }

        public RunEventHandler()
        {
            this.TestResults = new List<TestResult>();
            this.Errors = new List<string>();
            this.Attachments = new List<AttachmentSet>();
        }

        public void EnsureSuccess()
        {
            if (this.Errors.Any())
            {
                throw new InvalidOperationException($"Test run reported errors:{Environment.NewLine}{string.Join(Environment.NewLine + Environment.NewLine, this.Errors)}");
            }
        }

        public void HandleLogMessage(TestMessageLevel level, string message)
        {
            this.LogMessage = message;
            this.TestMessageLevel = level;
            if (level == TestMessageLevel.Error) {
                this.Errors.Add(message);
            }
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
            }

            if (testRunCompleteArgs.AttachmentSets != null)
            {
                this.Attachments.AddRange(testRunCompleteArgs.AttachmentSets);
            }

            this.Metrics = testRunCompleteArgs.Metrics;
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

        public bool AttachDebuggerToProcess(int pid)
        {
            // No op
            return true;
        }
    }
}
