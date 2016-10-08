// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Threading;

    using Microsoft.VisualStudio.TestPlatform.CoreUtilities;

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
        /// The switch for tracing test platform messages only. Initialize Trace listener as a part of this
        /// to avoid a separate Static constructor (to fix CA1810).
        /// </summary>
        private static readonly TraceSwitch TraceLevelSwitch = new TraceSwitch("TpTraceLevel", null);

        // Current process name/id that called trace so that it's easier to read logs.
        // We cache them for performance reason.
        private static readonly string ProcessName = GetProcessName();

        private static readonly int ProcessId = GetProcessId();

        /// <summary>
        /// Specifies whether the trace is initialized or not
        /// </summary>
        private static bool isInitialized = false;

        /// <summary>
        /// Trace listener object to which all test platform traces are written.
        /// </summary>
        private static TraceListener listener;

        /// <summary>
        /// Lock over initialization
        /// </summary>
        private static object isInitializationLock = new object();

        private static int traceFileSize = 0;
        private static int defaultTraceFileSize = 10240; // 10Mb.

        /// <summary>
        /// Log file name to setup custom file for logging.
        /// </summary>
        private static string logFileName = null;

        /// <summary>
        /// Gets or sets the trace level.
        /// </summary>
        public static TraceLevel TraceLevel
        {
            get
            {
                return TraceLevelSwitch.Level;
            }

            set
            {
                try
                {
                    TraceLevelSwitch.Level = value;
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

            logFileName = customLogFile;
            TraceLevel = TraceLevel.Verbose;
        }

#if NET46
        /// <summary>
        /// Setup remote trace listener in the child domain.
        /// If calling domain, doesn't have tracing enabled nothing is done.
        /// </summary>
        /// <param name="childDomain">Child <c>AppDomain</c>.</param>
        public static void SetupRemoteEqtTraceListeners(AppDomain childDomain)
        {
            Debug.Assert(childDomain != null, "domain");
            if (childDomain != null)
            {
                RemoteEqtTrace remoteEqtTrace = (RemoteEqtTrace)childDomain.CreateInstanceFromAndUnwrap(
                    typeof(RemoteEqtTrace).Assembly.Location,
                    typeof(RemoteEqtTrace).FullName);

                remoteEqtTrace.TraceLevel = TraceLevel;

                if (!Enum.Equals(TraceLevel, TraceLevel.Off))
                {
                    TraceListener tptListner = null;
                    foreach (TraceListener listener in Trace.Listeners)
                    {
                        if (string.Equals(listener.Name, ListenerName, StringComparison.OrdinalIgnoreCase))
                        {
                            Debug.Assert(tptListner == null, "Multiple TptListeners found.");
                            tptListner = listener;
                        }
                    }

                    remoteEqtTrace.SetupRemoteListeners(tptListner);
                }
            }
        }
#endif

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
                EqtTrace.listener = listener;
                if (listener != null)
                {
                    Trace.Listeners.Add(listener);
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
                    return TraceLevelSwitch.TraceError;
                case TraceLevel.Warning:
                    return TraceLevelSwitch.TraceWarning;
                case TraceLevel.Info:
                    return TraceLevelSwitch.TraceInfo;
                case TraceLevel.Verbose:
                    return TraceLevelSwitch.TraceVerbose;
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
        /// Combines together <c>EqtTrace.Fail</c> and Debug.Fail:
        /// Prints an formatted error message and prompts with a Debug dialog.
        /// </summary>
        /// <param name="format">The formatted error message</param>
        /// <param name="args">Arguments to the format</param>
        [Conditional("TRACE")]
        public static void Fail(string format, params object[] args)
        {
            string message = string.Format(CultureInfo.InvariantCulture, format, args);
            Error(message);
#if DEBUG
            Debug.Fail(message);
#endif
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
        /// Only prints the message if the condition is true
        /// </summary>
        /// <param name="condition">Condition for tracing.</param>
        /// <param name="message">Trace error message.</param>
        [Conditional("TRACE")]
        public static void ErrorIf(bool condition, string message)
        {
            if (condition)
            {
                Error(message);
            }
        }

        /// <summary>
        /// Only prints the formatted message if the condition is false
        /// </summary>
        /// <param name="condition">Condition for tracing.</param>
        /// <param name="message">Trace error message.</param>
        [Conditional("TRACE")]
        public static void ErrorUnless(bool condition, string message)
        {
            ErrorIf(!condition, message);
        }

        /// <summary>
        /// Prints the message if the condition is false. If the condition is true,
        /// the message is instead printed at the specified trace level.
        /// </summary>
        /// <param name="condition">Condition for trace.</param>
        /// <param name="bumpLevel">Level for trace.</param>
        /// <param name="message">Trace message.</param>
        [Conditional("TRACE")]
        public static void ErrorUnlessAlterTrace(bool condition, TraceLevel bumpLevel, string message)
        {
            if (condition)
            {
                WriteAtLevel(bumpLevel, message);
            }
            else
            {
                Error(message);
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
        /// Only prints the formatted message if the condition is false
        /// </summary>
        /// <param name="condition">Condition for trace.</param>
        /// <param name="format">Message format.</param>
        /// <param name="args">Trace message format arguments.</param>
        [Conditional("TRACE")]
        public static void ErrorUnless(bool condition, string format, params object[] args)
        {
            ErrorIf(!condition, format, args);
        }

        /// <summary>
        /// Prints the message if the condition is false. If the condition is true,
        /// the message is instead printed at the specified trace level.
        /// </summary>
        /// <param name="condition">Condition for trace.</param>
        /// <param name="bumpLevel">Level for trace.</param>
        /// <param name="format">Message format.</param>
        /// <param name="args">Trace message format arguments.</param>
        [Conditional("TRACE")]
        public static void ErrorUnlessAlterTrace(bool condition, TraceLevel bumpLevel, string format, params object[] args)
        {
            if (condition)
            {
                WriteAtLevel(bumpLevel, format, args);
            }
            else
            {
                Error(format, args);
            }
        }

        /// <summary>
        /// Only prints the formatted message if the condition is true
        /// </summary>
        /// <param name="condition">Condition for trace.</param>
        /// <param name="format">Message format.</param>
        /// <param name="args">Trace message format arguments.</param>
        [Conditional("TRACE")]
        public static void ErrorIf(bool condition, string format, params object[] args)
        {
            if (condition)
            {
                Error(format, args);
            }
        }

        /// <summary>
        /// Error and Debug.Fail combined in one call.
        /// </summary>
        /// <param name="format">The message to send to Debug.Fail and Error.</param>
        /// <param name="args">Arguments to string.Format.</param>
        [Conditional("TRACE")]
        public static void ErrorAssert(string format, params object[] args)
        {
            Debug.Assert(format != null, "format != null");
            var message = string.Format(CultureInfo.InvariantCulture, format, args);
            Error(message);
            Debug.Fail(message);
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
        /// Only prints the formatted message if the condition is true
        /// </summary>
        /// <param name="condition">Condition to evaluate for tracing.</param>
        /// <param name="message">Message to trace.</param>
        [Conditional("TRACE")]
        public static void WarningIf(bool condition, string message)
        {
            if (condition)
            {
                Warning(message);
            }
        }

        /// <summary>
        /// Only prints the formatted message if the condition is false
        /// </summary>
        /// <param name="condition">Condition to evaluate for tracing.</param>
        /// <param name="message">Message to trace.</param>
        [Conditional("TRACE")]
        public static void WarningUnless(bool condition, string message)
        {
            WarningIf(!condition, message);
        }

        /// <summary>
        /// Prints the message if the condition is false. If the condition is true,
        /// the message is instead printed at the specified trace level.
        /// </summary>
        /// <param name="condition">Condition to evaluate for tracing.</param>
        /// <param name="bumpLevel">Trace message level.</param>
        /// <param name="message">Message to trace.</param>
        [Conditional("TRACE")]
        public static void WarningUnlessAlterTrace(bool condition, TraceLevel bumpLevel, string message)
        {
            if (condition)
            {
                WriteAtLevel(bumpLevel, message);
            }
            else
            {
                Warning(message);
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
        /// Trace a warning message based on a condition.
        /// </summary>
        /// <param name="condition">Condition for tracing.</param>
        /// <param name="format">Format of the trace message.</param>
        /// <param name="args">Arguments for the trace message.</param>
        [Conditional("TRACE")]
        public static void WarningIf(bool condition, string format, params object[] args)
        {
            if (condition)
            {
                Warning(format, args);
            }
        }

        /// <summary>
        /// Only prints the formatted message if the condition is false
        /// </summary>
        /// <param name="condition">Condition for tracing.</param>
        /// <param name="format">Format of trace message.</param>
        /// <param name="args">Arguments for the trace message.</param>
        [Conditional("TRACE")]
        public static void WarningUnless(bool condition, string format, params object[] args)
        {
            WarningIf(!condition, format, args);
        }

        /// <summary>
        /// Prints the message if the condition is false. If the condition is true,
        /// the message is instead printed at the specified trace level.
        /// </summary>
        /// <param name="condition">Condition for tracing.</param>
        /// <param name="bumpLevel">Level of trace message.</param>
        /// <param name="format">Format of the trace message.</param>
        /// <param name="args">Arguments for trace message.</param>
        [Conditional("TRACE")]
        public static void WarningUnlessAlterTrace(bool condition, TraceLevel bumpLevel, string format, params object[] args)
        {
            if (condition)
            {
                WriteAtLevel(bumpLevel, format, args);
            }
            else
            {
                Warning(format, args);
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
        /// Trace an informational message based on a condition.
        /// </summary>
        /// <param name="condition">Condition for tracing.</param>
        /// <param name="message">Trace message.</param>
        [Conditional("TRACE")]
        public static void InfoIf(bool condition, string message)
        {
            if (condition)
            {
                Info(message);
            }
        }

        /// <summary>
        /// Only prints the formatted message if the condition is false
        /// </summary>
        /// <param name="condition">Condition for tracing.</param>
        /// <param name="message">Trace message.</param>
        [Conditional("TRACE")]
        public static void InfoUnless(bool condition, string message)
        {
            InfoIf(!condition, message);
        }

        /// <summary>
        /// Prints the message if the condition is false. If the condition is true,
        /// the message is instead printed at the specified trace level.
        /// </summary>
        /// <param name="condition">Condition for tracing.</param>
        /// <param name="bumpLevel">Trace message level.</param>
        /// <param name="message">Trace message.</param>
        [Conditional("TRACE")]
        public static void InfoUnlessAlterTrace(bool condition, TraceLevel bumpLevel, string message)
        {
            if (condition)
            {
                WriteAtLevel(bumpLevel, message);
            }
            else
            {
                Info(message);
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
        /// Trace an informational message based on a condition.
        /// </summary>
        /// <param name="condition">Condition for tracing.</param>
        /// <param name="format">Format of the trace message.</param>
        /// <param name="args">Arguments for the trace format.</param>
        [Conditional("TRACE")]
        public static void InfoIf(bool condition, string format, params object[] args)
        {
            if (condition)
            {
                Info(format, args);
            }
        }

        /// <summary>
        /// Only prints the formatted message if the condition is false
        /// </summary>
        /// <param name="condition">Condition for tracing.</param>
        /// <param name="format">Trace message format.</param>
        /// <param name="args">Trace message format arguments.</param>
        [Conditional("TRACE")]
        public static void InfoUnless(bool condition, string format, params object[] args)
        {
            InfoIf(!condition, format, args);
        }

        /// <summary>
        /// Prints the message if the condition is false. If the condition is true,
        /// the message is instead printed at the specified trace level.
        /// </summary>
        /// <param name="condition">Condition for tracing.</param>
        /// <param name="bumpLevel">Trace message level.</param>
        /// <param name="format">Trace message format.</param>
        /// <param name="args">Trace message arguments.</param>
        [Conditional("TRACE")]
        public static void InfoUnlessAlterTrace(bool condition, TraceLevel bumpLevel, string format, params object[] args)
        {
            if (condition)
            {
                WriteAtLevel(bumpLevel, format, args);
            }
            else
            {
                Info(format, args);
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
        /// Trace a verbose message based on condition.
        /// </summary>
        /// <param name="condition">Condition for tracing.</param>
        /// <param name="message">Trace message.</param>
        [Conditional("TRACE")]
        public static void VerboseIf(bool condition, string message)
        {
            if (condition)
            {
                Verbose(message);
            }
        }

        /// <summary>
        /// Only prints the formatted message if the condition is false
        /// </summary>
        /// <param name="condition">Condition for tracing.</param>
        /// <param name="message">Trace message.</param>
        [Conditional("TRACE")]
        public static void VerboseUnless(bool condition, string message)
        {
            VerboseIf(!condition, message);
        }

        /// <summary>
        /// Prints the message if the condition is false. If the condition is true,
        /// the message is instead printed at the specified trace level.
        /// </summary>
        /// <param name="condition">Condition for tracing.</param>
        /// <param name="level">Trace message level.</param>
        /// <param name="message">Trace message.</param>
        [Conditional("TRACE")]
        public static void VerboseUnlessAlterTrace(bool condition, TraceLevel level, string message)
        {
            if (condition)
            {
                WriteAtLevel(level, message);
            }
            else
            {
                Verbose(message);
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
        /// Trace a verbose message based on a condition.
        /// </summary>
        /// <param name="condition">Condition for tracing.</param>
        /// <param name="format">Message format.</param>
        /// <param name="args">Arguments for trace message.</param>
        [Conditional("TRACE")]
        public static void VerboseIf(bool condition, string format, params object[] args)
        {
            if (condition)
            {
                Verbose(format, args);
            }
        }

        /// <summary>
        /// Only prints the formatted message if the condition is false
        /// </summary>
        /// <param name="condition">Condition for tracing.</param>
        /// <param name="format">Format for the trace message.</param>
        /// <param name="args">Trace message arguments.</param>
        [Conditional("TRACE")]
        public static void VerboseUnless(bool condition, string format, params object[] args)
        {
            VerboseIf(!condition, format, args);
        }

        /// <summary>
        /// Prints the message if the condition is false. If the condition is true,
        /// the message is instead printed at the specified trace level.
        /// </summary>
        /// <param name="condition">Condition for tracing.</param>
        /// <param name="level">Trace message level.</param>
        /// <param name="format">Format of the trace message.</param>
        /// <param name="args">Arguments for the trace message format.</param>
        [Conditional("TRACE")]
        public static void VerboseUnlessAlterTrace(bool condition, TraceLevel level, string format, params object[] args)
        {
            if (condition)
            {
                WriteAtLevel(level, format, args);
            }
            else
            {
                Verbose(format, args);
            }
        }

        /// <summary>
        /// Setup trace listeners. It should be called when setting trace listener for child domain.
        /// </summary>
        /// <param name="listener">New listener.</param>
        internal static void SetupRemoteListeners(TraceListener listener)
        {
            lock (isInitializationLock)
            {
                // Add new listeners.
                if (listener != null)
                {
                    Trace.Listeners.Add(listener);
                    EqtTrace.listener = listener;
                }

                isInitialized = true;
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
                if (logFileName == null)
                {
                    logFileName = Path.Combine(
                        logsDirectory,
                        Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().MainModule.FileName) + ".TpTrace.log");
                }

                traceFileSize = defaultTraceFileSize;
                listener = new RollingFileTraceListener(logFileName, ListenerName, traceFileSize);
                Trace.Listeners.Add(listener);

                // Set the auto-flush flag, so that log entries are written immediately. This is done so that if the
                // process exits or is killed, we will have as much log information as possible in the log.
                Trace.AutoFlush = true;

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
                return Resources.Utility_ProcessNameWhenCannotGetIt;
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

        private static void WriteAtLevel(TraceLevel level, string message)
        {
            switch (level)
            {
                case TraceLevel.Off:
                    return;
                case TraceLevel.Error:
                    Error(message);
                    break;
                case TraceLevel.Warning:
                    Warning(message);
                    break;
                case TraceLevel.Info:
                    Info(message);
                    break;
                case TraceLevel.Verbose:
                    Verbose(message);
                    break;
                default:
                    Debug.Fail("We should never get here!");
                    break;
            }
        }

        private static void WriteAtLevel(TraceLevel level, string format, params object[] args)
        {
            Debug.Assert(format != null, "format != null");
            WriteAtLevel(level, string.Format(CultureInfo.InvariantCulture, format, args));
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

            string entryType = null;
            switch (level)
            {
                case TraceLevel.Error:
                    entryType = "E";
                    break;

                case TraceLevel.Warning:
                    entryType = "W";
                    break;

                case TraceLevel.Info:
                    entryType = "I";
                    break;

                case TraceLevel.Verbose:
                    entryType = "V";
                    break;

                default:
                    Debug.Fail("Should never get here. Unexpected TraceLevel: " + level);
                    break;
            }

            // The format below is a CSV so that Excel could be used easily to
            // view/filter the logs.
            var log = string.Format(
                CultureInfo.InvariantCulture,
                "{0}, {1}, {2}, {3:yyyy}/{3:MM}/{3:dd}, {3:HH}:{3:mm}:{3:ss}.{3:fff}, {6}, {4}, {5}",
                entryType,
                ProcessId,
                Thread.CurrentThread.ManagedThreadId,
                DateTime.Now,
                ProcessName,
                message,
                Stopwatch.GetTimestamp());

            try
            {
                if (listener != null)
                {
                    listener.WriteLine(log);
                    listener.Flush();
                }
                else
                {
                    Trace.WriteLine(log);
                }
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