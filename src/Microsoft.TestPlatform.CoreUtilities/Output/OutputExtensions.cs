// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Utilities
{
    using System;
    using System.Globalization;

    using Microsoft.VisualStudio.TestPlatform.CoreUtilities;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Resources;

    /// <summary>
    /// Utility Methods for sending output to IOutput.
    /// </summary>
    public static class OutputExtensions
    {
        private const string DefaultFormat = "{0}";

        /// <summary>
        /// Output an error message.
        /// </summary>
        /// <param name="output">Output instance the method is being invoked with.</param>
        /// <param name="format">Format string for the error message.</param>
        /// <param name="foregroundColor">Color in which text prints.</param>
        /// <param name="args">Arguments to format into the format string.</param>
        public static void Error(this IOutput output, string format, ConsoleColor foregroundColor=ConsoleColor.Red, params object[] args)
        {
            ConsoleColorHelper.SetColorForAction(foregroundColor, () =>
            {
                Output(output, OutputLevel.Error, DefaultFormat, format, args);
            });
        }

        /// <summary>
        /// Output a warning message.
        /// </summary>
        /// <param name="output">Output instance the method is being invoked with.</param>
        /// <param name="format">Format string for the warning message.</param>
        /// <param name="foregroundColor">Color in which text prints.</param>
        /// <param name="args">Arguments to format into the format string.</param>
        public static void Warning(this IOutput output, string format, ConsoleColor foregroundColor=ConsoleColor.Yellow, params object[] args)
        {
            ConsoleColorHelper.SetColorForAction(foregroundColor, () =>
            {
                Output(output, OutputLevel.Warning, DefaultFormat, format, args);
            });
        }

        /// <summary>
        /// Output a informational message.
        /// </summary>
        /// <param name="output">Output instance the method is being invoked with.</param>
        /// <param name="format">Format string for the informational message.</param>
        /// <param name="foregroundColor">Color in which text prints.</param>
        /// <param name="args">Arguments to format into the format string.</param>
        public static void Information(this IOutput output, string format, ConsoleColor foregroundColor=ConsoleColor.White, params object[] args)
        {
            ConsoleColorHelper.SetColorForAction(foregroundColor, () =>
            {
                Output(output, OutputLevel.Information, DefaultFormat, format, args);
            });
        }

        /// <summary>
        /// Formats the message.
        /// </summary>
        /// <param name="output">An output instance to write the message.</param>
        /// <param name="level">Message level.</param>
        /// <param name="messageTypeFormat">Format string for the message type.</param>
        /// <param name="format">Format string for the error message.</param>
        /// <param name="args">Arguments to format into the format string.</param>
        private static void Output(IOutput output, OutputLevel level, string messageTypeFormat, string format, params object[] args)
        {
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            output.WriteLine(Format(messageTypeFormat, format, args), level);
        }

        /// <summary>
        /// Formats the message.
        /// </summary>
        /// <param name="messageTypeFormat">Format string for the message type.</param>
        /// <param name="format">Format string for the error message.</param>
        /// <param name="args">Arguments to format into the format string.</param>
        /// <returns>Formatted message.</returns>
        private static string Format(string messageTypeFormat, string format, params object[] args)
        {
            if (format == null || string.IsNullOrEmpty(format.Trim()))
            {
                throw new ArgumentException(Resources.CannotBeNullOrEmpty, nameof(format));
            }

            string message = null;
            if (args != null && args.Length > 0)
            {
                message = string.Format(CultureInfo.CurrentCulture, format, args);
            }
            else
            {
                message = format;
            }

            return string.Format(CultureInfo.CurrentCulture, messageTypeFormat, message);
        }
    }
}
