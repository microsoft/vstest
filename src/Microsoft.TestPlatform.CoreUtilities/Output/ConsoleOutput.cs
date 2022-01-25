// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NETSTANDARD1_0 && !WINDOWS_UWP

namespace Microsoft.VisualStudio.TestPlatform.Utilities
{
    using System;
    using System.IO;

    /// <summary>
    /// Sends output to the console.
    /// </summary>
    public class ConsoleOutput : IOutput
    {
        private static readonly object lockObject = new();
        private static ConsoleOutput consoleOutput = null;

        private readonly TextWriter standardOutput = null;
        private readonly TextWriter standardError = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleOutput"/> class.
        /// </summary>
        internal ConsoleOutput()
        {
            standardOutput = Console.Out;
            standardError = Console.Error;
        }

        /// <summary>
        /// Gets the instance of <see cref="ConsoleOutput"/>.
        /// </summary>
        public static ConsoleOutput Instance
        {
            get
            {
                if (consoleOutput != null)
                {
                    return consoleOutput;
                }

                lock (lockObject)
                {
                    if (consoleOutput == null)
                    {
                        consoleOutput = new ConsoleOutput();
                    }
                }

                return consoleOutput;
            }
        }

        /// <summary>
        /// Writes the message with a new line.
        /// </summary>
        /// <param name="message">Message to be output.</param>
        /// <param name="level">Level of the message.</param>
        public void WriteLine(string message, OutputLevel level)
        {
            Write(message, level);
            Write(Environment.NewLine, level);
        }

        /// <summary>
        /// Writes the message with no new line.
        /// </summary>
        /// <param name="message">Message to be output.</param>
        /// <param name="level">Level of the message.</param>
        public void Write(string message, OutputLevel level)
        {
            switch (level)
            {
                case OutputLevel.Information:
                case OutputLevel.Warning:
                    standardOutput.Write(message);
                    break;

                case OutputLevel.Error:
                    standardError.Write(message);
                    break;

                default:
                    standardOutput.Write("ConsoleOutput.WriteLine: The output level is unrecognized: {0}", level);
                    break;
            }
        }
    }
}

#endif