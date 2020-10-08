﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
{
    /// <summary>
    /// Interface contract for handling start test session events.
    /// </summary>
    public interface IStartTestSessionEventsHandler : ITestMessageEventHandler
    {
        /// <summary>
        /// Dispatch StartTestSession complete event to listeners.
        /// </summary>
        /// <param name="testSessionInfo">The test session info.</param>
        void HandleStartTestSessionComplete(TestSessionInfo testSessionInfo);
    }
}
