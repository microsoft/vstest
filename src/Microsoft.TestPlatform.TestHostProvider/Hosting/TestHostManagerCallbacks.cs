// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.TestHostProvider.Hosting
{
    using System;
    using System.Diagnostics;
    using System.Text;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

    internal class TestHostManagerCallbacks
    {
        public static void ErrorReceivedCallback(StringBuilder testHostProcessStdError, string data)
        {
            if (!string.IsNullOrEmpty(data))
            {
                // Log all standard error message because on too much data we ignore starting part.
                // This is helpful in abnormal failure of testhost.
                EqtTrace.Warning("TestHostManagerCallbacks.ErrorReceivedCallback Test host standard error line: {0}", data);

                // Don't append more data if already reached max length.
                if (testHostProcessStdError.Length >= testHostProcessStdError.MaxCapacity)
                {
                    return;
                }

                // Add newline for readbility.
                data += Environment.NewLine;

                // Append sub string of data if appending all the data exceeds max capacity.
                if (testHostProcessStdError.Length + data.Length >= testHostProcessStdError.MaxCapacity)
                {
                    data = data.Substring(0, testHostProcessStdError.MaxCapacity - testHostProcessStdError.Length);
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

            int procId = -1;
            try
            {
                procId = (process as Process).Id;
            }
            catch (InvalidOperationException)
            {
            }

            if (exitCode != 0)
            {
                EqtTrace.Error("TestHostManagerCallbacks.ExitCallBack: Testhost processId: {0} exited with exitcode: {1} error: '{2}'", procId, exitCode, testHostProcessStdErrorStr);
            }
            else
            {
                EqtTrace.Info("TestHostManagerCallbacks.ExitCallBack: Testhost processId: {0} exited with exitcode: 0 error: '{1}'", procId, testHostProcessStdErrorStr);
            }

            onHostExited(new HostProviderEventArgs(testHostProcessStdErrorStr, exitCode, procId));
        }
    }
}
