
using StreamJsonRpc;
using System;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace Client
{
    // adopted from these examples with minor changes
    // https://github.com/AArnott/StreamJsonRpc.Sample/blob/master/StreamJsonRpc.Sample.Client/Client.cs
    // named pipeline is better now because I don't have to hookup a way to pass the name
    public class Class1
    {
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        public async Task Add(int l, int r)
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
        {
            Console.WriteLine("Connecting to server...");
            using (var stream = new NamedPipeClientStream(".", "StreamJsonRpcSamplePipe", PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                await stream.ConnectAsync();
                Console.WriteLine("Connected. Sending request...");
                var jsonRpc = JsonRpc.Attach(stream);
                int sum = await jsonRpc.InvokeAsync<int>("Add", l, r);
                Console.WriteLine($"{l} + {r} = {sum}");
                Console.WriteLine("Terminating stream...");
            }
        }
    }
}
