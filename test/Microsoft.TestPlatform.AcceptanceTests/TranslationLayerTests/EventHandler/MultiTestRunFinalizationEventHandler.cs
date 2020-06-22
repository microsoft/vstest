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
    public class MultiTestRunFinalizationEventHandler : IMultiTestRunFinalizationEventsHandler
    {
        public List<AttachmentSet> Attachments { get; private set; }

        public MultiTestRunFinalizationCompleteEventArgs CompleteArgs { get; private set; }

        public List<MultiTestRunFinalizationProgressEventArgs> ProgressArgs { get; private set; }

        /// <summary>
        /// Gets the log message.
        /// </summary>
        public string LogMessage { get; private set; }

        public List<string> Errors { get; set; }

        /// <summary>
        /// Gets the test message level.
        /// </summary>
        public TestMessageLevel TestMessageLevel { get; private set; }

        public MultiTestRunFinalizationEventHandler()
        {
            this.Errors = new List<string>();
            this.Attachments = new List<AttachmentSet>();
            this.ProgressArgs = new List<MultiTestRunFinalizationProgressEventArgs>();
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
            if (level == TestMessageLevel.Error) 
            {
                this.Errors.Add(message);
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

        public void HandleMultiTestRunFinalizationComplete(ICollection<AttachmentSet> attachments)
        {
            if(attachments != null)
            {
                this.Attachments.AddRange(attachments);
            }
        }

        public void HandleMultiTestRunFinalizationComplete(MultiTestRunFinalizationCompleteEventArgs finalizationCompleteEventArgs, IEnumerable<AttachmentSet> lastChunk)
        {
            if (lastChunk != null)
            {
                this.Attachments.AddRange(lastChunk);
            }

            if (finalizationCompleteEventArgs.Error != null)
            {
                Errors.Add(finalizationCompleteEventArgs.Error.Message);
            }

            CompleteArgs = finalizationCompleteEventArgs;
        }

        public void HandleFinalisedAttachments(IEnumerable<AttachmentSet> attachments)
        {
            throw new NotImplementedException();
        }

        public void HandleMultiTestRunFinalizationProgress(MultiTestRunFinalizationProgressEventArgs finalizationProgressEventArgs)
        {
            ProgressArgs.Add(finalizationProgressEventArgs);
        }
    }
}
