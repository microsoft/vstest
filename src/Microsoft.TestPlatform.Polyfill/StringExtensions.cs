// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETFRAMEWORK || WINDOWS_UWP || (NETCOREAPP && !NETCOREAPP2_1_OR_GREATER) || (NETSTANDARD && !NETSTANDARD2_1_OR_GREATER)

namespace System;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

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
[SuppressMessage("ApiDesign", "RS0016:Add public types and members to the declared API", Justification = PublicApiSuppressionJustification)]
[SuppressMessage("ApiDesign", "RS0037:Enable tracking of nullability of reference types in the declared API", Justification = PublicApiSuppressionJustification)]
public static class StringExtensions
{
    private const string PublicApiSuppressionJustification =
        "Ideally we would want to the type to be internal but that's causing some ambiguous resolving. " +
        "Besides, we want to avoid declaring this API in all of the Public API files of all projects referring this shared project.";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains(this string s, char c)
        => s.Contains(c.ToString());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains(this string s, string value, StringComparison comparisonType)
        => s.IndexOf(value, comparisonType) >= 0;
}

#endif
