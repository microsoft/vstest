// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.TestHost;

using System;

internal sealed class DebugAssertException : Exception
{
    public DebugAssertException(string message, string stackTrace) : base(message)
    {
        StackTrace = stackTrace;
    }

    public override string StackTrace { get; }
}
