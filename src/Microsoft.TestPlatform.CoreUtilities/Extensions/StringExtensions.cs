// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Microsoft.VisualStudio.TestPlatform.CoreUtilities.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Quotes a command-line argument, escaping embedded quotes and backslashes before quotes.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string AddDoubleQuote(this string value)
    {
        var builder = new StringBuilder();
        builder.Append('"');
        var backslashes = 0;
        foreach (var character in value)
        {
            if (character == '\\')
            {
                backslashes++;
                continue;
            }

            // Before a literal quote, double preceding backslashes and escape the quote itself.
            builder.Append('\\', character == '"' ? (backslashes * 2) + 1 : backslashes);
            builder.Append(character);
            backslashes = 0;
        }

        // Trailing backslashes must not escape the closing quote.
        builder.Append('\\', backslashes * 2);
        builder.Append('"');

        return builder.ToString();
    }
}
