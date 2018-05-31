// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Filtering
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using Microsoft.VisualStudio.TestPlatform.Common.Resources;

    public static class FilterHelpers
    {
        private const char EscapePrefix = '%';  
        private static readonly Dictionary<char, char> LookUpMap =
            new Dictionary<char, char>()
            {
                { '%', '0'},
                { '(', '1'},
                { ')', '2'},
                { '&', '3'},
                { '|', '4'},
                { '=', '5'},
                { '!', '6'},
                { '~', '7'},
            };

        private static readonly Dictionary<char, char> ReverseLookUpMap = LookUpMap.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);    
        private static readonly char[] SpecialCharacters = LookUpMap.Keys.ToArray();

        /// <summary>
        /// Escapes a set of special characters for filter (%, (, ), &, |, =, !, ~) by replacing them with their escape sequences. 
        /// </summary>
        /// <param name="str">The input string that contains the text to convert.</param>
        /// <returns>A string of characters with special characters converted to their escaped form.</returns>
        public static string Escape(string str)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            if (str.IndexOfAny(SpecialCharacters) < 0)
            {
                return str;
            }

            var builder = new StringBuilder();
            for (int i = 0; i < str.Length; ++i)
            {
                var currentChar = str[i];
                if (LookUpMap.TryGetValue(currentChar, out var escaped))
                {
                    builder.Append(EscapePrefix);
                    currentChar = escaped;
                }
                builder.Append(currentChar);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Converts any escaped characters in the input filter string.
        /// </summary>
        /// <param name="str"></param>
        /// <returns>A filter string of characters with any escaped characters converted to their unescaped form.</returns>
        public static string Unescape(string str)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            if (str.IndexOf(EscapePrefix) < 0)
            {
                return str;
            }

            var builder = new StringBuilder();
            for (int i = 0; i < str.Length; ++i)
            {
                var currentChar = str[i];
                if (currentChar == EscapePrefix)
                {
                    if (++i == str.Length || !ReverseLookUpMap.TryGetValue(currentChar = str[i], out var specialChar))
                    {
                        // "\" should be followed by a valid escape seq. i.e. '0' - '7'.
                        throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.TestCaseFilterEscapeException, str));
                    }
                    currentChar = specialChar;
                }

                // Strictly speaking, string to be unescaped shouldn't contain any of the special characters (except '%'),
                // but we will ignore that to avoid additional overhead.

                builder.Append(currentChar);
            }

            return builder.ToString();
        }
    }
}
