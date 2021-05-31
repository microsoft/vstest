// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETFRAMEWORK

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System;
    using System.Diagnostics;

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
        /// <summary>
        /// Setup remote trace listener in the child domain.
        /// If calling domain, doesn't have tracing enabled nothing is done.
        /// </summary>
        /// <param name="childDomain">Child <c>AppDomain</c>.</param>
        public void SetupRemoteEqtTraceListeners(AppDomain childDomain)
        {
            Debug.Assert(childDomain != null, "domain");
            if (childDomain != null)
            {
                RemoteEqtTrace remoteEqtTrace = (RemoteEqtTrace)childDomain.CreateInstanceFromAndUnwrap(
                    typeof(RemoteEqtTrace).Assembly.Location,
                    typeof(RemoteEqtTrace).FullName);

                if (!Enum.Equals(TraceLevel, TraceLevel.Off))
                {
                    remoteEqtTrace.TraceLevel = TraceLevel;

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
                else
                {
                    this.DoNotInitialize = true;
                }
            }
        }

        /// <inheritdoc/>
        public void SetupListener(TraceListener listener)
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
    }
}

#endif
