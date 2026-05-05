// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETCOREAPP

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.Common.DataCollection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Payloads;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;

/// <summary>
/// Source-generated <see cref="JsonSerializerContext"/> that provides STJ type metadata
/// without runtime reflection. Required for NativeAOT consumers where reflection-based
/// serialization is disabled by default.
///
/// Every type that flows through <see cref="JsonDataSerializer"/> must be listed here.
/// Custom converters (TestCaseConverterV2, TestPropertyConverter, etc.) are registered
/// on the <see cref="JsonSerializerOptions"/> separately — this context provides the
/// fallback metadata for types the converters delegate to STJ for.
/// </summary>
// --- Primitive / built-in types used as payloads ---
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(double))]
// --- Envelope DTOs (serialization-only, used by JsonDataSerializer) ---
[JsonSerializable(typeof(JsonElement))]
// --- ObjectModel types that cross the wire ---
[JsonSerializable(typeof(TestCase))]
[JsonSerializable(typeof(TestResult))]
[JsonSerializable(typeof(TestProperty))]
[JsonSerializable(typeof(TestObject))]
[JsonSerializable(typeof(AttachmentSet))]
[JsonSerializable(typeof(TestRunStatistics))]
[JsonSerializable(typeof(IEnumerable<TestCase>))]
[JsonSerializable(typeof(List<TestCase>))]
[JsonSerializable(typeof(TestCase[]))]
[JsonSerializable(typeof(IEnumerable<TestResult>))]
[JsonSerializable(typeof(List<KeyValuePair<TestProperty, object>>))]
[JsonSerializable(typeof(IEnumerable<AttachmentSet>))]
[JsonSerializable(typeof(List<AttachmentSet>))]
// --- Payload types deserialized by VsTestConsoleRequestSender ---
[JsonSerializable(typeof(DiscoveryCompletePayload))]
[JsonSerializable(typeof(DiscoveryRequestPayload))]
[JsonSerializable(typeof(TestMessagePayload))]
[JsonSerializable(typeof(TestRunCompletePayload))]
[JsonSerializable(typeof(TestRunChangedEventArgs))]
[JsonSerializable(typeof(TestRunStatsPayload))]
[JsonSerializable(typeof(StartTestSessionAckPayload))]
[JsonSerializable(typeof(StopTestSessionAckPayload))]
[JsonSerializable(typeof(TestProcessStartInfo))]
[JsonSerializable(typeof(EditorAttachDebuggerPayload))]
[JsonSerializable(typeof(TelemetryEvent))]
[JsonSerializable(typeof(TestRunAttachmentsProcessingCompletePayload))]
[JsonSerializable(typeof(TestRunAttachmentsProcessingProgressPayload))]
[JsonSerializable(typeof(BeforeTestRunStartPayload))]
[JsonSerializable(typeof(TestHostLaunchedPayload))]
[JsonSerializable(typeof(TestProcessAttachDebuggerPayload))]
[JsonSerializable(typeof(BeforeTestRunStartResult))]
[JsonSerializable(typeof(Collection<AttachmentSet>))]
// --- Collection / dictionary types used in payloads ---
[JsonSerializable(typeof(IDictionary<string, object>))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(IDictionary<string, string>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(Dictionary<string, HashSet<string>>))]
[JsonSerializable(typeof(IList<string>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(IEnumerable<string>))]
[JsonSerializable(typeof(Uri))]
// --- Event args ---
[JsonSerializable(typeof(TestRunCompleteEventArgs))]
[JsonSerializable(typeof(TestRunAttachmentsProcessingCompleteEventArgs))]
[JsonSerializable(typeof(AfterTestRunEndResult))]
[JsonSerializable(typeof(TestSessionInfo))]
[JsonSerializable(typeof(DiscoveryCriteria))]
[JsonSerializable(typeof(TestRunCriteria))]
// --- Internal envelope DTOs used by JsonDataSerializer ---
[JsonSerializable(typeof(JsonDataSerializer.MessageEnvelope))]
[JsonSerializable(typeof(JsonDataSerializer.VersionedMessageEnvelope))]
[JsonSerializable(typeof(JsonDataSerializer.VersionedMessageForSerialization))]
// --- Payload types SENT by VsTestConsoleRequestSender ---
[JsonSerializable(typeof(TestRunRequestPayload))]
[JsonSerializable(typeof(StartTestSessionPayload))]
[JsonSerializable(typeof(StopTestSessionPayload))]
[JsonSerializable(typeof(TestRunAttachmentsProcessingPayload))]
[JsonSerializable(typeof(CustomHostLaunchAckPayload))]
[JsonSerializable(typeof(EditorAttachDebuggerAckPayload))]
// --- PayloadedMessage<T> for each deserialized payload type ---
[JsonSerializable(typeof(JsonDataSerializer.PayloadedMessage<int>))]
[JsonSerializable(typeof(JsonDataSerializer.PayloadedMessage<TestMessagePayload>))]
[JsonSerializable(typeof(JsonDataSerializer.PayloadedMessage<IEnumerable<TestCase>>))]
[JsonSerializable(typeof(JsonDataSerializer.PayloadedMessage<List<TestCase>>))]
[JsonSerializable(typeof(JsonDataSerializer.PayloadedMessage<DiscoveryCompletePayload>))]
[JsonSerializable(typeof(JsonDataSerializer.PayloadedMessage<TestRunCompletePayload>))]
[JsonSerializable(typeof(JsonDataSerializer.PayloadedMessage<TestRunChangedEventArgs>))]
[JsonSerializable(typeof(JsonDataSerializer.PayloadedMessage<StartTestSessionAckPayload>))]
[JsonSerializable(typeof(JsonDataSerializer.PayloadedMessage<StopTestSessionAckPayload>))]
[JsonSerializable(typeof(JsonDataSerializer.PayloadedMessage<TestProcessStartInfo>))]
[JsonSerializable(typeof(JsonDataSerializer.PayloadedMessage<EditorAttachDebuggerPayload>))]
[JsonSerializable(typeof(JsonDataSerializer.PayloadedMessage<TelemetryEvent>))]
[JsonSerializable(typeof(JsonDataSerializer.PayloadedMessage<TestRunAttachmentsProcessingCompletePayload>))]
[JsonSerializable(typeof(JsonDataSerializer.PayloadedMessage<TestRunAttachmentsProcessingProgressPayload>))]
internal partial class TestPlatformJsonContext : JsonSerializerContext
{
}

#endif
