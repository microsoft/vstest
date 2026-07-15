// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// This file is vendored from https://github.com/Youssef1313/MTPSharding (YTest.MTP.PipeProtocol),
// used under the MIT license with the author's permission. See THIRD-PARTY-NOTICES.txt.
#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.MTP.PipeProtocol;

internal sealed class TestApplication : IDisposable
{
    private readonly string _pipeName = NamedPipeServer.GetPipeName(Guid.NewGuid().ToString("N"));
    private readonly string _pathToExe;
    private readonly string _arguments;
    private readonly string? _workingDirectory;
    private readonly IReadOnlyDictionary<string, string?>? _environmentVariables;
    private Task? _afterProcessStartTask;

    private readonly List<NamedPipeServer> _testAppPipeConnections = [];
    private readonly Dictionary<NamedPipeServer, HandshakeMessage> _handshakes = new();

    public TestApplication(string pathToExe, string arguments, string? workingDirectory = null, IReadOnlyDictionary<string, string?>? environmentVariables = null)
    {
        _pathToExe = pathToExe;
        _arguments = arguments;
        _workingDirectory = workingDirectory;
        _environmentVariables = environmentVariables;
    }

    public Func<DiscoveredTestMessages, Task>? OnDiscovered { get; set; }
    public Func<TestResultMessages, Task>? OnTestResult { get; set; }
    public Func<FileArtifactMessages, Task>? OnFileArtifact { get; set; }

    public async Task<TestProcessExitInformation> RunAsync(Func<int, Task>? afterProcessStartCallback = null, CancellationToken cancellationToken = default)
    {
        var processStartInfo = CreateProcessStartInfo(_pathToExe, _arguments, _workingDirectory);

        // The pipe accept-loop runs until we cancel it (after the process exits). Link it to the
        // caller's token so an aborted run also tears the loop down.
        var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var loopToken = cancellationTokenSource.Token;
        var testAppPipeConnectionLoop = Task.Run(async () => await WaitConnectionAsync(loopToken).ConfigureAwait(false));

        try
        {
            using var process = Process.Start(processStartInfo)!;

            // If the caller cancels the run, kill the MTP application so it tears down promptly instead
            // of blocking until it finishes on its own.
            using var killRegistration = cancellationToken.Register(static state =>
            {
                try
                {
                    var runningProcess = (Process)state!;
                    if (!runningProcess.HasExited)
                    {
                        runningProcess.Kill();
                    }
                }
                catch
                {
                    // Best effort: the process may have already exited.
                }
            }, process);

            if (afterProcessStartCallback is not null)
            {
                var afterProcessStartTask = afterProcessStartCallback(process.Id);
                _afterProcessStartTask = afterProcessStartTask;
                await afterProcessStartTask.ConfigureAwait(false);
            }

            // Reading from process stdout/stderr is done on separate threads to avoid blocking IO on the threadpool.
            // Note: even with 'process.StandardOutput.ReadToEndAsync()' or 'process.BeginOutputReadLine()', we ended up with
            // many TP threads just doing synchronous IO, slowing down the progress of the test run.
            // We want to read requests coming through the pipe and sending responses back to the test app as fast as possible.
            var stdOutTask = Task.Factory.StartNew(static standardOutput => ((StreamReader)standardOutput!).ReadToEnd(), process.StandardOutput, TaskCreationOptions.LongRunning);
            var stdErrTask = Task.Factory.StartNew(static standardError => ((StreamReader)standardError!).ReadToEnd(), process.StandardError, TaskCreationOptions.LongRunning);
            var outputAndError = await Task.WhenAll(stdOutTask, stdErrTask).ConfigureAwait(false);

            await process.WaitForExitAsync().ConfigureAwait(false);

            return new TestProcessExitInformation { StandardOutput = outputAndError[0], StandardError = outputAndError[1], ExitCode = process.ExitCode };
        }
        finally
        {
            cancellationTokenSource.Cancel();
            await testAppPipeConnectionLoop;
            cancellationTokenSource.Dispose();
        }
    }

    private ProcessStartInfo CreateProcessStartInfo(string pathToExe, string arguments, string? workingDirectory)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = pathToExe,
            Arguments = $"{arguments} --server dotnettestcli --dotnet-test-pipe {_pipeName}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        if (!string.IsNullOrEmpty(workingDirectory))
        {
            processStartInfo.WorkingDirectory = workingDirectory;
        }

        if (_environmentVariables is not null)
        {
            foreach (var variable in _environmentVariables)
            {
                if (variable.Value is null)
                {
                    processStartInfo.Environment.Remove(variable.Key);
                }
                else
                {
                    processStartInfo.Environment[variable.Key] = variable.Value;
                }
            }
        }

        return processStartInfo;
    }

    private async Task WaitConnectionAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                var pipeConnection = new NamedPipeServer(_pipeName, OnRequest, NamedPipeServerStream.MaxAllowedServerInstances, token);
                pipeConnection.RegisterAllSerializers();

                await pipeConnection.WaitConnectionAsync(token).ConfigureAwait(false);
                _testAppPipeConnections.Add(pipeConnection);
            }
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == token)
        {
        }
        catch (Exception ex)
        {
            Environment.FailFast(ex.ToString());
        }
    }

    private async Task<IResponse> OnRequest(NamedPipeServer server, IRequest request)
    {
        try
        {
            switch (request)
            {
                case HandshakeMessage handshakeMessage:
                    _handshakes.Add(server, handshakeMessage);
                    if (_afterProcessStartTask is not null)
                    {
                        SpinWait.SpinUntil(() => _afterProcessStartTask.IsCompleted);
                    }

                    if (handshakeMessage.Properties.TryGetValue(HandshakeMessagePropertyNames.ModulePath, out string? value))
                    {
                        return CreateHandshakeMessage(GetSupportedProtocolVersion(handshakeMessage));
                    }
                    break;

                case CommandLineOptionMessages commandLineOptionMessages:
                    break;

                case DiscoveredTestMessages discoveredTestMessages:
                    if (OnDiscovered is not null)
                    {
                        await OnDiscovered(discoveredTestMessages).ConfigureAwait(false);
                    }
                    break;

                case TestResultMessages testResultMessages:
                    if (OnTestResult is not null)
                    {
                        await OnTestResult(testResultMessages).ConfigureAwait(false);
                    }
                    break;

                case FileArtifactMessages fileArtifactMessages:
                    if (OnFileArtifact is not null)
                    {
                        await OnFileArtifact(fileArtifactMessages).ConfigureAwait(false);
                    }
                    break;

                case TestSessionEvent sessionEvent:
                    break;

                // If we don't recognize the message, log and skip it
                case UnknownMessage unknownMessage:
                    return VoidResponse.CachedInstance;

                default:
                    // If it doesn't match any of the above, throw an exception
                    throw new NotSupportedException($"Message Request type '{request.GetType()}' is unsupported.");
            }
        }
        catch (Exception ex)
        {
            Environment.FailFast(ex.ToString());
        }

        return VoidResponse.CachedInstance;
    }

    private static string GetSupportedProtocolVersion(HandshakeMessage handshakeMessage)
    {
        handshakeMessage.Properties.TryGetValue(HandshakeMessagePropertyNames.SupportedProtocolVersions, out string? protocolVersions);

        string version = string.Empty;
        if (protocolVersions is not null && protocolVersions.Split(';').Contains(ProtocolConstants.Version))
        {
            version = ProtocolConstants.Version;
        }

        return version;
    }

    private static HandshakeMessage CreateHandshakeMessage(string version)
    {
#if NET
        var processId = Environment.ProcessId.ToString();
        var architecture = RuntimeInformation.ProcessArchitecture.ToString();
        var frameworkDescription = RuntimeInformation.FrameworkDescription;
        var osDescription = RuntimeInformation.OSDescription;
#else
        using var process = Process.GetCurrentProcess();
        var processId = process.Id.ToString();
        // RuntimeInformation lives in a facade assembly that is not referenced on .NET Framework;
        // avoid taking that dependency (binding-redirect sensitive) and use equivalent fallbacks.
        var architecture = Environment.Is64BitProcess ? "X64" : "X86";
        var frameworkDescription = ".NET Framework";
        var osDescription = Environment.OSVersion.ToString();
#endif
        return new HandshakeMessage(new Dictionary<byte, string>
        {
            { HandshakeMessagePropertyNames.PID, processId },
            { HandshakeMessagePropertyNames.Architecture, architecture },
            { HandshakeMessagePropertyNames.Framework, frameworkDescription },
            { HandshakeMessagePropertyNames.OS, osDescription },
            { HandshakeMessagePropertyNames.SupportedProtocolVersions, version },
            { HandshakeMessagePropertyNames.IsIDE, "true" }, // TODO: Make it user configurable.
        });
    }


    public void Dispose()
    {
        Exception? exceptionAggregation = null;
        foreach (var namedPipeServer in _testAppPipeConnections)
        {
            try
            {
                namedPipeServer.Dispose();
            }
            catch (Exception ex)
            {
                if (_handshakes.TryGetValue(namedPipeServer, out var handshake))
                {
                    var messageBuilder = new StringBuilder("Error disposing NamedPipeServer corresponding to handshake:");
                    messageBuilder.AppendLine();
                    messageBuilder.AppendLine($"Test executable path: {_pathToExe}");
                    foreach (var kvp in handshake.Properties)
                    {
                        messageBuilder.AppendLine($"{kvp.Key}: {kvp.Value}");
                    }

                    ex = new Exception(messageBuilder.ToString(), ex);
                }
                else
                {
                    var messageBuilder = new StringBuilder("Error disposing NamedPipeServer, and no handshake was found.");
                    messageBuilder.AppendLine();
                    messageBuilder.AppendLine($"Test executable path: {_pathToExe}");
                    ex = new Exception(messageBuilder.ToString(), ex);
                }

                if (exceptionAggregation is null)
                {
                    exceptionAggregation = ex;
                }
                else
                {
                    if (exceptionAggregation is AggregateException aggregateException)
                    {
                        exceptionAggregation = new AggregateException(aggregateException.InnerExceptions.Concat(new[] { ex }));
                    }
                    else
                    {
                        exceptionAggregation = new AggregateException(exceptionAggregation, ex);
                    }
                }
            }
        }

        if (exceptionAggregation is not null)
        {
            throw exceptionAggregation;
        }
    }
}
