// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
{
    /// <summary>
    /// Interface contract for handling start test runner events.
    /// </summary>
    public interface IStartTestRunnerEventsHandler : ITestMessageEventHandler
    {
        /// <summary>
        /// Dispatch StartTestRunnerComplete event to listeners.
        /// </summary>
        /// <param name="runnerPids">Test runner pid set.</param>
        void HandleStartTestRunnerComplete(Session session);
    }
}
