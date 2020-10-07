// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETFRAMEWORK || NETCOREAPP

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

    /// <summary>
    /// Helper class to deal with process related functionality.
    /// </summary>
    public partial class ProcessHelper : IProcessHelper
    {
        private static readonly string ARM = "arm";

        /// <inheritdoc/>
        public object LaunchProcess(string processPath, string arguments, string workingDirectory, IDictionary<string, string> envVariables, Action<object, string> errorCallback, Action<object> exitCallBack, Action<object, string> outputCallBack)
        {
            var process = new Process();
            try
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
                    process.Exited += (sender, args) =>
                    {
                        // Call WaitForExit without again to ensure all streams are flushed,
                        var p = sender as Process;
                        try
                        {
                            // Add timeout to avoid indefinite waiting on child process exit.
                            p.WaitForExit(500);
                        }
                        catch (InvalidOperationException)
                        {
                        }

                        // If exit callback has code that access Process object, ensure that the exceptions handling should be done properly.
                        exitCallBack(p);
                    };
                }

                // EqtTrace.Verbose("ProcessHelper: Starting process '{0}' with command line '{1}'", processPath, arguments);
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
            catch (Exception)
            {
                process.Dispose();
                process = null;

                // EqtTrace.Error("TestHost Object {0} failed to launch with the following exception: {1}", processPath, exception.Message);
                throw;
            }

            return process as object;
        }

        /// <inheritdoc/>
        public string GetCurrentProcessFileName()
        {
            return Process.GetCurrentProcess().MainModule.FileName;
        }

        /// <inheritdoc/>
        public string GetTestEngineDirectory()
        {
            return Path.GetDirectoryName(typeof(ProcessHelper).GetTypeInfo().Assembly.Location);
        }

        /// <inheritdoc/>
        public int GetCurrentProcessId()
        {
            return Process.GetCurrentProcess().Id;
        }

        /// <inheritdoc/>
        public string GetProcessName(int processId)
        {
            return Process.GetProcessById(processId).ProcessName;
        }

        /// <inheritdoc/>
        public bool TryGetExitCode(object process, out int exitCode)
        {
            var proc = process as Process;
            try
            {
                if (proc != null && proc.HasExited)
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
        public void SetExitCallback(int processId, Action<object> callbackAction)
        {
            try
            {
                var process = Process.GetProcessById(processId);
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
        public void TerminateProcess(object process)
        {
            var proc = process as Process;
            try
            {
                if (proc != null && !proc.HasExited)
                {
                    proc.Kill();
                }
            }
            catch (InvalidOperationException)
            {
            }
        }

        /// <inheritdoc/>
        public int GetProcessId(object process)
        {
            var proc = process as Process;
            return proc?.Id ?? -1;
        }

        /// <inheritdoc/>
        public PlatformArchitecture GetCurrentProcessArchitecture()
        {
            if (IntPtr.Size == 8)
            {
                return PlatformArchitecture.X64;
            }

            return PlatformArchitecture.X86;
        }

        /// <inheritdoc/>
        public string GetNativeDllDirectory()
        {
            var osArchitecture = new PlatformEnvironment().Architecture;
            if (osArchitecture == PlatformArchitecture.ARM || osArchitecture == PlatformArchitecture.ARM64)
            {
                return Path.Combine(this.GetCurrentProcessLocation(), this.GetCurrentProcessArchitecture().ToString().ToLower(), ARM);
            }

            return Path.Combine(this.GetCurrentProcessLocation(), this.GetCurrentProcessArchitecture().ToString().ToLower());
        }

        /// <inheritdoc/>
        public void WaitForProcessExit(object process)
        {
            var proc = process as Process;
            if (proc != null && !proc.HasExited)
            {
                proc.WaitForExit();
            }
        }
    }
}

#endif
