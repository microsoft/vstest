// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// This file is vendored from https://github.com/Youssef1313/MTPSharding (YTest.MTP.PipeProtocol),
// used under the MIT license with the author's permission. See THIRD-PARTY-NOTICES.txt.
#nullable enable

using System;
#if NET
using System.Buffers;
#endif
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.MTP.PipeProtocol;

internal sealed class NamedPipeServer : NamedPipeBase
{
    private static bool IsUnix => Path.DirectorySeparatorChar == '/';

    private readonly Func<NamedPipeServer, IRequest, Task<IResponse>> _callback;
    private readonly NamedPipeServerStream _namedPipeServerStream;
    private readonly CancellationToken _cancellationToken;
    private readonly MemoryStream _serializationBuffer = new();
    private readonly MemoryStream _messageBuffer = new();
    private readonly byte[] _readBuffer = new byte[250000];
    private Task? _loopTask;
    private bool _disposed;

    public NamedPipeServer(
        string pipeName,
        Func<NamedPipeServer, IRequest, Task<IResponse>> callback,
        int maxNumberOfServerInstances,
        CancellationToken cancellationToken)
    {
        PipeName = pipeName;
        _namedPipeServerStream = new NamedPipeServerStream(
            pipeName,
            PipeDirection.InOut,
            maxNumberOfServerInstances,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous,
            inBufferSize: 0,
            outBufferSize: 0);
        _callback = callback;
        _cancellationToken = cancellationToken;
    }

    public string PipeName { get; private set; }

    [MemberNotNullWhen(true, nameof(_loopTask))]
    public bool WasConnected { get; private set; }

    public async Task WaitConnectionAsync(CancellationToken cancellationToken)
    {
        await _namedPipeServerStream.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
        WasConnected = true;
        _loopTask = Task.Run(
            async () =>
            {
                try
                {
                    await InternalLoopAsync(_cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException ex) when (ex.CancellationToken == _cancellationToken)
                {
                    // We are being cancelled, so we don't need to wait anymore
                    return;
                }
            }, cancellationToken);
    }

    /// <summary>
    /// 4 bytes = message size
    /// ------- Payload -------
    /// 4 bytes = serializer id
    /// x bytes = object buffer.
    /// </summary>
    private async Task InternalLoopAsync(CancellationToken cancellationToken)
    {
        int currentMessageSize = 0;
        int missingBytesToReadOfWholeMessage = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            int missingBytesToReadOfCurrentChunk = 0;
            int currentReadIndex = 0;
#if NET
            int currentReadBytes = await _namedPipeServerStream.ReadAsync(_readBuffer.AsMemory(currentReadIndex, _readBuffer.Length), cancellationToken).ConfigureAwait(false);
#else
            int currentReadBytes = await _namedPipeServerStream.ReadAsync(_readBuffer, currentReadIndex, _readBuffer.Length, cancellationToken).ConfigureAwait(false);
#endif
            if (currentReadBytes == 0)
            {
                // The client has disconnected
                return;
            }

            // Reset the current chunk size
            missingBytesToReadOfCurrentChunk = currentReadBytes;

            // If currentRequestSize is 0, we need to read the message size
            if (currentMessageSize == 0)
            {
                // We need to read the message size, first 4 bytes
                currentMessageSize = BitConverter.ToInt32(_readBuffer, 0);
                missingBytesToReadOfCurrentChunk = currentReadBytes - sizeof(int);
                missingBytesToReadOfWholeMessage = currentMessageSize;
                currentReadIndex = sizeof(int);
            }

            if (missingBytesToReadOfCurrentChunk > 0)
            {
                // We need to read the rest of the message
#if NET
                await _messageBuffer.WriteAsync(_readBuffer.AsMemory(currentReadIndex, missingBytesToReadOfCurrentChunk), cancellationToken).ConfigureAwait(false);
#else
                await _messageBuffer.WriteAsync(_readBuffer, currentReadIndex, missingBytesToReadOfCurrentChunk, cancellationToken).ConfigureAwait(false);
#endif
                missingBytesToReadOfWholeMessage -= missingBytesToReadOfCurrentChunk;
            }

            // If we have read all the message, we can deserialize it
            if (missingBytesToReadOfWholeMessage == 0)
            {
                // Deserialize the message
                _messageBuffer.Position = 0;

                // Get the serializer id
                int serializerId = BitConverter.ToInt32(_messageBuffer.GetBuffer(), 0);

                // Get the serializer
                INamedPipeSerializer requestNamedPipeSerializer = GetSerializer(serializerId, skipUnknownMessages: true);

                // Deserialize the message
                _messageBuffer.Position += sizeof(int); // Skip the serializer id
                var deserializedObject = (IRequest)requestNamedPipeSerializer.Deserialize(_messageBuffer);

                // Call the callback
                IResponse response = await _callback(this, deserializedObject).ConfigureAwait(false);

                // Write the message size
                _messageBuffer.Position = 0;

                // Get the response serializer
                INamedPipeSerializer responseNamedPipeSerializer = GetSerializer(response.GetType());

                // Serialize the response
                responseNamedPipeSerializer.Serialize(response, _serializationBuffer);

                // The length of the message is the size of the message plus one byte to store the serializer id
                // Space for the message
                int sizeOfTheWholeMessage = (int)_serializationBuffer.Position;

                // Space for the serializer id
                sizeOfTheWholeMessage += sizeof(int);

                // Write the message size
#if NET
                byte[] bytes = ArrayPool<byte>.Shared.Rent(sizeof(int));
                try
                {
                    BitConverter.TryWriteBytes(bytes, sizeOfTheWholeMessage);

                    await _messageBuffer.WriteAsync(bytes.AsMemory(0, sizeof(int)), cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(bytes);
                }
#else
                await _messageBuffer.WriteAsync(BitConverter.GetBytes(sizeOfTheWholeMessage), 0, sizeof(int), cancellationToken).ConfigureAwait(false);
#endif

                // Write the serializer id
#if NET
                bytes = ArrayPool<byte>.Shared.Rent(sizeof(int));
                try
                {
                    BitConverter.TryWriteBytes(bytes, responseNamedPipeSerializer.Id);

                    await _messageBuffer.WriteAsync(bytes.AsMemory(0, sizeof(int)), cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(bytes);
                }
#else
                await _messageBuffer.WriteAsync(BitConverter.GetBytes(responseNamedPipeSerializer.Id), 0, sizeof(int), cancellationToken).ConfigureAwait(false);
#endif

                // Write the message
#if NET
                await _messageBuffer.WriteAsync(_serializationBuffer.GetBuffer().AsMemory(0, (int)_serializationBuffer.Position), cancellationToken).ConfigureAwait(false);
#else
                await _messageBuffer.WriteAsync(_serializationBuffer.GetBuffer(), 0, (int)_serializationBuffer.Position, cancellationToken).ConfigureAwait(false);
#endif

                // Send the message
                try
                {
#if NET
                    await _namedPipeServerStream.WriteAsync(_messageBuffer.GetBuffer().AsMemory(0, (int)_messageBuffer.Position), cancellationToken).ConfigureAwait(false);
#else
                    await _namedPipeServerStream.WriteAsync(_messageBuffer.GetBuffer(), 0, (int)_messageBuffer.Position, cancellationToken).ConfigureAwait(false);
#endif
                    await _namedPipeServerStream.FlushAsync(cancellationToken).ConfigureAwait(false);
#if NET
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        _namedPipeServerStream.WaitForPipeDrain();
                    }
#else
                    // .NET Framework only runs on Windows, so the pipe drain always applies.
                    _namedPipeServerStream.WaitForPipeDrain();
#endif
                }
                finally
                {
                    // Reset the buffers
                    _messageBuffer.Position = 0;
                    _serializationBuffer.Position = 0;
                }

                // Reset the control variables
                currentMessageSize = 0;
                missingBytesToReadOfWholeMessage = 0;
            }
        }
    }

    public static string GetPipeName(string name)
    {
        if (!IsUnix)
        {
            return $"testingplatform.pipe.{name.Replace('\\', '.')}";
        }

        // Similar to https://github.com/dotnet/roslyn/blob/99bf83c7bc52fa1ff27cf792db38755d5767c004/src/Compilers/Shared/NamedPipeUtil.cs#L26-L42
        return Path.Combine("/tmp", name);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            if (WasConnected)
            {
                // If the loop task is null at this point we have race condition, means that the task didn't start yet and we already dispose.
                // This is unexpected and we throw an exception.

                // To close gracefully we need to ensure that the client closed the stream line 103.
                if (!_loopTask!.Wait(TimeSpan.FromSeconds(90)))
                {
                    throw new InvalidOperationException("Loop task couldn't finish in timely manner");
                }
            }
        }
        finally
        {
            // Ensure we are still disposing the resouces correctly, even if _loopTask completes with
            // an exception, or if the task doesn't complete within the 90 seconds limit.
            _namedPipeServerStream.Dispose();
            _disposed = true;
        }
    }
}
