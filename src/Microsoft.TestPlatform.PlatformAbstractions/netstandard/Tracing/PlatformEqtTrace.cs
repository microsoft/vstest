// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETSTANDARD

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System;

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
    public class PlatformEqtTrace : IPlatformEqtTrace
    {
        public static string ErrorOnInitialization { get; set; }

        public bool DoNotInitialize
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public void WriteLine(PlatformTraceLevel level, string message)
        {
            throw new NotImplementedException();
        }

        public bool InitializeVerboseTrace(string customLogFile)
        {
            throw new NotImplementedException();
        }

        public bool InitializeTrace(string customLogFile, PlatformTraceLevel traceLevel)
        {
            throw new NotImplementedException();
        }

        public bool ShouldTrace(PlatformTraceLevel traceLevel)
        {
            throw new NotImplementedException();
        }

        public string GetLogFile()
        {
            throw new NotImplementedException();
        }

        public void SetTraceLevel(PlatformTraceLevel value)
        {
            throw new NotImplementedException();
        }

        public PlatformTraceLevel GetTraceLevel()
        {
            throw new NotImplementedException();
        }
    }
}

#endif
