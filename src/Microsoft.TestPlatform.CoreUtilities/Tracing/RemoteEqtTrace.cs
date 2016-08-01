// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
#if NET46
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    
    /// <summary>
    /// A class used to expose EqtTrace functionality across AppDomains.
    /// <see cref="EqtTrace.GetRemoteEqtTrace"/>
    /// </summary>
    public sealed class RemoteEqtTrace : MarshalByRefObject
    {
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
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Used in remote objects.")]
        internal void SetupRemoteListeners(TraceListener listener)
        {
            EqtTrace.SetupRemoteListeners(listener);
        }
    }
#endif
}
