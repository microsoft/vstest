// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
{
    /// <summary>
    /// Interface contract for handling test session events.
    /// </summary>
    public interface ITestSessionEventsHandler : ITestMessageEventHandler
    {
        /// <summary>
        /// Dispatch StartTestSession complete event to listeners.
        /// </summary>
        /// 
        /// <param name="testSessionInfo">The test session info.</param>
        void HandleStartTestSessionComplete(TestSessionInfo testSessionInfo);

        /// <summary>
        /// Dispatch StopTestSession complete event to listeners.
        /// </summary>
        /// 
        /// <param name="testSessionInfo">
        /// The test session info for the session that was stopped.
        /// </param>
        /// <param name="stopped">
        /// True if the session was successfully stopped, false otherwise.
        /// </param>
        void HandleStopTestSessionComplete(TestSessionInfo testSessionInfo, bool stopped);
    }
}
