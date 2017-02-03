// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System;
    using System.Diagnostics;
    using System.Collections.Generic;

    /// <summary>
    /// The start info of the test runner
    /// </summary>
    public class TestProcessStartInfo
    {
        /// <summary>
        /// The name of the test runner exe
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// The arguments for the test runner
        /// </summary>
        public string Arguments { get; set; }

        /// <summary>
        /// The working directory for the test runner
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// The associated environment variables
        /// </summary>
        public IDictionary<string, string> EnvironmentVariables { get; set; }

        /// <summary>
        /// Any additional custom properties that might be required for the launch
        /// For example - emulator ID, remote machine details etc.
        /// </summary>
        public IDictionary<string, string> CustomProperties { get; set; }

        /// <summary>
        /// Callback on process exit
        /// </summary>
        public Action<Process> ExitCallback { get; set; }
    }
}
