// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.ProgrammerTests.CommandLine;

internal static class StringExtensions
{
    public static string Join(this IEnumerable<string> value, string separator)
    {
        return string.Join(separator, value);
    }

    public static string JoinBySpace(this IEnumerable<string> value)
    {
        return string.Join(" ", value);
    }

    public static List<string> ToList(this string value)
    {
        return new List<string> { value };
    }
}
