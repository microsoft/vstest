// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;

/// <summary>
/// Helper class to deal with process related functionality.
/// </summary>
public partial class ProcessHelper : IProcessHelper
{
    private static readonly string Arm = "arm";
    private readonly Process _currentProcess = Process.GetCurrentProcess();

    // Bounded time (ms) we wait for a crashed process's redirected stderr to reach EOF before reading it,
    // so a late-delivered crash callstack (e.g. "Stack overflow.") is not dropped. See the Exited handler.
    private const int CrashErrorDrainTimeout = 5000;

    // Bounded time (ms) we wait for stderr to drain after an exit we are not interested in diagnosing, kept
    // short so neither the common case nor the rare grandchild-keeps-the-pipe-open case adds latency.
    private const int NonCrashErrorDrainTimeout = 500;

    // Bounded time (ms) we still give stderr once we decide to tear the process down. Not zero, because the
    // output is often already sitting in the pipe and costs nothing to pick up, but short enough that an abort
    // stays responsive even when a grandchild process (e.g. a browser driver) keeps the pipe open forever.
    private const int TearDownErrorDrainTimeout = 100;

    // Per-process signal that we are deliberately tearing the process down (aborting or cleaning up a run),
    // rather than observing it die on its own. Cancelling it cuts the stderr drain short - including a drain
    // that is already in flight, which is what keeps aborting a run from an IDE responsive. ConditionalWeakTable
    // holds only weak references to the processes, so entries disappear when a process is collected and nothing
    // has to be removed explicitly.
    private readonly ConditionalWeakTable<Process, CancellationTokenSource> _tearDownSignals = new();

#if !NET
    private readonly IEnvironment _environment;
#endif

    /// <summary>
    /// Default constructor.
    /// </summary>
    public ProcessHelper() : this(new PlatformEnvironment())
    {
    }

    internal ProcessHelper(IEnvironment environment)
    {
#if !NET
        _environment = environment;
#endif
    }

    /// <inheritdoc/>
    public object LaunchProcess(string processPath, string? arguments, string? workingDirectory, IDictionary<string, string?>? envVariables, Action<object?, string?>? errorCallback, Action<object?>? exitCallBack, Action<object?, string?>? outputCallBack)
        => LaunchProcess(processPath, arguments, workingDirectory, envVariables, errorCallback, exitCallBack, outputCallBack, createNoNewWindow: true);

    /// <inheritdoc/>
    public object LaunchProcess(string processPath, string? arguments, string? workingDirectory, IDictionary<string, string?>? envVariables, Action<object?, string?>? errorCallback, Action<object?>? exitCallBack, Action<object?, string?>? outputCallBack, bool createNoNewWindow)
    {
        if (!File.Exists(processPath))
        {
            throw new FileNotFoundException("Path does not exist: " + processPath, processPath);
        }

        var process = new Process();
        try
        {
            InitializeAndStart();
        }
        catch (Exception)
        {
            process.Dispose();

            //EqtTrace.Error("TestHost Object {0} failed to launch with the following exception: {1}", processPath, exception.Message);
            throw;
        }

        return process;

        // Local functions
        void InitializeAndStart()
        {
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = createNoNewWindow;
            process.StartInfo.WorkingDirectory = workingDirectory;

            process.StartInfo.FileName = processPath;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.RedirectStandardError = true;

            process.EnableRaisingEvents = true;

            // Set additional environment variables.
            if (envVariables != null)
            {
                foreach (var kvp in envVariables)
                {
                    process.StartInfo.AddEnvironmentVariable(kvp.Key, kvp.Value);
                }
            }

            if (outputCallBack != null)
            {
                process.StartInfo.RedirectStandardOutput = true;
                process.OutputDataReceived += (sender, args) => outputCallBack(sender as Process, args.Data);
            }

            // Completed once the redirected stderr stream reaches EOF (signaled by a null Data event,
            // which is raised after all stderr lines have been handed to errorCallback). This is
            // the only reliable signal that the asynchronously-collected error output is complete:
            // neither WaitForExit(timeout) nor WaitForExitAsync(token) is guaranteed to observe EOF, because
            // the latter stops waiting for it as soon as its token is cancelled. The exit handler below awaits
            // (bounded) on this before reading.
            TaskCompletionSource<bool>? errorStreamClosed = null;
            if (errorCallback != null)
            {
                // RunContinuationsAsynchronously so completing this from the ErrorDataReceived callback does not
                // inline the exit handler's continuation onto the stderr-reader thread.
                errorStreamClosed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                process.ErrorDataReceived += (sender, args) =>
                {
                    errorCallback(sender as Process, args.Data);

                    // Signal EOF only after the last callback has been delivered, so anyone who observes this
                    // is guaranteed to see the complete error output.
                    if (args.Data is null)
                    {
                        errorStreamClosed.TrySetResult(true);
                    }
                };
            }

            if (exitCallBack != null)
            {
                process.Exited += async (sender, args) =>
                {
                    // Bounded time we give the process to fully exit after we are notified of its exit.
                    const int processExitTimeout = 500;

                    if (sender is Process p)
                    {
                        try
                        {
                            // NOTE: When receiving an exit event, we want to give some time to the child process
                            // to close properly (i.e. flush output, error stream...). Despite this simple need,
                            // the actual implementation needs to be complex, especially for Unix systems.
                            // See ticket https://github.com/microsoft/vstest/issues/3375 to get the links to all
                            // issues, discussions and documentations.
                            //
                            // On .NET 5 and later we use WaitForExitAsync to give the child process (and any
                            // grandchild) some time to exit. NOTE: WaitForExitAsync does wait for the redirected
                            // Output/Error streams to reach EOF, but only for as long as its token allows - once
                            // the token is cancelled it stops waiting for them. The bounded stderr drain after
                            // this block is what gives a crashed process a longer, separate budget to deliver its
                            // callstack.
                            //
                            // For older frameworks, the solution is more tricky but it seems we can get the expected
                            // behavior using the parameterless 'WaitForExit()' combined with an awaited Task.Run call.
                            // 'using' so the timer the timeout allocates is released as soon as we are done waiting,
                            // instead of leaking one per process exit when many test hosts are spawned.
                            using var cts = new CancellationTokenSource(processExitTimeout);
#if NET
                            await p.WaitForExitAsync(cts.Token);
#else
                            // NOTE: In case we run on Windows we must call 'WaitForExit(timeout)' instead of calling
                            // the parameterless overload. The reason for this requirement stems from the behavior of
                            // the Selenium WebDriver when debugging a test. If the debugger is detached, the default
                            // action is to kill the testhost process that it was attached to, but for some reason we
                            // end up with a zombie process that would make us wait indefinitely with a simple
                            // 'WaitForExit()' call. This in turn causes the vstest.console to block waiting for the
                            // test request to finish and this behavior will be visible to the user since TW will
                            // show the Selenium test as still running. Only killing the Edge Driver process would help
                            // unblock vstest.console, but this is not a reasonable ask to our users.
                            //
                            // TODO: This fix is not ideal, it's only a workaround to make Selenium tests usable again.
                            // Ideally, we should spend some more time here in order to better understand what causes
                            // the testhost to become a zombie process in the first place.
                            if (_environment.OperatingSystem is PlatformOperatingSystem.Windows)
                            {
                                p.WaitForExit(processExitTimeout);
                            }
                            else
                            {
                                cts.Token.Register(() =>
                                {
                                    try
                                    {
                                        if (!p.HasExited)
                                        {
                                            // We are force-killing a process that overran the exit budget (e.g. a
                                            // grandchild keeps it hanging). Signal the teardown - exactly like
                                            // TerminateProcess does - BEFORE killing, so the stderr drain below
                                            // uses the short teardown budget instead of treating our own kill as
                                            // a crash and waiting the generous budget unnecessarily.
                                            SignalTearDown(p);
                                            p.Kill();
                                        }
                                    }
                                    catch
                                    {
                                        // Ignore all exceptions thrown when trying to kill a process that may be
                                        // left hanging. This is a best effort to kill it, but should we fail for
                                        // any reason we'd probably block on 'WaitForExit()' anyway.
                                    }
                                });
                                await Task.Run(() => p.WaitForExit(), cts.Token).ConfigureAwait(false);
                            }
#endif
                        }
                        catch
                        {
                            // Ignore all exceptions thrown when asking for process to exit.
                            // We "expect" TaskCanceledException, COMException (if process was disposed before calling
                            // the exit) or InvalidOperationException.
                        }

                        // The process has exited. Asynchronously wait (bounded) for the redirected stderr to reach
                        // EOF so that asynchronously-collected error output (e.g. a testhost crash callstack such as
                        // "Stack overflow.") is complete before the exit callback consumes it.
                        //
                        // We await rather than block here on purpose: the crash callstack can be delivered to
                        // ErrorDataReceived noticeably late under load (e.g. thread-pool starvation while many test
                        // hosts run in parallel on CI), and blocking a thread-pool thread for the whole drain budget
                        // would compete with the very ErrorDataReceived callback we are waiting for and could starve
                        // it out. Dropping that output both produces a misleading error message and makes
                        // RunTestsShouldThrowOnStackOverflowException flaky.
                        //
                        // This drain budget is intentionally separate from (and far more generous than) the
                        // process-exit budget above, and the generous part is only spent on a crash - an abnormal
                        // exit of a process we were not already tearing down. A clean exit gets a short grace
                        // period, and a process we are tearing down (aborting or cleaning up a run) gets less
                        // still, because there the priority is to get out of the way rather than to diagnose.
                        // The teardown signal is a cancellation, so asking to tear down also cuts short a drain
                        // that is already in flight. In every case the wait returns as soon as EOF is observed, so
                        // a process that exits and drains promptly pays almost nothing.
                        var tearDown = GetTearDownToken(p);
                        var errorDrainTimeout = GetErrorDrainTimeout(exitedCleanly: ExitedCleanly(p), tearingDown: tearDown.IsCancellationRequested);
                        await WaitForErrorStreamToDrainAsync(errorStreamClosed, errorDrainTimeout, tearDown, TearDownErrorDrainTimeout).ConfigureAwait(false);
                    }

                    // If exit callback has code that access Process object, ensure that the exceptions handling should be done properly.
                    exitCallBack(sender);
                };
            }

            // EqtTrace.Verbose("ProcessHelper: Starting process '{0}' with command line '{1}'", processPath, arguments);
            // TODO: Enable logging here, and consider wrapping Win32Exception into another that shows the path of the process.
            process.Start();

            if (errorCallback != null)
            {
                process.BeginErrorReadLine();
            }

            if (outputCallBack != null)
            {
                process.BeginOutputReadLine();
            }
        }
    }

    /// <summary>
    /// Asynchronously waits, bounded by <paramref name="timeoutMilliseconds"/>, for the redirected standard
    /// error stream to reach EOF (signaled by completing <paramref name="errorStreamClosed"/>). This ensures all
    /// <see cref="Process.ErrorDataReceived"/> callbacks have completed - and therefore the captured error
    /// output is complete - before it is consumed by the exit callback. It returns immediately when there is
    /// no redirected error stream, when the timeout is not positive, or when the stream has already drained
    /// (the common case), and is otherwise bounded by the timeout (e.g. a grandchild process keeps the pipe
    /// open), so the caller can never hang. It deliberately does not block the calling thread while waiting,
    /// so it does not consume a thread-pool thread that the pending <see cref="Process.ErrorDataReceived"/>
    /// callback may itself need in order to deliver EOF under thread-pool starvation.
    /// <para>
    /// When <paramref name="tearDown"/> is signaled we are no longer diagnosing the process but getting out of
    /// its way (aborting or cleaning up a run), so the remaining wait collapses to
    /// <paramref name="tearDownTimeoutMilliseconds"/>. That applies to a wait that is already in flight too,
    /// which is what keeps an abort responsive when a long crash budget is already being spent.
    /// </para>
    /// </summary>
    internal static async Task WaitForErrorStreamToDrainAsync(
        TaskCompletionSource<bool>? errorStreamClosed,
        int timeoutMilliseconds,
        CancellationToken tearDown = default,
        int tearDownTimeoutMilliseconds = 0)
    {
        if (errorStreamClosed is null || timeoutMilliseconds <= 0 || errorStreamClosed.Task.IsCompleted)
        {
            return;
        }

        using var timeoutCancellation = new CancellationTokenSource();

        // Registered rather than awaited alongside the others, so a teardown that arrives mid-wait shortens the
        // budget instead of being noticed only after the original one has been spent. Disposed before
        // timeoutCancellation (reverse declaration order), so the callback cannot run against a disposed source.
        using var tearDownRegistration = tearDown.CanBeCanceled
            ? tearDown.Register(() =>
            {
                try
                {
                    timeoutCancellation.CancelAfter(tearDownTimeoutMilliseconds);
                }
                catch (ObjectDisposedException)
                {
                    // The wait already finished and disposed its timeout; there is nothing left to shorten.
                }
            })
            : default;

        // Cancelling the delay leaves it in the canceled - not faulted - state, so it never needs to be observed.
        var delayTask = Task.Delay(timeoutMilliseconds, timeoutCancellation.Token);
        var completedTask = await Task.WhenAny(errorStreamClosed.Task, delayTask).ConfigureAwait(false);

        // Stop the timer as soon as the stream drains so we don't leave it pending for the whole timeout.
        if (completedTask != delayTask)
        {
            timeoutCancellation.Cancel();
        }
    }

    /// <summary>
    /// Returns whether a process exited with code 0. A process whose exit code cannot be retrieved (e.g. it was
    /// disposed while the exit was being handled) is reported as not clean, so a possible crash keeps the
    /// generous stderr budget rather than being cut short. Deciding that we are tearing the process down is a
    /// separate, explicit signal, so this does not have to guess at intent.
    /// </summary>
    private static bool ExitedCleanly(Process process)
    {
        try
        {
            return process.HasExited && process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Picks the bounded time we are willing to wait for the redirected stderr to reach EOF.
    /// <list type="bullet">
    /// <item>A process we are tearing down (aborting or cleaning up a run) gets the shortest budget: we are no
    /// longer diagnosing it, we are getting out of its way, and a grandchild process (e.g. a browser driver)
    /// that keeps the pipe open must not be able to stall the abort.</item>
    /// <item>A clean exit gets a short grace period, because there is normally nothing left to collect.</item>
    /// <item>A crash - an abnormal exit of a process we were not tearing down - gets the generous budget, so a
    /// late-delivered crash callstack such as "Stack overflow." is captured rather than truncated.</item>
    /// </list>
    /// </summary>
    internal static int GetErrorDrainTimeout(bool exitedCleanly, bool tearingDown)
        => tearingDown ? TearDownErrorDrainTimeout
        : exitedCleanly ? NonCrashErrorDrainTimeout
        : CrashErrorDrainTimeout;

    /// <summary>
    /// Signals that we are deliberately tearing <paramref name="process"/> down, so its stderr drain is cut
    /// short. Safe to call more than once, and safe to call for a process this helper did not launch.
    /// </summary>
    private void SignalTearDown(Process process)
    {
        try
        {
            GetTearDownSource(process).Cancel();
        }
        catch
        {
            // Cancel surfaces whatever the registered callbacks threw. Failing to shorten a drain is not worth
            // failing the teardown the caller actually asked for. (EqtTrace is not available in this assembly.)
        }
    }

    /// <summary>
    /// Returns the teardown token for <paramref name="process"/>, already signaled when we asked to tear the
    /// process down before it exited.
    /// </summary>
    private CancellationToken GetTearDownToken(Process process)
        => GetTearDownSource(process).Token;

    private CancellationTokenSource GetTearDownSource(Process process)
        => _tearDownSignals.GetValue(process, static _ => new CancellationTokenSource());

    /// <inheritdoc/>
    public string? GetCurrentProcessFileName()
    {
        return _currentProcess.MainModule?.FileName;
    }

    /// <inheritdoc/>
    public string? GetTestEngineDirectory()
    {
        return Path.GetDirectoryName(typeof(ProcessHelper).Assembly.Location);
    }

    /// <inheritdoc/>
    public int GetCurrentProcessId()
    {
        return _currentProcess.Id;
    }

    /// <inheritdoc/>
    public string GetProcessName(int processId)
    {
        if (processId == _currentProcess.Id)
        {
            return _currentProcess.ProcessName;
        }

        return Process.GetProcessById(processId).ProcessName;
    }

    /// <inheritdoc/>
    public bool TryGetExitCode(object? process, out int exitCode)
    {
        try
        {
            if (process is Process proc && proc.HasExited)
            {
                exitCode = proc.ExitCode;
                return true;
            }
        }
        catch (InvalidOperationException)
        {
            // Process may have already exited — exit code unavailable.
        }

        exitCode = 0;
        return false;
    }

    /// <inheritdoc/>
    public void SetExitCallback(int processId, Action<object?>? callbackAction)
    {
        try
        {
            var process = processId == _currentProcess.Id ? _currentProcess : Process.GetProcessById(processId);
            process.EnableRaisingEvents = true;
            process.Exited += (sender, args) => callbackAction?.Invoke(sender);
        }
        catch (ArgumentException)
        {
            // Process.GetProcessById() throws ArgumentException if process is not running(identifier might be expired).
            // Invoke callback immediately.
            callbackAction?.Invoke(null);
        }
    }

    /// <inheritdoc/>
    public void TerminateProcess(object? process)
    {
        if (process is not Process proc)
        {
            return;
        }

        // We are tearing this process down on purpose (abort/cleanup), so we are no longer interested in
        // diagnosing it. Signal that BEFORE the kill, so the exit handler - which can run at any moment from
        // here on - cannot miss it, and signal it even when the process has already exited, so a stderr drain
        // that is already in flight for an earlier crash is cut short instead of holding up the abort.
        SignalTearDown(proc);

        try
        {
            if (!proc.HasExited)
            {
                proc.Kill();
            }
        }
        catch (InvalidOperationException)
        {
            // Process may have already exited — exit code unavailable.
        }
    }

    /// <inheritdoc/>
    public int GetProcessId(object? process)
    {
        var proc = process as Process;
        return proc?.Id ?? -1;
    }

    /// <inheritdoc/>
    public string GetNativeDllDirectory()
    {
        var osArchitecture = new PlatformEnvironment().Architecture;
        return osArchitecture is PlatformArchitecture.ARM or PlatformArchitecture.ARM64
            ? Path.Combine(GetCurrentProcessLocation(), GetFormattedCurrentProcessArchitecture(), Arm)
            : Path.Combine(GetCurrentProcessLocation(), GetFormattedCurrentProcessArchitecture());
    }

    private string GetFormattedCurrentProcessArchitecture()
        => GetCurrentProcessArchitecture().ToString()
            .ToLower(
        CultureInfo.InvariantCulture
            );

    /// <inheritdoc/>
    public void WaitForProcessExit(object? process)
    {
        if (process is Process proc && !proc.HasExited)
        {
            proc.WaitForExit();
        }
    }
}
