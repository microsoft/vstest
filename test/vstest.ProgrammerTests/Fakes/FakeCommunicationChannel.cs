// Copyright (c) Microsoft Corporation. All rights reserved.
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
    public Task Spin { get; }

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

    public CancellationTokenSource CancellationTokenSource = new();

    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    public FakeCommunicationChannel(FakeErrorAggregator fakeErrorAggregator)
    {
        FakeErrorAggregator = fakeErrorAggregator;

        NextResponses.Enqueue(new RequestResponsePair<string, FakeMessage>(MessageType.VersionCheck, new FakeMessage<int>(MessageType.VersionCheck, 5)));
        NextResponses.Enqueue(new RequestResponsePair<string, FakeMessage>(MessageType.ExecutionInitialize, FakeMessage.NoResponse));
        var result = new TestRunCompletePayload
        {
            TestRunCompleteArgs = new TestRunCompleteEventArgs(new TestRunStatistics(), false, false, null, new System.Collections.ObjectModel.Collection<AttachmentSet>(), TimeSpan.Zero),
            LastRunTests = new TestRunChangedEventArgs(new TestRunStatistics(), new List<TestResult>(), new List<TestCase>()),
        };
        NextResponses.Enqueue(new RequestResponsePair<string, FakeMessage>(MessageType.StartTestExecutionWithSources, new FakeMessage<TestRunCompletePayload>(MessageType.ExecutionComplete, result), true));

        Spin = Task.Run(async () =>
        {
            var token = CancellationTokenSource.Token;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Only consume messages if someone is listening on the other side.
                    if (MessageReceived != null)
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
                            Respond(errorResponse);
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
                            Respond(errorResponse);
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
                            Respond(errorResponse);
                        }
                        else
                        {
                            var responsePair = NextResponses.Dequeue();
                            if (responsePair.Debug && Debugger.IsAttached)
                            {
                                // We are about to send an interesting message
                                Debugger.Break();
                            }
                            // TODO: remove !, once we fix the type
                            var response = responsePair.Response!;
                            ProcessedMessages.Add(new RequestResponsePair<Message, FakeMessage>(requestMessage, response));

                            // If we created a pair with NoResponse message, we won't send that back to the server.
                            if (response != FakeMessage.NoResponse)
                            {
                                Respond(response);
                            }
                        }
                    }
                    else
                    {
                        await Task.Delay(100);
                    }
                }
                catch (Exception ex)
                {
                    FakeErrorAggregator.Add(ex);
                }
            }

        }, CancellationTokenSource.Token);
    }

    private void Respond(FakeMessage response)
    {
        // We started processing the message when there was still someone listenting, but processing the message
        // took us some time and the listener might have unsubscribed. check again if anyone is interested in the
        // data. This is still a race condondition. In real code we solve this via SafeInvoke that does null check
        // and catches the exception. In this code I prefer doing it this way, to see if it is fragile.
        //
        // The data from the message will be lost if the listener unsubscribes. This is okay, as it is similar to writing data
        // to network stream while the other side disconnects. This is purely issue of timing, that can never be avoided.
        if (MessageReceived != null)
        {
            MessageReceived(this, new MessageReceivedEventArgs { Data = response.SerializedMessage });
        }
        //TODO: record unprocessed responses?
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
        Response = response;
        Debug = debug;
    }

    public RequestResponsePair(T request, Func<T, U> responseFactory, bool debug = false)
    {
        Request = request;
        ResponseFactory = responseFactory;
        Debug = debug;
    }

    public T Request { get; }

    // TODO: make this Expression<Func< so we can get some info about what this is doing when looking directly at this instance
    public Func<T, U>? ResponseFactory { get; }
    public U? Response { get; private set; }

    public bool Debug { get; init; }

    // TODO: Let's sleep on this and see if I understand tomorrow what I was trying to do, because this has too many usages now...
    // One day later I do remember it, but I am still not convinced. But let's keep that for now. The idea here is to get either a canned
    // response or a response generated based on the incoming request (e.g. version comes in that is 3, response is lower version (2)).
    // both of these could be done by just executing Func, but that is not readable during debug time. Or maybe some variation on Either<>
    // but that seems as a very foreign concept to common C#.
    public U GetResponse(T? request = null)
    {
        if (ResponseFactory != null)
        {
            // TODO: what am I doing wrong? (Why do I need that '!' ? I assign Request in both ctors and don't have setter, is the null coalescing not
            // supposed to propagate non-nullable type to the target type?
            // TODO: I don't like rewriting the response here. This is yet another sign that I am possibly mixing concepts in this class.
            Response = ResponseFactory(request ?? Request!);
            return Response;
        }
        else
        {
            // TODO: split this class to two that has the same parent, so we are sure we have a response in any case.
            return Response!;
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
    /// When paired with this message, the channel should only recieve the request but not send this message back to the caller. 
    /// </summary>
    public static FakeMessage NoResponse { get; }
}
