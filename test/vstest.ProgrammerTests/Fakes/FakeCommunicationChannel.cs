// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Linq.Expressions;

using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;

using Newtonsoft.Json;

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

    public List<string> UnknowsMessages { get; } = new();

    public List<RequestResponsePair<Message, Message>> ProcessedMessages { get; } = new();

    /// <summary>
    /// Queue of MessageType of the incoming request, and the response that will be sent back.
    /// </summary>
    public Queue<RequestResponsePair<string, Message>> NextResponses { get; } = new();

    public CancellationTokenSource CancellationTokenSource = new();

    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    public FakeCommunicationChannel()
    {
        NextResponses.Enqueue(new RequestResponsePair<string, Message>(MessageType.VersionCheck, new Message(MessageType.VersionCheck, 1)));
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
                        var message = JsonDataSerializer.Instance.DeserializeMessage(rawMessage);
                        if (message.MessageType == MessageType.VersionCheck)
                        {
                            // VersionCheck message expects a response with VersionCheck message,
                            // correctly the message will contain number that is the same or lower than the received number,
                            // so we use the highest version of protocol that both sides support
                            // TODO: I am just replying with the same message.
                            //
                            // Notifiy the listening side that there is new data.
                            MessageReceived(this, new MessageReceivedEventArgs { Data = rawMessage });
                        }
                        else
                        {
                            throw new InvalidOperationException($"unknown message, {message.MessageType}, {message.Payload}");
                        }

                    }
                    else
                    {
                        await Task.Delay(100);
                    }
                }
                catch
                {

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

internal class RequestResponsePair<T, U>
{
    public RequestResponsePair(T request, U response)
    {
        Request = request;
        Response = response;
    }

    public RequestResponsePair(T request, Expression<Func<T, U>> responseFactory)
    {
        Request = request;
        ResponseFactory = responseFactory;
    }

    public T Request { get; set; }

    // TODO: make this Expression<Func< so we can get some info about this was doing when looking directly at this instance
    public Func<T, U> ResponseFactory { get; }
    public U? Response { get; set; }

    // TODO: Let's sleep on this and see if I understand tomorrow what I was trying to do, because this has too many usages now...
    public GetResponse(T request = null)
    {
        if (ResponseFactory != null)
        {
            Response = ResponseFactory(request ?? Request);
            return Response;
        }
        else
        {
            return Response;
        }
    }
}
