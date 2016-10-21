// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities
{
    using System;

    /// <summary>
    /// Utility methods for manipulating strings.
    /// </summary>
    public static class StringUtilities
    {
        /// <summary>
        /// Prepares the string for output by converting null values to the "(null)" string
        /// and removing any trailing new lines.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>The string that is prepared for output.</returns>
        public static string PrepareForOutput(string input)
        {
            string result = input;
            if (input == null)
            {
                result = Resources.Resources.NullString;
            }

            result = result.TrimEnd(Environment.NewLine.ToCharArray());

            return result;
        }

        /// <summary>
        /// Checks if given string is null or a whitespace.
        /// </summary>
        /// <param name="input">string to check</param>
        /// <returns>True if string is null or a whitespace, false otherwise</returns>
        public static bool IsNullOrWhiteSpace(string input)
        {
            if (input != null)
            {
                input = input.Trim();
            }

            return string.IsNullOrEmpty(input);
        }
    }
}
