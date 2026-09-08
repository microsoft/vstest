// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CoreUtilities.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Add double quote around string. Useful in case of path which has white space in between.
    /// Embedded double quotes are escaped and any run of backslashes immediately preceding the
    /// closing quote is doubled, so the resulting value round-trips correctly when parsed back
    /// as a single command-line argument (matching Windows CommandLineToArgvW parsing rules).
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string AddDoubleQuote(this string value)
    {
        var escaped = value.Replace("\"", "\\\"");

        // If the value (before escaping quotes) ends with one or more backslashes, those
        // backslashes must be doubled, otherwise they would escape the closing quote we're
        // about to append (e.g. "D:\" would be parsed as "D:" followed by a literal quote).
        var trailingBackslashes = 0;
        for (var i = value.Length - 1; i >= 0 && value[i] == '\\'; i--)
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
