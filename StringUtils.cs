// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.TestPlatform;

internal static class StringUtils
{
    [SuppressMessage("ApiDesign", "RS0030:Do not used banned APIs", Justification = "Replacement API to allow nullable hints for compiler")]
    public static bool IsNullOrEmpty([NotNullWhen(returnValue: false)] this string? value)
        => string.IsNullOrEmpty(value);

    [SuppressMessage("ApiDesign", "RS0030:Do not used banned APIs", Justification = "Replacement API to allow nullable hints for compiler")]
    public static bool IsNullOrWhiteSpace([NotNullWhen(returnValue: false)] this string? value)
        => string.IsNullOrWhiteSpace(value);
}
