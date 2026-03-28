// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Client.Async.Internal;

/// <summary>
/// Protocol constants matching the vstest.console design mode protocol.
/// These mirror the MessageType values from CommunicationUtilities but are
/// defined here to avoid taking a dependency on that assembly.
/// </summary>
internal static class ProtocolConstants
{
    // Latest protocol version supported by this client.
    public const int ProtocolVersion = 7;

    // Session messages
    public const string SessionConnected = "TestSession.Connected";
    public const string SessionEnd = "TestSession.Terminate";
    public const string TestMessage = "TestSession.Message";

    // Version negotiation
    public const string VersionCheck = "ProtocolVersion";
    public const string ProtocolError = "ProtocolError";

    // Discovery
    public const string DiscoveryInitialize = "TestDiscovery.Initialize";
    public const string StartDiscovery = "TestDiscovery.Start";
    public const string TestCasesFound = "TestDiscovery.TestFound";
    public const string DiscoveryComplete = "TestDiscovery.Completed";

    // Execution
    public const string ExecutionInitialize = "TestExecution.Initialize";
    public const string TestRunAllSourcesWithDefaultHost = "TestExecution.RunAllWithDefaultHost";
    public const string TestRunStatsChange = "TestExecution.StatsChange";
    public const string ExecutionComplete = "TestExecution.Completed";
}
