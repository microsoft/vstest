// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CoreUtilities.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Add double quote around string. Useful in case of path which has white space in between.
    /// Embedded double quotes are escaped and any run of trailing backslashes immediately
    /// preceding the closing quote is doubled, so the resulting value round-trips correctly
    /// through Windows command-line parsing (CommandLineToArgvW).
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string AddDoubleQuote(this string value)
    {
        var escaped = value.Replace("\"", "\\\"");

        // Count trailing backslashes (after the quote-escaping above) and double them so they
        // aren't interpreted as escaping the closing quote we're about to append.
        int trailingBackslashes = 0;
        for (int i = escaped.Length - 1; i >= 0 && escaped[i] == '\\'; i--)
        {
            trailingBackslashes++;
        }

        if (trailingBackslashes > 0)
        {
            escaped += new string('\\', trailingBackslashes);
        }

        return "\"" + escaped + "\"";
    }
}
