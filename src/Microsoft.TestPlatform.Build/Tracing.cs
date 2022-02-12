// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Build.Trace;

using System;

public static class Tracing
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Part of the public API.")]
    public static bool traceEnabled = false;

    public static void Trace(string message)
    {
        if (traceEnabled)
        {
            Console.WriteLine(message);
        }
    }
}
