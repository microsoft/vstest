// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETSTANDARD && !NETSTANDARD2_0

using System;
using System.Diagnostics;

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel;

/// <summary>
/// Wrapper class for tracing.
///     - Shortcut-methods for Error, Warning, Info, Verbose.
///     - Adds additional information to the trace: calling process name, PID, ThreadID, Time.
///     - Uses custom switch <c>EqtTraceLevel</c> from .config file.
///     - By default tracing if OFF.
///     - Our build environment always sets the /d:TRACE so this class is always enabled,
///       the Debug class is enabled only in debug builds (/d:DEBUG).
///     - We ignore exceptions thrown by underlying TraceSwitch (e.g. due to config file error).
///       We log ignored exceptions to system Application log.
///       We pass through exceptions thrown due to incorrect arguments to <c>EqtTrace</c> methods.
/// Usage: <c>EqtTrace.Info("Here's how to trace info");</c>
/// </summary>
public partial class PlatformEqtTrace : IPlatformEqtTrace
{
    private PlatformTraceLevel _traceLevel = PlatformTraceLevel.Off;

    public static string ErrorOnInitialization { get; set; }

    public bool DoNotInitialize { get; set; }

    public void WriteLine(PlatformTraceLevel traceLevel, string message)
    {
        if (!ShouldTrace(traceLevel))
        {
            return;
        }

        var level = Enum.GetName(typeof(PlatformTraceLevel), traceLevel);

        Debug.WriteLine($"[{level}] {message}");
    }

    public bool InitializeVerboseTrace(string customLogFile)
    {
#if DEBUG
        // We don't have access to System.Diagnostics.Trace on netstandard1.3
        // so we write to Debug. No need to initialize for non-debug builds.
        return true;
#else
        return false;
#endif
    }

    public bool InitializeTrace(string customLogFile, PlatformTraceLevel traceLevel)
    {
        _traceLevel = traceLevel;

#if DEBUG
        // We don't have access to System.Diagnostics.Trace on netstandard1.3
        // so we write to Debug. No need to initialize for non-debug builds.
        return true;
#else
        return false;
#endif
    }

    public bool ShouldTrace(PlatformTraceLevel traceLevel)
    {
        return !DoNotInitialize && (int)_traceLevel >= (int)traceLevel;
    }

    public string GetLogFile() => string.Empty;

    public void SetTraceLevel(PlatformTraceLevel value)
    {
        _traceLevel = value;
    }

    public PlatformTraceLevel GetTraceLevel() => _traceLevel;
}

#endif
