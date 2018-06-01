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
        private const char EscapePrefix = '\\';    
        private static readonly char[] SpecialCharacters = { '\\', '(', ')', '&', '|', '=', '!', '~' };

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
                if (SpecialCharacters.Contains(currentChar))
                {
                    builder.Append(EscapePrefix);
                }
                builder.Append(currentChar);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Converts any escaped characters in the input filter string.
        /// </summary>
        /// <param name="str">The input string that contains the text to convert.</param>
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
                    if (++i == str.Length || !SpecialCharacters.Contains(currentChar = str[i]))
                    {
                        // "\" should be followed by a special character.
                        throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.TestCaseFilterEscapeException, str));
                    }
                }

                // Strictly speaking, string to be unescaped shouldn't contain any of the special characters, 
                // other than being part of escape sequence, but we will ignore that to avoid additional overhead.

                builder.Append(currentChar);
            }

            return builder.ToString();
        }

        internal static IEnumerable<string> TokenizeFilterExpressionString(string str)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            StringBuilder tokenBuilder = new StringBuilder();

            var last = '\0';
            for (int i = 0; i < str.Length; ++i)
            {
                var current = str[i];

                if (last == EscapePrefix)
                {
                    // Don't check if `current` is one of the special characters here.
                    // Instead, we blindly let any character follows '\' pass though and 
                    // relies on `FiltetrHelpers.Unescape` to report such errors.
                    tokenBuilder.Append(current);

                    if (current == EscapePrefix)
                    {
                        // We just encountered "\\" (escaped '\'), this will set last to '\0' 
                        // so the next char will not be treated as a suffix of escape sequence.
                        current = '\0';
                    }
                }
                else
                {
                    switch (current)
                    {
                        case '(':
                        case ')':
                        case '&':
                        case '|':
                            if (tokenBuilder.Length > 0)
                            {
                                yield return tokenBuilder.ToString();
                                tokenBuilder.Clear();
                            }
                            yield return current.ToString();   
                            break;

                        default:
                            tokenBuilder.Append(current);
                            break;
                    }      
                }

                last = current;
            }

            if (tokenBuilder.Length > 0)
            {
                yield return tokenBuilder.ToString();
            }
        }

        internal static IEnumerable<string> TokenizeFilterConditionString(string str)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            StringBuilder tokenBuilder = new StringBuilder();

            var last = '\0';
            for (int i = 0; i < str.Length; ++i)
            {
                var current = str[i];

                if (last == FilterHelpers.EscapePrefix)
                {
                    // Don't check if `current` is one of the special characters here.
                    // Instead, we blindly let any character follows '\' pass though and 
                    // relies on `FiltetrHelpers.Unescape` to report such errors.
                    tokenBuilder.Append(current);

                    if (current == FilterHelpers.EscapePrefix)
                    {
                        // We just encountered "\\" (escaped '\'), this will set last to '\0' 
                        // so the next char will not be treated as a suffix of escape sequence.
                        current = '\0';
                    }
                }
                else
                {
                    switch (current)
                    {
                        case '=':
                            if (tokenBuilder.Length > 0)
                            {
                                yield return tokenBuilder.ToString();
                                tokenBuilder.Clear();
                            }
                            yield return "=";
                            break;

                        case '!':
                            if (tokenBuilder.Length > 0)
                            {
                                yield return tokenBuilder.ToString();
                                tokenBuilder.Clear();
                            }
                            // Determine if this is a "!=" or just a single "!".
                            var next = str[i + 1];
                            if (next == '=')
                            {
                                ++i;
                                current = next;
                                yield return "!=";
                            }
                            else
                            {
                                yield return "!";
                            }
                            break;

                        case '~':
                            if (tokenBuilder.Length > 0)
                            {
                                yield return tokenBuilder.ToString();
                                tokenBuilder.Clear();
                            }
                            yield return "~";
                            break;

                        default:
                            tokenBuilder.Append(current);
                            break;
                    }
                }
                last = current;
            }

            if (tokenBuilder.Length > 0)
            {
                yield return tokenBuilder.ToString();
            }
        }
    }
}
