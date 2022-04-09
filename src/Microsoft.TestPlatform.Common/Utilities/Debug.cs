// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;

using SystemDebug = System.Diagnostics.Debug;

namespace Microsoft.TestPlatform;

[SuppressMessage("ApiDesign", "RS0030:Do not used banned APIs", Justification = "Replacement API to allow nullable hints for compiler")]
internal static class Debug
{
    /// <inheritdoc cref="SystemDebug.Assert(bool)"/>
    [Conditional("DEBUG")]
    public static void Assert([DoesNotReturnIf(false)] bool b)
        => SystemDebug.Assert(b);

    /// <inheritdoc cref="SystemDebug.Assert(bool, string)"/>
    [Conditional("DEBUG")]
    public static void Assert([DoesNotReturnIf(false)] bool b, string message)
        => SystemDebug.Assert(b, message);
}
