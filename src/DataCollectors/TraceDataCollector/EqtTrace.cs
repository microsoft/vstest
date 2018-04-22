// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestTools.Diagnostics
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Threading;

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
    public static class EqtTrace
    {
        /// <summary>
        /// Name of the trace listener.
        /// </summary>
        private const string ListenerName = "TptTraceListener";

        /// <summary>
        /// Use a custom trace source. This doesn't pollute the default tracing for user applications.
        /// </summary>
        private static readonly TraceSource Source = new TraceSource("TpTrace", SourceLevels.Off);

        /// <summary>
        /// Create static maps for TraceLevel to SourceLevels. The APIs need to provide TraceLevel
        /// for backward compatibility with older versions of Object Model.
        /// </summary>
        private static readonly Dictionary<TraceLevel, SourceLevels> TraceSourceLevelsMap =
            new Dictionary<TraceLevel, SourceLevels>
                {
                        { TraceLevel.Error, SourceLevels.Error },
                        { TraceLevel.Info, SourceLevels.Information },
                        { TraceLevel.Off, SourceLevels.Off },
                        { TraceLevel.Verbose, SourceLevels.Verbose },
                        { TraceLevel.Warning, SourceLevels.Warning }
                };

        /// <summary>
        /// Create static maps for SourceLevels to TraceLevel. The APIs need to provide TraceLevel
        /// for backward compatibility with older versions of Object Model.
        /// </summary>
        private static readonly Dictionary<SourceLevels, TraceLevel> SourceTraceLevelsMap =
            new Dictionary<SourceLevels, TraceLevel>
                {
                        { SourceLevels.Error, TraceLevel.Error },
                        { SourceLevels.Information, TraceLevel.Info },
                        { SourceLevels.Off, TraceLevel.Off },
                        { SourceLevels.Verbose, TraceLevel.Verbose },
                        { SourceLevels.Warning, TraceLevel.Warning },
                        { SourceLevels.All, TraceLevel.Verbose }
                };

        /// <summary>
        /// Create static maps for SourceLevels to TraceLevel. The APIs need to provide TraceLevel
        /// for backward compatibility with older versions of Object Model.
        /// </summary>
        private static readonly Dictionary<TraceLevel, TraceEventType> TraceLevelEventTypeMap =
            new Dictionary<TraceLevel, TraceEventType>
                {
                        { TraceLevel.Error, TraceEventType.Error },
                        { TraceLevel.Info, TraceEventType.Information },
                        { TraceLevel.Verbose, TraceEventType.Verbose },
                        { TraceLevel.Warning, TraceEventType.Warning }
                };

        // Current process name/id that called trace so that it's easier to read logs.
        // We cache them for performance reason.
        private static readonly string ProcessName = GetProcessName();

        private static readonly int ProcessId = GetProcessId();

        /// <summary>
        /// Specifies whether the trace is initialized or not
        /// </summary>
        private static bool isInitialized = false;

        /// <summary>
        /// Lock over initialization
        /// </summary>
        private static object isInitializationLock = new object();

        private static int traceFileSize = 0;
        private static int defaultTraceFileSize = 10240; // 10Mb.

        /// <summary>
        /// Gets the log file for tracing.
        /// </summary>
        public static string LogFile
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the trace level.
        /// </summary>
        public static TraceLevel TraceLevel
        {
            get
            {
                return SourceTraceLevelsMap[Source.Switch.Level];
            }

            set
            {
                try
                {
                    Source.Switch.Level = TraceSourceLevelsMap[value];
                }
                catch (ArgumentException e)
                {
                    LogIgnoredException(e);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether tracing error statements is enabled.
        /// </summary>
        public static bool IsErrorEnabled
        {
            get
            {
                return ShouldTrace(TraceLevel.Error);
            }
        }

        /// <summary>
        /// Gets a value indicating whether tracing info statements is enabled.
        /// </summary>
        public static bool IsInfoEnabled
        {
            get
            {
                return ShouldTrace(TraceLevel.Info);
            }
        }

        /// <summary>
        /// Gets a value indicating whether tracing verbose statements is enabled.
        /// </summary>
        public static bool IsVerboseEnabled
        {
            get
            {
                return ShouldTrace(TraceLevel.Verbose);
            }
        }

        /// <summary>
        /// Gets a value indicating whether tracing warning statements is enabled.
        /// </summary>
        public static bool IsWarningEnabled
        {
            get
            {
                return ShouldTrace(TraceLevel.Warning);
            }
        }

        /// <summary>
        /// Initializes the verbose tracing with custom log file
        /// And overrides if any trace is set before
        /// </summary>
        /// <param name="customLogFile">A custom log file for trace messages.</param>
        public static void InitializeVerboseTrace(string customLogFile)
        {
            isInitialized = false;

            LogFile = customLogFile;
            TraceLevel = TraceLevel.Verbose;
            Source.Switch.Level = SourceLevels.All;
        }

        /// <summary>
        /// Setup a custom trace listener instead of default trace listener created by test platform.
        /// This is needed by DTA Agent where it needs to listen test platform traces but doesn't use test platform listener.
        /// </summary>
        /// <param name="listener">
        /// The listener.
        /// </param>
        public static void SetupListener(TraceListener listener)
        {
            lock (isInitializationLock)
            {
                // Add new listeners.
                if (listener != null)
                {
                    Source.Listeners.Add(listener);
                }

                isInitialized = true;
            }
        }

        /// <summary>
        /// Gets a value indicating if tracing is enabled for a trace level.
        /// </summary>
        /// <param name="traceLevel">Trace level.</param>
        /// <returns>True if tracing is enabled.</returns>
        public static bool ShouldTrace(TraceLevel traceLevel)
        {
            switch (traceLevel)
            {
                case TraceLevel.Off:
                    return false;
                case TraceLevel.Error:
                    return Source.Switch.ShouldTrace(TraceEventType.Error);
                case TraceLevel.Warning:
                    return Source.Switch.ShouldTrace(TraceEventType.Warning);
                case TraceLevel.Info:
                    return Source.Switch.ShouldTrace(TraceEventType.Information);
                case TraceLevel.Verbose:
                    return Source.Switch.ShouldTrace(TraceEventType.Verbose);
                default:
                    Debug.Fail("Should never get here!");
                    return false;
            }
        }

        /// <summary>
        /// Prints an error message and prompts with a Debug dialog
        /// </summary>
        /// <param name="message">the error message</param>
        [Conditional("TRACE")]
        public static void Fail(string message)
        {
            Error(message);
            Debug.Fail(message);
        }

        /// <summary>
        /// Trace an error message.
        /// </summary>
        /// <param name="message">Error message.</param>
        [Conditional("TRACE")]
        public static void Error(string message)
        {
            if (ShouldTrace(TraceLevel.Error))
            {
                WriteLine(TraceLevel.Error, message);
            }
        }

        /// <summary>
        /// Trace an error message with formatting arguments.
        /// </summary>
        /// <param name="format">Format of error message.</param>
        /// <param name="args">Parameters for the error message.</param>
        [Conditional("TRACE")]
        public static void Error(string format, params object[] args)
        {
            Debug.Assert(format != null, "format != null");

            // Check level before doing string.Format to avoid string creation if tracing is off.
            if (ShouldTrace(TraceLevel.Error))
            {
                Error(string.Format(CultureInfo.InvariantCulture, format, args));
            }
        }

        /// <summary>
        /// Write a exception if tracing for error is enabled
        /// </summary>
        /// <param name="exceptionToTrace">The exception to write.</param>
        [Conditional("TRACE")]
        public static void Error(Exception exceptionToTrace)
        {
            Debug.Assert(exceptionToTrace != null, "exceptionToTrace != null");

            // Write only if tracing for error is enabled.
            // Done upfront to avoid perf hit.
            if (ShouldTrace(TraceLevel.Error))
            {
                // Write at error level
                WriteLine(TraceLevel.Error, FormatException(exceptionToTrace));
            }
        }

        /// <summary>
        /// Trace a warning message.
        /// </summary>
        /// <param name="message">Trace message.</param>
        [Conditional("TRACE")]
        public static void Warning(string message)
        {
            if (ShouldTrace(TraceLevel.Warning))
            {
                WriteLine(TraceLevel.Warning, message);
            }
        }

        /// <summary>
        /// Trace a warning message.
        /// </summary>
        /// <param name="format">Format of the trace message.</param>
        /// <param name="args">Arguments for the trace message format.</param>
        [Conditional("TRACE")]
        public static void Warning(string format, params object[] args)
        {
            Debug.Assert(format != null, "format != null");

            // Check level before doing string.Format to avoid string creation if tracing is off.
            if (ShouldTrace(TraceLevel.Warning))
            {
                Warning(string.Format(CultureInfo.InvariantCulture, format, args));
            }
        }

        /// <summary>
        /// Trace an informational message.
        /// </summary>
        /// <param name="message">Trace message.</param>
        [Conditional("TRACE")]
        public static void Info(string message)
        {
            if (ShouldTrace(TraceLevel.Info))
            {
                WriteLine(TraceLevel.Info, message);
            }
        }

        /// <summary>
        /// Trace an informational message based on a format.
        /// </summary>
        /// <param name="format">Trace message format.</param>
        /// <param name="args">Arguments for trace format.</param>
        [Conditional("TRACE")]
        public static void Info(string format, params object[] args)
        {
            Debug.Assert(format != null, "format != null");

            // Check level before doing string.Format to avoid string creation if tracing is off.
            if (ShouldTrace(TraceLevel.Info))
            {
                Info(string.Format(CultureInfo.InvariantCulture, format, args));
            }
        }

        /// <summary>
        /// Trace a verbose message.
        /// </summary>
        /// <param name="message">Trace message.</param>
        [Conditional("TRACE")]
        public static void Verbose(string message)
        {
            if (ShouldTrace(TraceLevel.Verbose))
            {
                WriteLine(TraceLevel.Verbose, message);
            }
        }

        /// <summary>
        /// Trace a verbose message.
        /// </summary>
        /// <param name="format">Format of trace message.</param>
        /// <param name="args">Arguments for trace message.</param>
        [Conditional("TRACE")]
        public static void Verbose(string format, params object[] args)
        {
            Debug.Assert(format != null, "format != null");

            // Check level before doing string.Format to avoid string creation if tracing is off.
            if (ShouldTrace(TraceLevel.Verbose))
            {
                Verbose(string.Format(CultureInfo.InvariantCulture, format, args));
            }
        }

        /// <summary>
        /// Ensure the trace is initialized
        /// </summary>
        private static void EnsureTraceIsInitialized()
        {
            if (isInitialized)
            {
                return;
            }

            lock (isInitializationLock)
            {
                if (isInitialized)
                {
                    return;
                }

                string logsDirectory = Path.GetTempPath();

                // Set the trace level and add the trace listener
                if (LogFile == null)
                {
                    using (var process = Process.GetCurrentProcess())
                    {
                        // In case of parallel execution, there may be several processes with same name.
                        // Add a process id to make the traces unique.
                        LogFile = Path.Combine(
                            logsDirectory,
                            Path.GetFileNameWithoutExtension(process.MainModule.FileName) + "." + process.Id + ".TpTrace.log");
                    }
                }

                // Add a default listener
                traceFileSize = defaultTraceFileSize;
                Source.Listeners.Add(new TextWriterTraceListener(new FileStream(LogFile, FileMode.Create)));

                isInitialized = true;
            }
        }

        /// <summary>
        /// Formats an exception into a nice looking message.
        /// </summary>
        /// <param name="exceptionToTrace">The exception to write.</param>
        /// <returns>The formatted string.</returns>
        private static string FormatException(Exception exceptionToTrace)
        {
            // Prefix for each line
            string prefix = Environment.NewLine + '\t';

            // Format this exception
            StringBuilder message = new StringBuilder();
            message.Append(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Exception: {0}{1}Message: {2}{3}Stack Trace: {4}",
                    exceptionToTrace.GetType(),
                    prefix,
                    exceptionToTrace.Message,
                    prefix,
                    exceptionToTrace.StackTrace));

            // If there is base exception, add that to message
            if (exceptionToTrace.GetBaseException() != null)
            {
                message.Append(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}BaseExceptionMessage: {1}",
                        prefix,
                        exceptionToTrace.GetBaseException().Message));
            }

            // If there is inner exception, add that to message
            // We deliberately avoid recursive calls here.
            if (exceptionToTrace.InnerException != null)
            {
                // Format same as outer exception except
                // "InnerException" is prefixed to each line
                Exception inner = exceptionToTrace.InnerException;
                prefix += "InnerException";
                message.Append(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}: {1}{2} Message: {3}{4} Stack Trace: {5}",
                        prefix,
                        inner.GetType(),
                        prefix,
                        inner.Message,
                        prefix,
                        inner.StackTrace));

                if (inner.GetBaseException() != null)
                {
                    message.Append(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "{0}BaseExceptionMessage: {1}",
                            prefix,
                            inner.GetBaseException().Message));
                }
            }

            // Append a new line
            message.Append(Environment.NewLine);

            return message.ToString();
        }

        /// <summary>
        /// Get the process name. Note: we cache it, use m_processName.
        /// </summary>
        /// <returns>Name of the process.</returns>
        private static string GetProcessName()
        {
            try
            {
                // return ProcessHelper.GetProcessName();
                string processName = null;

                string[] args = Environment.GetCommandLineArgs();

                if (args != null && args.Length != 0)
                {
                    // Leave the extension if specified, otherwise don't add it (e.g. case a.exe.exe).
                    // It seems that if .exe suffix is not specified Framework adds .EXE to agrs[0].
                    processName = Path.GetFileName(args[0]);
                }

                // If we still have not got process name from command line - use the slow way.
                // This should never happen unless the process is called from execv with empty cmdline.
                if (string.IsNullOrEmpty(processName))
                {
                    Debug.Fail("Could not get process name from command line, will try to use the slow way.");
                    using (var process = Process.GetCurrentProcess())
                    {
                        processName = process.ProcessName;
                    }
                }

                return processName;
            }
            catch (Exception e)
            {
                // valid suppress
                Debug.Fail("Could not get process name: " + e);
                LogIgnoredException(e);
                return "Unknown Process";
            }
        }

        private static int GetProcessId()
        {
            try
            {
                using (var process = Process.GetCurrentProcess())
                {
                    return process.Id;
                }
            }
            catch (InvalidOperationException e)
            {
                Debug.Fail("Could not get process id: " + e);
                LogIgnoredException(e);
                return -1;
            }
        }

        /// <summary>
        /// Adds the message to the trace log.
        /// The line becomes:
        ///     [I, PID, ThreadID, 2003/06/11 11:56:07.445] CallingAssemblyName: message.
        /// </summary>
        /// <param name="level">Trace level.</param>
        /// <param name="message">The message to add to trace.</param>
        private static void WriteLine(TraceLevel level, string message)
        {
            Debug.Assert(message != null, "message != null");
            Debug.Assert(!string.IsNullOrEmpty(ProcessName), "!string.IsNullOrEmpty(ProcessName)");

            // Ensure trace is initlized
            EnsureTraceIsInitialized();

            // The format below is a CSV so that Excel could be used easily to
            // view/filter the logs.
            var log = string.Format(
                CultureInfo.InvariantCulture,
                "{0}, {1}, {2:yyyy}/{2:MM}/{2:dd}, {2:HH}:{2:mm}:{2:ss}.{2:fff}, {5}, {3}, {4}",
                ProcessId,
                Thread.CurrentThread.ManagedThreadId,
                DateTime.Now,
                ProcessName,
                message,
                Stopwatch.GetTimestamp());

            try
            {
                Source.TraceEvent(TraceLevelEventTypeMap[level], 0, log);
                Source.Flush();
            }
            catch (Exception e)
            {
                // valid suppress
                // Log exception from tracing into event viewer.
                LogIgnoredException(e);
            }
        }

        /// <summary>
        /// Auxiliary method: logs the exception that is being ignored.
        /// </summary>
        /// <param name="e">The exception to log.</param>
        private static void LogIgnoredException(Exception e)
        {
            Debug.Assert(e != null, "e != null");

            EnsureTraceIsInitialized();

            try
            {
                // Note: Debug.WriteLine may throw if there is a problem in .config file.
                Debug.WriteLine("Ignore exception: " + e);
            }
            catch
            {
                // Ignore everything at this point.
            }
        }
    }
}