// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.ProgrammerTests.CommandLine;

using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

using vstest.ProgrammerTests.Fakes;

/// <summary>
/// Builds a list of RequestResponse pairs, with the provided values. Each method is a name of incoming message type.
/// The order in which the builder methods are called determines the order or responses.
/// </summary>
internal class FakeMessagesBuilder
{
    private readonly List<RequestResponsePair<string, FakeMessage>> _responses = new();

    /// <summary>
    /// For VersionCheck message it responds with VersionCheck response that has the given version.
    /// </summary>
    /// <param name="version"></param>
    /// <returns></returns>
    internal FakeMessagesBuilder VersionCheck(int version)
    {
        AddPair(MessageType.VersionCheck, version);
        return this;
    }

    /// <summary>
    /// For VersionCheck message it responds with the given FakeMessage.
    /// </summary>
    /// <param name="message">Message to respond with, or FakeMessage.NoResponse to not respond.</param>
    /// <returns></returns>
    internal FakeMessagesBuilder VersionCheck(FakeMessage message)
    {
        AddPair(MessageType.VersionCheck, message);
        return this;
    }

    /// <summary>
    /// For VersionCheck message it does the given before action and responds with the given FakeMessage and then does the given after action.
    /// Use FakeMessage.NoResponse to not respond.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    internal FakeMessagesBuilder VersionCheck(Action<string> beforeAction, FakeMessage message, Action<string> afterAction)
    {
        AddPair(MessageType.VersionCheck, message, beforeAction, afterAction);
        return this;
    }

    internal FakeMessagesBuilder ExecutionInitialize(FakeMessage message)
    {
        AddPair(MessageType.ExecutionInitialize, message);
        return this;
    }

    internal FakeMessagesBuilder StartTestExecutionWithSources(List<List<TestResult>> testResultBatches)
    {
        var tests = testResultBatches;
        // this will create as many test stats changes messages, as there are batches -1
        // the last batch will be sent as test run complete event

        // TODO: make the stats agree with the tests below
        List<FakeMessage> changeMessages = tests.Take(tests.Count - 1).Select(batch =>
            new FakeMessage<TestRunChangedEventArgs>(MessageType.TestRunStatsChange,
                  new TestRunChangedEventArgs(new TestRunStatistics(new Dictionary<TestOutcome, long> { [TestOutcome.Passed] = batch.Count }), batch, new List<TestCase>())
                 )).ToList<FakeMessage>();
        FakeMessage completedMessage = new FakeMessage<TestRunCompletePayload>(MessageType.ExecutionComplete, new TestRunCompletePayload
        {
            // TODO: make the stats agree with the tests below
            TestRunCompleteArgs = new TestRunCompleteEventArgs(new TestRunStatistics(new Dictionary<TestOutcome, long> { [TestOutcome.Passed] = 1 }), false, false, null, new System.Collections.ObjectModel.Collection<AttachmentSet>(), TimeSpan.Zero),
            LastRunTests = new TestRunChangedEventArgs(new TestRunStatistics(new Dictionary<TestOutcome, long> { [TestOutcome.Passed] = 1 }), tests.Last(), new List<TestCase>()),
        });
        List<FakeMessage> messages = changeMessages.Concat(new[] { completedMessage }).ToList();

        AddPairWithMultipleMessages(MessageType.StartTestExecutionWithSources, messages);
        return this;
    }

    
    internal FakeMessagesBuilder SessionEnd(FakeMessage fakeMessage)
    {
        AddPair(MessageType.SessionEnd, fakeMessage);
        return this;
    }

    internal FakeMessagesBuilder SessionEnd(FakeMessage message, Action<string>? beforeAction = null, Action<string>? afterAction = null)
    {
        AddPair(MessageType.SessionEnd, message, beforeAction, afterAction);
        return this;
    }

    private void AddPair<T>(string messageType, T value, Action<string>? beforeAction = null, Action<string>? afterAction = null)
    {
        // TODO: add actions
        AddPair(messageType, new FakeMessage<T>(messageType, value), beforeAction, afterAction);
    }

    private void AddPair(string messageType, FakeMessage message, Action<string>? beforeAction = null, Action<string>? afterAction = null)
    {
        // TODO: add actions
        AddPairWithMultipleMessages(messageType, new[] { message }, beforeAction, afterAction);
    }

    // TODO: this uses different name, because it would never be chosen when we provide IEnumerable, the overload with T value is used instead. This is error prone, better design?
    private void AddPairWithMultipleMessages(string messageType, IEnumerable<FakeMessage> messages, Action<string>? beforeAction = null, Action<string>? afterAction = null)
    {
        // TODO: add after actions
        Func<string, List<FakeMessage>> callActionAndReturnMessages = m =>
        {
            if (beforeAction != null)
            {
                beforeAction(m);
            }
            return messages.ToList();
        };

        _responses.Add(new RequestResponsePair<string, FakeMessage>(messageType, callActionAndReturnMessages));
    }

    internal List<RequestResponsePair<string, FakeMessage>> Build()
    {
        return _responses;
    }
}
