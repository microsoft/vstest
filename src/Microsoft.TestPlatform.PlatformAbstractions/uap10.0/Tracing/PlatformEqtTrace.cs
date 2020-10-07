// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if WINDOWS_UWP

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System;
    using System.Diagnostics.Tracing;
    using System.IO;

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
        private static object initLock = new object();

        private static bool isInitialized = false;

        public static string ErrorOnInitialization { get; set; }

        public static string LogFile { get; set; }

        public bool DoNotInitialize
        {
            get;
            set;
        }

        private static PlatformTraceLevel TraceLevel { get; set; }

        /// <inheritdoc/>
        public void WriteLine(PlatformTraceLevel level, string message)
        {
            if (this.TraceInitialized() && TraceLevel > PlatformTraceLevel.Off)
            {
                switch (level)
                {
                    case PlatformTraceLevel.Off:
                        break;

                    case PlatformTraceLevel.Error:
                        UnitTestEventSource.Log.Error(message);
                        break;

                    case PlatformTraceLevel.Warning:
                        UnitTestEventSource.Log.Warn(message);
                        break;

                    case PlatformTraceLevel.Info:
                        UnitTestEventSource.Log.Info(message);
                        break;

                    case PlatformTraceLevel.Verbose:
                        UnitTestEventSource.Log.Verbose(message);
                        break;
                }
            }
        }

        /// <inheritdoc/>
        public bool InitializeVerboseTrace(string customLogFile)
        {
            return this.InitializeTrace(customLogFile, PlatformTraceLevel.Verbose);
        }

        /// <inheritdoc/>
        public bool InitializeTrace(string customLogFile, PlatformTraceLevel traceLevel)
        {
            string logFileName = string.Empty;
            try
            {
                logFileName = Path.GetFileNameWithoutExtension(customLogFile.TrimStart('"').TrimEnd('"')).Replace(" ", "_");
            }
            catch
            {
                logFileName = Guid.NewGuid().ToString();
            }

            LogFile = Path.Combine(Path.GetTempPath(), logFileName + ".TpTrace.log");
            TraceLevel = traceLevel;

            return this.TraceInitialized();
        }

        /// <inheritdoc/>
        public bool ShouldTrace(PlatformTraceLevel traceLevel)
        {
            return isInitialized;
        }

        /// <inheritdoc/>
        public string GetLogFile()
        {
            return LogFile;
        }

        /// <inheritdoc/>
        public void SetTraceLevel(PlatformTraceLevel value)
        {
            TraceLevel = value;
        }

        /// <inheritdoc/>
        public PlatformTraceLevel GetTraceLevel()
        {
            return TraceLevel;
        }

        /// <summary>
        /// Initializes Tracing based on Trace Level
        /// </summary>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        private bool TraceInitialized()
        {
            lock (initLock)
            {
                if (isInitialized)
                {
                    return isInitialized;
                }

                try
                {
                    var eventListener = new FileEventListener(string.IsNullOrEmpty(LogFile) ? "UnitTestLog" : LogFile);

                    PlatformTraceLevel traceLevel = this.GetTraceLevel();
                    if (traceLevel > PlatformTraceLevel.Off)
                    {
                        eventListener.EnableEvents(UnitTestEventSource.Log, EventLevel.Error);
                    }

                    if (traceLevel > PlatformTraceLevel.Error)
                    {
                        eventListener.EnableEvents(UnitTestEventSource.Log, EventLevel.Warning);
                    }

                    if (traceLevel > PlatformTraceLevel.Warning)
                    {
                        eventListener.EnableEvents(UnitTestEventSource.Log, EventLevel.Informational);
                    }

                    if (traceLevel > PlatformTraceLevel.Info)
                    {
                        eventListener.EnableEvents(UnitTestEventSource.Log, EventLevel.Verbose);
                    }

                    isInitialized = true;
                }
                catch (Exception ex)
                {
                    this.UnInitializeTrace();
                    ErrorOnInitialization = ex.Message;
                    return false;
                }

                return isInitialized;
            }
        }

        private void UnInitializeTrace()
        {
            isInitialized = false;
            LogFile = null;
            TraceLevel = PlatformTraceLevel.Off;
        }
    }
}

#endif
