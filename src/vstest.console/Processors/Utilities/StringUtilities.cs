// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities
{
    using System.Collections.Generic;
    using System.Text;

    internal static class StringExtensions
    {
        public static IEnumerable<string> Tokenize(this string input, char separator, char escape)
        {
            if (input == null) yield break;
            var buffer = new StringBuilder();
            var escaping = false;
            foreach (var c in input)
            {
                if (escaping)
                {
                    buffer.Append(c);
                    escaping = false;
                }
                else if (c == escape)
                {
                    escaping = true;
                }
                else if (c == separator)
                {
                    yield return buffer.Flush();
                }
                else
                {
                    buffer.Append(c);
                }
            }
            if (buffer.Length > 0 || input[input.Length - 1] == separator) yield return buffer.Flush();
        }

        public static string Flush(this StringBuilder stringBuilder)
        {
            var result = stringBuilder.ToString();
            stringBuilder.Clear();
            return result;
        }
    }
}
