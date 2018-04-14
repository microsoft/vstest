// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests.TranslationLayerTests
{
    using System.Diagnostics;
    using System.Threading;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;

    /// <summary>
    /// The custom test host launcher.
    /// </summary>
    public class CustomTestHostLauncher : ITestHostLauncher
    {
        public int ProcessId
        {
            get;
            private set;
        }

        /// <inheritdoc
        public bool IsDebug => true;

        /// <inheritdoc
        public int LaunchTestHost(TestProcessStartInfo defaultTestHostStartInfo, CancellationToken cancellationToken = default(CancellationToken))
        {
            var processInfo = new ProcessStartInfo(
                                      defaultTestHostStartInfo.FileName,
                                      defaultTestHostStartInfo.Arguments)
            {
                WorkingDirectory = defaultTestHostStartInfo.WorkingDirectory
                                      };
            processInfo.UseShellExecute = false;

            var process = new Process { StartInfo = processInfo };
            process.Start();

            if (process != null)
            {
                return process.Id;
            }

            return -1;
        }
    }
}
