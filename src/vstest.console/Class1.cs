using StreamJsonRpc;
using System;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace Server
{
    class RpcServer
    {
        public async Task ServeAsync()
        {
            int clientId = 0;
            while (true)
            {
                Console.WriteLine("Waiting for client to make a connection...");
                var stream = new NamedPipeServerStream("StreamJsonRpcSamplePipe", PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await stream.WaitForConnectionAsync();
                Task nowait = ResponseToRpcRequestsAsync(stream, ++clientId);
            }
        }

        private static async Task ResponseToRpcRequestsAsync(NamedPipeServerStream stream, int clientId)
        {
            Console.WriteLine($"Connection request #{clientId} received. Spinning off an async Task to cater to requests.");
            var jsonRpc = JsonRpc.Attach(stream, new Worker());
            Console.WriteLine($"JSON-RPC listener attached to #{clientId}. Waiting for requests...");
            await jsonRpc.Completion;
            Console.WriteLine($"Connection #{clientId} terminated.");
        }
    }

    class Worker
    {
        public int Add(int a, int b)
        {
            Console.WriteLine($"Received request: {a} + {b}");
            return a + b;
        }
    }
}
