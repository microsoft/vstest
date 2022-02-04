// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System;

#if NETFRAMEWORK || WINDOWS_UWP || (NETCOREAPP && !NETCOREAPP2_1_OR_GREATER) || (NETSTANDARD && !NETSTANDARD2_1_OR_GREATER)
/// <summary>
/// This type acts as a polyfill to enable using latest features will keeping compatibility with old framework.
/// Using latest features helps us to benefit from performance/readability gain and helps with enforcing standard
/// coding styles/conventions across the codebase while reducing mental overhead to know whether an API is available
/// for the given target framework.
/// </summary>
/// <remarks>
/// - Do not add other non-polyfilling members to this type, instead create different helper.
/// - The full class needs to be conditionally included otherwise, despite a working build, we end up with runtime missing methods.
/// </remarks>
internal static class PolyfillStringExtensions
{
    public static bool Contains(this string s, char c)
        => s.Contains(c.ToString());

    public static bool Contains(this string s, string value, StringComparison comparisonType)
        => s.IndexOf(value, comparisonType) >= 0;
}
#endif
