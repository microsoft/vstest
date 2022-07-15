// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Client
{
    /// <summary>
    /// Special implementation of test run event handler that allows us to register actions only for selected methods.
    /// Normally you would most likely do the handling directly in the class that implements ITestRunEventsHandler,
    /// and you would keep the methods you don't need empty.
    /// </summary>
    internal class MyTestRunEventsHandler : ITestRunEventsHandler
    {
        public Action<TestMessageLevel, string> OnLogMessage;

        public Action<TestRunCompleteEventArgs, TestRunChangedEventArgs, ICollection<AttachmentSet>, ICollection<string>> OnTestRunComplete;
        public Action<TestRunChangedEventArgs> OnTestRunStatsChange;

        public Action<string> OnRawMessage;

        public void HandleLogMessage(TestMessageLevel level, string message)
        {
            if (OnLogMessage != null)
                OnLogMessage(level, message);
        }

        public void HandleRawMessage(string rawMessage)
        {
            if (OnRawMessage != null)
                OnRawMessage(rawMessage);
        }

        public void HandleTestRunComplete(TestRunCompleteEventArgs testRunCompleteArgs, TestRunChangedEventArgs lastChunkArgs, ICollection<AttachmentSet> runContextAttachments, ICollection<string> executorUris)
        {
            if (OnTestRunComplete != null)
                OnTestRunComplete(testRunCompleteArgs, lastChunkArgs, runContextAttachments, executorUris);
        }

        public void HandleTestRunStatsChange(TestRunChangedEventArgs testRunChangedArgs)
        {
            if (OnTestRunStatsChange != null)
                OnTestRunStatsChange(testRunChangedArgs);
        }

        public int LaunchProcessWithDebuggerAttached(TestProcessStartInfo testProcessStartInfo)
        {
            // This is not used in vstest 17.2+;
            return -1;
        }
    }
}
