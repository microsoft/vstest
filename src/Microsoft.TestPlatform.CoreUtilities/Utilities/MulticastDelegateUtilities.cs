// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Utilities
{
    using System;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.CoreUtilities;
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
        public static void SafeInvoke(this Delegate delegates, object sender, EventArgs args, string traceDisplayName)
        {
            if (args == null)
            {
                throw new ArgumentNullException(Resources.CannotBeNullOrEmpty, "args");
            }

            if (string.IsNullOrWhiteSpace(traceDisplayName))
            {
                throw new ArgumentException(Resources.CannotBeNullOrEmpty, traceDisplayName);
            }

            if (delegates != null)
            {
                foreach (Delegate handler in delegates.GetInvocationList())
                {
                    try
                    {
                        EqtTrace.Verbose("MulticastDelegateUtilities.SafeInvoke: {0}", handler);
                        handler.DynamicInvoke(sender, args);
                    }
                    catch (TargetInvocationException e)
                    {
                        if (EqtTrace.IsErrorEnabled)
                        {
                            EqtTrace.Error(
                                "{0}: Exception occurred while calling handler of type {1} for {2}: {3}",
                                traceDisplayName,
                                handler.Target.GetType().FullName,
                                args.GetType().Name,
                                e);
                        }
                    }
                }
            }
        }
    }
}
