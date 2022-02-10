﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;

using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

using vstest.ProgrammerTests.CommandLine;

namespace vstest.ProgrammerTests.Fakes;

internal class FakeCommunicationChannel : ICommunicationChannel
{
    // The naming here is a bit confusing when this is implemented in process.
    // Normally one side in one process would have one end of the communication channel,
    // and would use Send to pass message to another process. The other side would get notified
    // about new data by NotifyDataAvailable, read the data there, and send them to other consumers
    // using MessageReceived event. These consumers would then call Send, the channel and post the data
    // to the other process. The other process would recieve the data and be notified by MessageReceived.
    //
    // But when we are in the same process, one side sends data using Send. We recieve them here by reading
    // them from the queue (NotifyDataAvailable is not used, because we monitor the queue directly here, instead of
    // in communication manager). And then we reply back to the sender, by invoking MessageReceived.

    public BlockingCollection<string> InQueue { get; } = new();
    public BlockingCollection<FakeMessage> OutQueue { get; } = new();
    public Task ProcessIncomingMessages { get; }
    public Task ProcessOutgoingMessages { get; }

    /// <summary>
    /// True if we encountered unexpected message (e.g. unknown message, message out of order) or when we sent all our prepared responses, and there were still more requests coming.
    /// </summary>
    public bool Faulted { get; private set; }

    public List<RequestResponsePair<Message, FakeMessage>> ProcessedMessages { get; } = new();

    /// <summary>
    /// Queue of MessageType of the incoming request, and the response that will be sent back.
    /// </summary>
    public Queue<RequestResponsePair<string, FakeMessage>> NextResponses { get; } = new();
    public FakeErrorAggregator FakeErrorAggregator { get; }
    public FakeMessage? OutgoingMessage { get; private set; }

    public CancellationTokenSource CancellationTokenSource = new();

    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    public FakeCommunicationChannel(List<RequestResponsePair<string, FakeMessage>> responses, FakeErrorAggregator fakeErrorAggregator)
    {
        FakeErrorAggregator = fakeErrorAggregator;

        responses.ForEach(NextResponses.Enqueue);

        ProcessIncomingMessages = Task.Run(() =>
        {
            var token = CancellationTokenSource.Token;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var rawMessage = InQueue.Take(token);
                    var requestMessage = JsonDataSerializer.Instance.DeserializeMessage(rawMessage);

                    if (Faulted)
                    {
                        // We already failed, when there are more requests coming, just save them and respond with error. We want to avoid
                        // a situation where server ignores our error message and responds with another request, for which we accidentally
                        // have the right answer in queue.
                        //
                        // E.g. We have VersionCheck, TestRunStart prepared, and server sends: VersionCheck, TestInitialize, TestRunStart.
                        // The first request has a valid response. The next TestInitialize does not have a valid response and errors out,
                        // but the server ignores it, and sends TestRunStart, which would normally have a prepared response, and lead to
                        // possibly overlooking the error response to TestInitialize.
                        //
                        // With this check in place we will not respond to TestRunStart with success, but with error.
                        // TODO: Better way to map MessageType and the payload type.
                        // TODO: simpler way to report error, and add it to the error aggregator
                        var errorMessage = $"FakeCommunicationChannel: FakeCommunicationChannel: Got message {requestMessage.MessageType}. But a message that was unexptected was received previously and the channel is now faulted. Review {nameof(ProcessedMessages)}, and {nameof(NextResponses)}.";
                        var exception = new Exception(errorMessage);
                        FakeErrorAggregator.Add(exception);
                        var errorResponse = new FakeMessage<TestMessagePayload>(MessageType.TestMessage, new TestMessagePayload { MessageLevel = TestMessageLevel.Error, Message = errorMessage });
                        ProcessedMessages.Add(new RequestResponsePair<Message, FakeMessage>(requestMessage, errorResponse));
                        Faulted = true;
                        OutQueue.Add(errorResponse);
                    }

                    // Just peek at it so we can keep the message on the the queue in case of error.
                    if (!NextResponses.TryPeek(out var nextResponsePair))
                    {
                        // If there are no more prepared responses then return protocol error.
                        var errorMessage = $"FakeCommunicationChannel: Got message {requestMessage.MessageType}, but no more requests were expected, because there are no more responses in {nameof(NextResponses)}.";
                        var exception = new Exception(errorMessage);
                        FakeErrorAggregator.Add(exception);
                        var errorResponse = new FakeMessage<TestMessagePayload>(MessageType.ProtocolError, new TestMessagePayload { MessageLevel = TestMessageLevel.Error, Message = errorMessage });
                        ProcessedMessages.Add(new RequestResponsePair<Message, FakeMessage>(requestMessage, errorResponse));
                        Faulted = true;
                        OutQueue.Add(errorResponse);
                    }
                    else if (nextResponsePair.Request != requestMessage.MessageType)
                    {
                        // If the incoming message does not match what we expected return protocol error. The lsat message will remain in the
                        // NextResponses queue.
                        var errorMessage = $"FakeCommunicationChannel: Excpected message {nextResponsePair.Request} but got {requestMessage.MessageType}.";
                        var exception = new Exception(errorMessage);
                        FakeErrorAggregator.Add(exception);
                        var errorResponse = new FakeMessage<TestMessagePayload>(MessageType.ProtocolError, new TestMessagePayload { MessageLevel = TestMessageLevel.Error, Message = errorMessage });
                        ProcessedMessages.Add(new RequestResponsePair<Message, FakeMessage>(requestMessage, errorResponse));
                        Faulted = true;
                        OutQueue.Add(errorResponse);
                    }
                    else
                    {
                        var responsePair = NextResponses.Dequeue();
                        if (responsePair.Debug && Debugger.IsAttached)
                        {
                            // We are about to send an interesting message
                            Debugger.Break();
                        }

                        // TODO: passing the raw message in, is strange
                        var responses = responsePair.GetResponse(rawMessage)!;
                        ProcessedMessages.Add(new RequestResponsePair<Message, FakeMessage>(requestMessage, responses));

                        foreach (var response in responses)
                        {
                            // If we created a pair with NoResponse message, we won't send that back to the server.
                            if (response != FakeMessage.NoResponse)
                            {
                                OutQueue.Add(response);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    FakeErrorAggregator.Add(ex);
                }
            }

        }, CancellationTokenSource.Token);

        ProcessOutgoingMessages = Task.Run(() =>
        {
            var token = CancellationTokenSource.Token;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // TODO: better name? this is message that we are currently trying to send
                    OutgoingMessage = OutQueue.Take();
                    // This is still a race condition. In real code we solve this via SafeInvoke that does null check
                    // and catches the exception. In this code I prefer doing it this way, to see if it is fragile.
                    if (MessageReceived != null)
                    {
                        MessageReceived(this, new MessageReceivedEventArgs { Data = OutgoingMessage.SerializedMessage });
                    }
                    OutgoingMessage = null;
                }
                catch (Exception ex)
                {
                    FakeErrorAggregator.Add(ex);
                }
            }

        }, CancellationTokenSource.Token);
    }

    public void Dispose()
    {
        CancellationTokenSource.Cancel();
        InQueue.CompleteAdding();
    }

    public Task NotifyDataAvailable()
    {
        // This is used only by communication manager. vstest.console does not use communication manager.
        throw new NotImplementedException();
    }

    public Task Send(string data)
    {
        InQueue.Add(data);
        return Task.CompletedTask;
    }
}

internal class RequestResponsePair<T, U> where T : class
{
    public RequestResponsePair(T request, U response, bool debug = false)
    {
        Request = request;
        Responses = new List<U> { response };
        Debug = debug;
    }

    public RequestResponsePair(T request, IEnumerable<U> responses, bool debug = false)
    {
        Request = request;
        Responses = responses.ToList();
        Debug = debug;
    }

    public RequestResponsePair(T request, Func<T, List<U>> responseFactory, bool debug = false)
    {
        Request = request;
        ResponseFactory = responseFactory;
        Debug = debug;
    }

    public T Request { get; }

    // TODO: make this Expression<Func< so we can get some info about what this is doing when looking directly at this instance
    public Func<T, List<U>>? ResponseFactory { get; }
    public List<U> Responses { get; private set; }

    public bool Debug { get; init; }

    // TODO: Let's sleep on this and see if I understand tomorrow what I was trying to do, because this has too many usages now...
    // One day later I do remember it, but I am still not convinced. But let's keep that for now. The idea here is to get either a canned
    // response or a response generated based on the incoming request (e.g. version comes in that is 3, response is lower version (2)).
    // both of these could be done by just executing Func, but that is not readable during debug time. Or maybe some variation on Either<>
    // but that seems as a very foreign concept to common C#.
    public List<U> GetResponse(T? request = null)
    {
        if (ResponseFactory != null)
        {
            // TODO: what am I doing wrong? (Why do I need that '!' ? I assign Request in both ctors and don't have setter, is the null coalescing not
            // supposed to propagate non-nullable type to the target type?
            // TODO: I don't like rewriting the response here. This is yet another sign that I am possibly mixing concepts in this class.
            Responses = ResponseFactory(request ?? Request!);
            return Responses;
        }
        else
        {
            // TODO: split this class to two that has the same parent, so we are sure we have a response in any case.
            return Responses!;
        }
    }
}

/// <summary>
/// A class like Message / VersionedMessage that is easier to create and review during debugging.
/// </summary>
internal sealed class FakeMessage<T> : FakeMessage
{
    public FakeMessage(string messageType, T payload, int version = 0)
    {
        MessageType = messageType;
        Payload = payload;
        Version = version;
        SerializedMessage = JsonDataSerializer.Instance.SerializePayload(MessageType, payload, version);
    }

    /// <summary>
    /// Message identifier, usually coming from the MessageType class.
    /// </summary>
    public string MessageType { get; }

    /// <summary>
    /// The payload that this message is holding.
    /// </summary>
    public T Payload { get; }

    /// <summary>
    /// Version of the message to allow the internal serializer to choose the correct serialization strategy.
    /// </summary>
    public int Version { get; }
}

/// <summary>
/// Marker for Fake message so we can put put all FakeMessages into one collection, without making it too wide.
/// </summary>
internal abstract class FakeMessage
{
    /// <summary>
    /// The message serialized using the default JsonDataSerializer.
    /// </summary>
    // TODO: Is there a better way to ensure that is is not null, we will always set it in the inherited types, but it would be nice to have warning if we did not.
    // And adding constructor makes it difficult to use the serializer, especially if we wanted to the serializer dynamic and not a static instance.
    public string SerializedMessage { get; init; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    public static FakeMessage NoResponse { get; } = new FakeMessage<int>("NoResponse", 0);
}
