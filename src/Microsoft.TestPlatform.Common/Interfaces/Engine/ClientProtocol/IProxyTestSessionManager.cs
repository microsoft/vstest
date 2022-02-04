﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;

using Client;

/// <summary>
/// Orchestrates test session related functionality for the engine communicating with the
/// client.
/// </summary>
public interface IProxyTestSessionManager
{
    /// <summary>
    /// Starts the test session based on the test session criteria.
    /// </summary>
    /// 
    /// <param name="eventsHandler">
    /// Event handler for handling events fired during test session management operations.
    /// </param>
    /// 
    /// <returns>True if the operation succeeded, false otherwise.</returns>
    bool StartSession(ITestSessionEventsHandler eventsHandler);

    /// <summary>
    /// Stops the test session.
    /// </summary>
    ///
    /// <returns>True if the operation succeeded, false otherwise.</returns>
    bool StopSession();
}
