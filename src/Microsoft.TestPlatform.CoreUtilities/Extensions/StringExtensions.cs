// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CoreUtilities.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Add double quote around string. Useful in case of path which has white space in between.
    /// Embedded double quotes are escaped and any run of backslashes immediately preceding the
    /// closing quote is doubled, so the result parses back to the original value under the
    /// Windows <c>CommandLineToArgvW</c> quoting rules.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string AddDoubleQuote(this string value)
    {
        var escaped = value.Replace("\"", "\\\"");

        var trailingBackslashCount = 0;
        for (int i = escaped.Length - 1; i >= 0 && escaped[i] == '\\'; i--)
        {
            trailingBackslashCount++;
        }

        if (trailingBackslashCount > 0)
        {
            escaped += new string('\\', trailingBackslashCount);
        }

        return "\"" + escaped + "\"";
    }
}
