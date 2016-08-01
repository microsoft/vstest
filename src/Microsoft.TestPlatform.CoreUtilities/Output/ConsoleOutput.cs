// ---------------------------------------------------------------------------
// <copyright file="ConsoleOutput.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//     Sends output to the console.
// </summary>
// ---------------------------------------------------------------------------

namespace Microsoft.VisualStudio.TestPlatform.Utilities
{
    using System;
    using System.IO;
    using System.Text;    

    /// <summary>
    /// Sends output to the console.
    /// </summary>    
    public class ConsoleOutput : IOutput
    {
        #region Fields

        private TextWriter m_standardOutput = null;
        private TextWriter m_standardError = null;
        private static Object s_lockObject = new object();
        private static ConsoleOutput s_consoleOutput = null;

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        internal ConsoleOutput()
        {
            m_standardOutput = Console.Out;
            m_standardError = Console.Error;
        }

        #endregion

        public static ConsoleOutput Instance
        {
            get
            {
                if (s_consoleOutput != null)
                {
                    return s_consoleOutput;
                }
                
                lock (s_lockObject)
                {
                    if (s_consoleOutput == null)
                    {
                        s_consoleOutput = new ConsoleOutput();
                    }
                }
                return s_consoleOutput;
            }
        }

        #region IOutput

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
                    m_standardOutput.Write(message);
                    break;

                case OutputLevel.Error:
                    m_standardError.Write(message);
                    break;

                default:
                    m_standardOutput.Write("ConsoleOutput.WriteLine: The output level is unrecognized: {0}", level);
                    break;
            }
        }

        #endregion

    }
}
