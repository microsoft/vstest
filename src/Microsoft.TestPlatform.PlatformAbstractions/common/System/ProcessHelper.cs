// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETFRAMEWORK || NETCOREAPP || NETSTANDARD2_0

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
#if !NET5_0_OR_GREATER
using System.Threading.Tasks;
#endif

using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;

/// <summary>
/// Helper class to deal with process related functionality.
/// </summary>
public partial class ProcessHelper : IProcessHelper
{
    private static readonly string Arm = "arm";
    private readonly Process _currentProcess = Process.GetCurrentProcess();

    /// <inheritdoc/>
    public object LaunchProcess(string processPath, string? arguments, string? workingDirectory, IDictionary<string, string?>? envVariables, Action<object?, string?>? errorCallback, Action<object?>? exitCallBack, Action<object?, string?>? outputCallBack)
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
            process = null;

            //EqtTrace.Error("TestHost Object {0} failed to launch with the following exception: {1}", processPath, exception.Message);
            throw;
        }

        return process;

        // Local functions
        void InitializeAndStart()
        {
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WorkingDirectory = workingDirectory;

            process.StartInfo.FileName = processPath;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.RedirectStandardError = true;

            process.EnableRaisingEvents = true;

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

            if (errorCallback != null)
            {
                process.ErrorDataReceived += (sender, args) => errorCallback(sender as Process, args.Data);
            }

            if (exitCallBack != null)
            {
                process.Exited += async (sender, args) =>
                {
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
                            // On .NET 5 and later, the solution is simple, we can simply use WaitForExitAsync which
                            // correctly ensure that some time is given to the child process (or any grandchild) to
                            // flush before exit happens.
                            //
                            // For older frameworks, the solution is more tricky but it seems we can get the expected
                            // behavior using the parameterless 'WaitForExit()' combined with an awaited Task.Run call.
                            var cts = new CancellationTokenSource(500);
#if NET5_0_OR_GREATER
                            await p.WaitForExitAsync(cts.Token);
#else
                            await Task.Run(() => p.WaitForExit(), cts.Token);
#endif
                        }
                        catch
                        {
                            // Ignore all exceptions thrown when asking for process to exit.
                            // We "expect" TaskCanceledException, COMException (if process was disposed before calling
                            // the exit) or InvalidOperationException.
                        }
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

    /// <inheritdoc/>
    public string? GetCurrentProcessFileName()
    {
        return _currentProcess.MainModule?.FileName;
    }

    /// <inheritdoc/>
    public string? GetTestEngineDirectory()
    {
        return Path.GetDirectoryName(typeof(ProcessHelper).GetTypeInfo().Assembly.Location);
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
                proc.Kill();
            }
        }
        catch (InvalidOperationException)
        {
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
#if !NETCOREAPP1_0
        CultureInfo.InvariantCulture
#endif
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

#endif
