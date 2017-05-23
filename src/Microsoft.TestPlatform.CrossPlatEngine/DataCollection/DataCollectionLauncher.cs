// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

    /// <summary>
    /// Abstract DataCollection Launcher provides functionality to handle process launch and exit events.
    /// </summary>
    internal abstract class DataCollectionLauncher : IDataCollectionLauncher
    {
        protected IProcessHelper processHelper;

        protected IMessageLogger messageLogger;

        protected StringBuilder processStdError;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionLauncher"/> class.
        /// </summary>
        /// <param name="processHelper">
        /// The process helper.
        /// </param>
        /// <param name="messageLogger">
        /// The message logger.
        /// </param>
        public DataCollectionLauncher(IProcessHelper processHelper, IMessageLogger messageLogger)
        {
            this.processHelper = processHelper;
            this.messageLogger = messageLogger;
            this.processStdError = new StringBuilder(this.ErrorLength, this.ErrorLength);
        }

        /// <inheritdoc />
        public Process DataCollectorProcess { get; protected set; }

        /// <summary>
        /// Gets or sets the error length for data collector error stream.
        /// </summary>
        protected int ErrorLength { get; set; } = 4096;

        /// <summary>
        /// Gets callback on process exit
        /// </summary>
        protected Action<object> ExitCallBack => (process) =>
        {
            var exitCode = 0;
            var processStdErrorStr = this.processStdError.ToString();

            this.processHelper.TryGetExitCode(process, out exitCode);

            if (exitCode != 0)
            {
                EqtTrace.Error("Data collector exited with error: '{0}'", processStdErrorStr);

                if (!string.IsNullOrWhiteSpace(processStdErrorStr))
                {
                    this.messageLogger.SendMessage(TestMessageLevel.Error, processStdErrorStr);
                }
            }
        };

        /// <summary>
        /// Gets callback to read from process error stream
        /// </summary>
        protected Action<object, string> ErrorReceivedCallback => (process, data) =>
            {
                if (!string.IsNullOrEmpty(data))
                {
                    // Log all standard error message because on too much data we ignore starting part.
                    // This is helpful in abnormal failure of process.
                    EqtTrace.Warning("Data collector standard error line: {0}", data);

                    // Add newline for readbility.
                    data += Environment.NewLine;

                    // if incoming data stream is huge empty entire testError stream, & limit data stream to MaxCapacity
                    if (data.Length > this.processStdError.MaxCapacity)
                    {
                        this.processStdError.Clear();
                        data = data.Substring(data.Length - this.processStdError.MaxCapacity);
                    }
                    else
                    {
                        // remove only what is required, from beginning of error stream
                        int required = data.Length + this.processStdError.Length - this.processStdError.MaxCapacity;
                        if (required > 0)
                        {
                            this.processStdError.Remove(0, required);
                        }
                    }

                    this.processStdError.Append(data);
                }
            };

        /// <summary>
        /// The launch data collector.
        /// </summary>
        /// <param name="environmentVariables">
        /// The environment variables.
        /// </param>
        /// <param name="commandLineArguments">
        /// The command line arguments.
        /// </param>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public abstract int LaunchDataCollector(
            IDictionary<string, string> environmentVariables,
            IList<string> commandLineArguments);
    }
}