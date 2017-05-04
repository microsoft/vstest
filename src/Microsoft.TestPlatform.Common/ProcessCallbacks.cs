// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common
{
    using System;
    using System.Diagnostics;
    using System.Text;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

    internal class ProcessCallbacks
    {
        public static void ErrorReceivedCallback(StringBuilder processStdError, string data, string processName)
        {
            if (!string.IsNullOrEmpty(data))
            {
                // Log all standard error message because on too much data we ignore starting part.
                // This is helpful in abnormal failure of process.
                EqtTrace.Warning("{0} standard error line: {1}", processName, data);

                // Add newline for readbility.
                data += Environment.NewLine;

                // if incoming data stream is huge empty entire testError stream, & limit data stream to MaxCapacity
                if (data.Length > processStdError.MaxCapacity)
                {
                    processStdError.Clear();
                    data = data.Substring(data.Length - processStdError.MaxCapacity);
                }
                else
                {
                    // remove only what is required, from beginning of error stream
                    int required = data.Length + processStdError.Length - processStdError.MaxCapacity;
                    if (required > 0)
                    {
                        processStdError.Remove(0, required);
                    }
                }

                processStdError.Append(data);
            }
        }

        public static void ExitCallBack(
            IProcessHelper processHelper,
            object process,
            StringBuilder processStdError,
            Action<HostProviderEventArgs> onProcessExited, string processName)
        {
            var exitCode = 0;
            var processStdErrorStr = processStdError.ToString();

            processHelper.TryGetExitCode(process, out exitCode);

            if (exitCode != 0)
            {
                EqtTrace.Error("{0} exited with error: '{1}'", processName, processStdErrorStr);
            }

            onProcessExited(new HostProviderEventArgs(processStdErrorStr, exitCode, (process as Process).Id));
        }
    }
}
