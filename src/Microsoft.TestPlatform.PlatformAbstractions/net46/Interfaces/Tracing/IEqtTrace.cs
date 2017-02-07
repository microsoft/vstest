using System;
using System.Diagnostics;

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    
    public partial interface IPlatformEqtTrace
    {   
        /// <summary>
        /// Setup remote trace listener in the child domain.
        /// If calling domain, doesn't have tracing enabled nothing is done.
        /// </summary>
        /// <param name="childDomain">Child <c>AppDomain</c>.</param>
        void SetupRemoteEqtTraceListeners(AppDomain childDomain);

        /// <summary>
        /// Setup a custom trace listener instead of default trace listener created by test platform.
        /// This is needed by DTA Agent where it needs to listen test platform traces but doesn't use test platform listener.
        /// </summary>
        /// <param name="listener">
        /// The listener.
        /// </param>
        void SetupListener(TraceListener listener);

    }
}