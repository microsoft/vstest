﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.TestPlatform.Build.Trace;

public static class Tracing
{
    public static bool traceEnabled = false;

    public static void Trace(string message)
    {
        if (traceEnabled)
        {
            Console.WriteLine(message);
        }
    }
}