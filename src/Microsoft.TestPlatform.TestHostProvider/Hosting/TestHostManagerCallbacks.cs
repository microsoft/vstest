// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.TestHostProvider.Hosting
{
    using System;
    using System.Diagnostics;
    using System.Text;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

    internal class TestHostManagerCallbacks
    {
        public static void ErrorReceivedCallback(StringBuilder testHostProcessStdError, string data)
        {
            if (!string.IsNullOrEmpty(data))
            {
                // Log all standard error message because on too much data we ignore starting part.
                // This is helpful in abnormal failure of testhost.
                EqtTrace.Warning("Test host standard error line: {0}", data);

                // Add newline for readbility.
                data += Environment.NewLine;

                // if incoming data stream is huge empty entire testError stream, & limit data stream to MaxCapacity
                if (data.Length > testHostProcessStdError.MaxCapacity)
                {
                    testHostProcessStdError.Clear();
                    data = data.Substring(data.Length - testHostProcessStdError.MaxCapacity);
                }

                // remove only what is required, from beginning of error stream
                else
                {
                    int required = data.Length + testHostProcessStdError.Length - testHostProcessStdError.MaxCapacity;
                    if (required > 0)
                    {
                        testHostProcessStdError.Remove(0, required);
                    }
                }

                testHostProcessStdError.Append(data);
            }
        }

        public static void ExitCallBack(
            IProcessHelper processHelper,
            object process,
            StringBuilder testHostProcessStdError,
            Action<HostProviderEventArgs> onHostExited)
        {
            var exitCode = 0;
            var testHostProcessStdErrorStr = testHostProcessStdError.ToString();

            processHelper.TryGetExitCode(process, out exitCode);

            if (exitCode != 0)
            {
                EqtTrace.Error("Test host exited with error: '{0}'", testHostProcessStdErrorStr);
            }

            onHostExited(new HostProviderEventArgs(testHostProcessStdErrorStr, exitCode, (process as Process).Id));
        }
    }
}
