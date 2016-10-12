// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
#if NET46
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// A class used to expose EqtTrace functionality across AppDomains.
    /// </summary>
    public sealed class RemoteEqtTrace : MarshalByRefObject
    {
        /// <summary>
        /// Gets or sets the trace level.
        /// </summary>
        public TraceLevel TraceLevel
        {
            get
            {
                return EqtTrace.TraceLevel;
            }

            set
            {
                EqtTrace.TraceLevel = value;
            }
        }

        /// <summary>
        /// Register listeners from parent domain in current domain.
        /// </summary>
        /// <param name="listener">Trace listener instance.</param>
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Used in remote objects.")]
        internal void SetupRemoteListeners(TraceListener listener)
        {
            EqtTrace.SetupRemoteListeners(listener);
        }
    }
#endif
}
