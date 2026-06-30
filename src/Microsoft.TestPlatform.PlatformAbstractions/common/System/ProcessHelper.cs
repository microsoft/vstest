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

    // Bounded time (ms) we wait for stderr to drain after a clean exit, kept short so neither the common
    // case nor the rare grandchild-keeps-the-pipe-open case adds latency. See the Exited handler.
    private const int CleanExitErrorDrainTimeout = 500;

    // Processes we deliberately killed (e.g. when aborting or cleaning up a run). Their abnormal exit code is
    // expected and is not a crash, so the exit handler must not spend the long stderr-drain budget on them -
    // that would make aborting a run from an IDE slow whenever a grandchild process (e.g. a browser driver)
    // keeps the stderr pipe open. ConditionalWeakTable holds only weak references to the processes, so entries
    // disappear when a process is collected and nothing has to be removed explicitly.
    private readonly ConditionalWeakTable<Process, object> _deliberatelyTerminatedProcesses = new();
    private readonly object _deliberatelyTerminatedProcessesLock = new();
    private static readonly object DeliberateTerminationMarker = new();

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

    /// <summary>
    /// Gets or sets the set of environment variables to be used when spawning a new process.
    /// Should this set of environment variables be null, the environment variables inherited from
    /// the parent process will be used.
    /// </summary>
    internal static IDictionary<string, string?>? ExternalEnvironmentVariables { get; set; }

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

            // When vstest.console is started in its own process in VisualStudio it is TestWindowStoreHost that starts it.
            // TestWindowStoreHost inherits environment variables from ServiceHost and DevEnv. Those env variables,
            // contain multiple "internal" environment variables, and they also contain DOTNET_ROOT pointing to the 
            // .NET that is shipped with VisualStudio. So to work around this, vstest.console is given a set of environment
            // variables that has only variables that DevEnv was started with. So it gets a "clean" set of env variables.
            //
            // When we run vstest.console in process, we cannot start ourselves with the same clean set of env variables,
            // and the best we can do is to start our child processes (testhost / datacollector) with this environment.
            // To do that we pass that set of "clean" env variables down to the ProcessHelper, and use those instead
            // of all the variables that are set in the current process.
            if (ExternalEnvironmentVariables is not null)
            {
                process.StartInfo.EnvironmentVariables.Clear();
                foreach (var kvp in ExternalEnvironmentVariables)
                {
                    if (kvp.Value is null)
                    {
                        continue;
                    }

                    process.StartInfo.AddEnvironmentVariable(kvp.Key, kvp.Value);
                }
            }

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
            // neither WaitForExit(timeout) nor WaitForExitAsync guarantees the ErrorDataReceived
            // callbacks have run. The exit handler below awaits (bounded) on this before reading.
            TaskCompletionSource<bool>? errorStreamClosed = null;
            if (errorCallback != null)
            {
                // RunContinuationsAsynchronously so completing this from the ErrorDataReceived callback does not
                // inline the exit handler's continuation onto the stderr-reader thread.
                errorStreamClosed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                process.ErrorDataReceived += (sender, args) =>
                {
                    if (args.Data is null)
                    {
                        errorStreamClosed.TrySetResult(true);
                    }

                    errorCallback(sender as Process, args.Data);
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
                            // grandchild) some time to exit. NOTE: WaitForExitAsync only waits for the process
                            // to exit; it does NOT guarantee that the asynchronous Output/ErrorDataReceived
                            // callbacks have finished delivering. The bounded stderr drain after this block
                            // ensures the captured error output is complete before exitCallBack reads it.
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
                        // WaitForExit(timeout)/WaitForExitAsync do not guarantee the ErrorDataReceived callbacks
                        // have run.
                        //
                        // We await rather than block here on purpose: the crash callstack can be delivered to
                        // ErrorDataReceived noticeably late under load (e.g. thread-pool starvation while many test
                        // hosts run in parallel on CI), and blocking a thread-pool thread for the whole drain budget
                        // would compete with the very ErrorDataReceived callback we are waiting for and could starve
                        // it out. Dropping that output both produces a misleading error message and makes
                        // RunTestsShouldThrowOnStackOverflowException flaky.
                        //
                        // This drain budget is intentionally separate from (and far more generous than) the
                        // process-exit budget above, and the generous part is only spent when the process crashed -
                        // i.e. it exited abnormally on its own. A clean exit, or a process we deliberately killed
                        // (e.g. aborting a run from an IDE), gets only a short grace period so we never add latency
                        // to those cases - in particular we must not hang for seconds on abort when a grandchild
                        // process keeps the stderr pipe open and EOF never arrives. In every case the wait returns
                        // as soon as EOF is observed, so a process that exits and drains promptly pays almost nothing.
                        var errorDrainTimeout = GetErrorDrainTimeout(DidProcessExitCleanly(p), WasDeliberatelyTerminated(p));
                        await WaitForErrorStreamToDrainAsync(errorStreamClosed, errorDrainTimeout).ConfigureAwait(false);
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
    /// no redirected error stream or when the timeout is not positive, and is otherwise bounded by the timeout
    /// (e.g. a grandchild process keeps the pipe open), so the caller can never hang. It deliberately does not
    /// block the calling thread while waiting, so it does not consume a thread-pool thread that the pending
    /// <see cref="Process.ErrorDataReceived"/> callback may itself need in order to deliver EOF under
    /// thread-pool starvation.
    /// </summary>
    internal static async Task WaitForErrorStreamToDrainAsync(TaskCompletionSource<bool>? errorStreamClosed, int timeoutMilliseconds)
    {
        if (errorStreamClosed is null || timeoutMilliseconds <= 0)
        {
            return;
        }

        using var timeoutCancellation = new CancellationTokenSource();
        var delayTask = Task.Delay(timeoutMilliseconds, timeoutCancellation.Token);
        var completedTask = await Task.WhenAny(errorStreamClosed.Task, delayTask).ConfigureAwait(false);

        // Stop the timer as soon as the stream drains so we don't leave it pending for the whole timeout.
        if (completedTask != delayTask)
        {
            timeoutCancellation.Cancel();
        }
    }

    /// <summary>
    /// Returns <see langword="true"/> when the process has exited with a zero exit code. A non-zero exit code
    /// (a crash) - or an exit code that cannot be retrieved - is treated as not-clean so the caller waits the
    /// longer stderr drain budget and does not truncate potentially important crash output.
    /// </summary>
    private static bool DidProcessExitCleanly(Process process)
    {
        try
        {
            return process.HasExited && process.ExitCode == 0;
        }
        catch
        {
            // If the exit code is not retrievable (e.g. the process handle is gone), assume a crash so we
            // give the redirected stderr the longer budget to drain.
            return false;
        }
    }

    /// <summary>
    /// Picks the bounded time we are willing to wait for the redirected stderr to reach EOF. A genuine crash
    /// (an abnormal exit we did not cause) gets the generous budget so a late-delivered crash callstack is
    /// captured; a clean exit, or a process we deliberately killed (an abort/cleanup), gets only the short
    /// budget so we never add latency to those cases.
    /// </summary>
    internal static int GetErrorDrainTimeout(bool exitedCleanly, bool deliberatelyTerminated)
        => exitedCleanly || deliberatelyTerminated ? CleanExitErrorDrainTimeout : CrashErrorDrainTimeout;

    private void MarkDeliberatelyTerminated(Process process)
    {
        lock (_deliberatelyTerminatedProcessesLock)
        {
            if (!_deliberatelyTerminatedProcesses.TryGetValue(process, out _))
            {
                _deliberatelyTerminatedProcesses.Add(process, DeliberateTerminationMarker);
            }
        }
    }

    private bool WasDeliberatelyTerminated(Process process)
    {
        lock (_deliberatelyTerminatedProcessesLock)
        {
            return _deliberatelyTerminatedProcesses.TryGetValue(process, out _);
        }
    }

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
        try
        {
            if (process is Process proc && !proc.HasExited)
            {
                // Killing a still-running process on purpose (abort/cleanup): record it so the exit handler
                // treats the resulting abnormal exit as an abort (short stderr drain), not a crash (long
                // stderr drain). A process that exits on its own is never recorded here, so a genuine crash
                // still gets the generous budget.
                MarkDeliberatelyTerminated(proc);
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
