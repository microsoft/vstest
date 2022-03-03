// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if WINDOWS_UWP

using System.Diagnostics.Tracing;

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel;

internal sealed class UnitTestEventSource : EventSource
{
    public static UnitTestEventSource Log { get; } = new UnitTestEventSource();

    [Event(1, Level = EventLevel.Verbose)]
    public void Verbose(string message)
    {
        WriteEvent(1, message);
    }

    [Event(2, Level = EventLevel.Informational)]
    public void Info(string message)
    {
        WriteEvent(2, message);
    }

    [Event(3, Level = EventLevel.Warning)]
    public void Warn(string message)
    {
        WriteEvent(3, message);
    }

    [Event(4, Level = EventLevel.Error)]
    public void Error(string message)
    {
        WriteEvent(4, message);
    }

    [Event(5, Level = EventLevel.Critical)]
    public void Critical(string message)
    {
        WriteEvent(5, message);
    }
}

#endif
