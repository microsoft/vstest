// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Utilities
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Resources;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    /// <summary>
    /// Utility methods for MulticastDelegates.
    /// </summary>
    public static class MulticastDelegateUtilities
    {
        /// <summary>
        /// Invokes each of the subscribers of the event and handles exceptions which are thrown
        /// ensuring that each handler is invoked even if one throws.
        /// </summary>
        /// <param name="delegates">Event handler to invoke.</param>
        /// <param name="sender">Sender to use when raising the event.</param>
        /// <param name="args">Arguments to provide.</param>
        /// <param name="traceDisplayName">Name to use when tracing out errors.</param>
        // Using [CallerMemberName] for the traceDisplayName is a possibility, but in few places we call through other
        // methods until we reach here. And it would change the public API.
        public static void SafeInvoke(this Delegate delegates, object sender, EventArgs args, string traceDisplayName)
        {
            if (args == null)
            {
                throw new ArgumentNullException(Resources.CannotBeNullOrEmpty, nameof(args));
            }

            if (string.IsNullOrWhiteSpace(traceDisplayName))
            {
                throw new ArgumentException(Resources.CannotBeNullOrEmpty, nameof(traceDisplayName));
            }

            if (delegates != null)
            {
                var invocationList = delegates.GetInvocationList();
                var i = 0;
                foreach (Delegate handler in invocationList)
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();

                    try
                    {
                        handler.DynamicInvoke(sender, args);
                        if (EqtTrace.IsVerboseEnabled)
                        {
                            stopwatch = Stopwatch.StartNew();
                            EqtTrace.Verbose("MulticastDelegateUtilities.SafeInvoke: {0}: Invoking callback {1}/{2} for {3}.{4}, took {5} ms.",
                                    traceDisplayName,
                                    ++i,
                                    invocationList.Length,
                                    handler.Target ?? "static",
                                    handler.Method.Name,
                                    stopwatch.ElapsedMilliseconds);
                        }
                    }
                    catch (TargetInvocationException exception)
                    {
                        if (EqtTrace.IsErrorEnabled)
                        {
                            EqtTrace.Error(
                                "MulticastDelegateUtilities.SafeInvoke: {0}: Invoking callback {1}/{2} for {3}.{4}, failed after {5} ms with: {6}",
                                ++i,
                                invocationList.Length,
                                handler.Target ?? "static",
                                handler.Method.Name,
                                traceDisplayName,
                                stopwatch.ElapsedMilliseconds,
                                exception);
                        }
                    }
                }
            }
            else
            {
                EqtTrace.Verbose("MulticastDelegateUtilities.SafeInvoke: {0}: Invoking callbacks was skipped because there are no subscribers.", traceDisplayName);
            }
        }
    }
}
