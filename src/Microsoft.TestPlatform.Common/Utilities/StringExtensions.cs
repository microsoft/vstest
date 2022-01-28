// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System;

#if NETFRAMEWORK || WINDOWS_UAP || (NETSTANDARD && !NETSTANDARD2_1_OR_GREATER)
internal static class StringExtensions
{
    public static bool Contains(this string s, string value, StringComparison comparisonType)
        => s.IndexOf(value, comparisonType) >= 0;

    public static bool Contains(this string s, char value)
        => s.Contains(value.ToString());
}
#endif
