// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;

using SystemDebug = System.Diagnostics.Debug;

namespace Microsoft.TestPlatform;

internal static class TPDebug
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
