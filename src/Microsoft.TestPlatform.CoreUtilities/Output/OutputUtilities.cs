// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Utilities
{
    using System;
    using System.Globalization;

    using Microsoft.VisualStudio.TestPlatform.CoreUtilities;

    /// <summary>
    /// Utility Methods for sending output to IOutput.
    /// </summary>
    public static class OutputUtilities
    {
        #region Extension Methods

        /// <summary>
        /// Output an error message.
        /// </summary>
        /// <param name="output">Output instance the method is being invoked with.</param>
        /// <param name="format">Format string for the error message.</param>
        /// <param name="args">Arguments to format into the format string.</param>
        public static void Error(this IOutput output, String format, params object[] args)
        {
            using (new ConsoleColorHelper(ConsoleColor.Red))
            {
                Output(output, OutputLevel.Error, Resources.CommandLineError, format, args);
            }
        }

        /// <summary>
        /// Output a warning message.
        /// </summary>
        /// <param name="output">Output instance the method is being invoked with.</param>
        /// <param name="format">Format string for the warning message.</param>
        /// <param name="args">Arguments to format into the format string.</param>
        public static void Warning(this IOutput output, string format, params object[] args)
        {
            using (new ConsoleColorHelper(ConsoleColor.Yellow))
            {
                Output(output, OutputLevel.Warning, Resources.CommandLineWarning, format, args);
            }
        }

        /// <summary>
        /// Output a informational message.
        /// </summary>
        /// <param name="output">Output instance the method is being invoked with.</param>
        /// <param name="format">Format string for the informational message.</param>
        /// <param name="args">Arguments to format into the format string.</param>
        public static void Information(this IOutput output, string format, params object[] args)
        {
            Output(output, OutputLevel.Information, Resources.CommandLineInformational, format, args);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Formats the message.
        /// </summary>
        /// <param name="messageTypeFormat">Format string for the message type.</param>
        /// <param name="format">Format string for the error message.</param>
        /// <param name="args">Arguments to format into the format string.</param>
        private static void Output(IOutput output, OutputLevel level, string messageTypeFormat, string format, params object[] args)
        {
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }

            string message = Format(messageTypeFormat, format, args);
            output.WriteLine(message, level);
        }

        /// <summary>
        /// Formats the message.
        /// </summary>
        /// <param name="messageTypeFormat">Format string for the message type.</param>
        /// <param name="format">Format string for the error message.</param>
        /// <param name="args">Arguments to format into the format string.</param>
        private static string Format(string messageTypeFormat, string format, params object[] args)
        {
            if (format==null || String.IsNullOrEmpty(format.Trim()))
            {
                throw new ArgumentException(Resources.CannotBeNullOrEmpty, "format");
            }

            string message = null;
            if (args != null && args.Length > 0)
            {
                message =
                    String.Format(
                        CultureInfo.CurrentCulture,
                        format,
                        args);
            }
            else
            {
                message = format;
            }

            message =
                String.Format(
                    CultureInfo.CurrentCulture,
                    messageTypeFormat,
                    message);

            return message;
        }

        #endregion
    }
}
