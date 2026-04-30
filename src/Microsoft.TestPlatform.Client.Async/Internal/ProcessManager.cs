// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.TestPlatform.Client.Async.Internal;

/// <summary>
/// Manages a vstest.console child process. Captures stderr asynchronously
/// and detects process exit immediately without blocking.
/// </summary>
internal sealed class ProcessManager : IDisposable
{
    private readonly Process _process;
    private readonly StringBuilder _errorOutput = new();
    private readonly TaskCompletionSource<int> _exitTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private ProcessManager(Process process)
    {
        _process = process;
    }

    public int ProcessId => _process.Id;

    public bool HasExited => _process.HasExited;

    public int? ExitCode => HasExited ? _process.ExitCode : null;

    public string ErrorOutput
    {
        get
        {
            lock (_errorOutput)
            {
                return _errorOutput.ToString();
            }
        }
    }

    /// <summary>
    /// Task that completes when the process exits. The result is the exit code.
    /// </summary>
    public Task<int> ExitedTask => _exitTcs.Task;

    /// <summary>
    /// Launch a vstest.console process in design mode, connecting back to the given port.
    /// </summary>
    public static ProcessManager Launch(string vstestConsolePath, int port)
    {
        int parentPid;
        using (var currentProcess = Process.GetCurrentProcess())
        {
            parentPid = currentProcess.Id;
        }

        var startInfo = new ProcessStartInfo
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };

        if (vstestConsolePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            startInfo.FileName = "dotnet";
            startInfo.Arguments = $"\"{vstestConsolePath}\" /Port:{port} /ParentProcessId:{parentPid}";
        }
        else
        {
            startInfo.FileName = vstestConsolePath;
            startInfo.Arguments = $"/Port:{port} /ParentProcessId:{parentPid}";
        }

        var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        var manager = new ProcessManager(process);

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                lock (manager._errorOutput)
                {
                    manager._errorOutput.AppendLine(e.Data);
                }
            }
        };

        // Drain stdout to avoid blocking the child process.
        process.OutputDataReceived += (_, _) => { };

        process.Exited += (_, _) =>
        {
            manager._exitTcs.TrySetResult(process.ExitCode);
        };

        process.Start();
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();

        return manager;
    }

    /// <summary>
    /// Create a <see cref="VsTestProcessExitedException"/> from the current process state.
    /// </summary>
    public VsTestProcessExitedException CreateExitException(string context)
    {
        string stderr = ErrorOutput;
        string message = string.IsNullOrWhiteSpace(stderr)
            ? $"vstest.console process (PID {ProcessId}) exited with code {ExitCode} during {context}."
            : $"vstest.console process (PID {ProcessId}) exited with code {ExitCode} during {context}. Stderr: {stderr.Trim()}";
        return new VsTestProcessExitedException(message, ExitCode ?? -1);
    }

    public void Dispose()
    {
        try
        {
            if (!_process.HasExited)
            {
                _process.Kill();
            }
        }
        catch (InvalidOperationException)
        {
            // Process already exited.
        }

        _process.Dispose();
    }
}

/// <summary>
/// Exception thrown when vstest.console exits unexpectedly.
/// </summary>
public class VsTestProcessExitedException : Exception
{
    public VsTestProcessExitedException(string message, int exitCode)
        : base(message)
    {
        ExitCode = exitCode;
    }

    /// <summary>
    /// The exit code of the vstest.console process.
    /// </summary>
    public int ExitCode { get; }
}
