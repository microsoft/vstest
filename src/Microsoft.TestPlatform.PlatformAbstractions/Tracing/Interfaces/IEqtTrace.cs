using System;
using System.Diagnostics;

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    //
    // Summary:
    //     Specifies what messages to output for the System.Diagnostics.Debug, System.Diagnostics.Trace
    //     and System.Diagnostics.TraceSwitch classes.
    public enum CustomTraceLevel
    {
        //
        // Summary:
        //     Output no tracing and debugging messages.
        Off = 0,
        //
        // Summary:
        //     Output error-handling messages.
        Error = 1,
        //
        // Summary:
        //     Output warnings and error-handling messages.
        Warning = 2,
        //
        // Summary:
        //     Output informational messages, warnings, and error-handling messages.
        Info = 3,
        //
        // Summary:
        //     Output all debugging and tracing messages.
        Verbose = 4
    }


    public interface IEqtTrace
    {
        void WriteLine(CustomTraceLevel level, string message);

        void InitializeVerboseTrace(string customLogFile);

        bool ShouldTrace(CustomTraceLevel traceLevel);

        string GetLogFile();

        void SetTraceLevel(CustomTraceLevel value);

#if NET46
        void SetupRemoteEqtTraceListeners(AppDomain childDomain);

        void SetupListener(TraceListener listener);
#endif

    }
}