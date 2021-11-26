using System;
using System.IO;
using System.Diagnostics;

namespace Nohwnd.AttachVS
{
    /// <summary>
    /// Attaches the current, or a selected process to a Visual Studio instance that is running as a parent process.
    /// </summary>
    public static class AttachHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static bool AttachVS(int? vsPid)
        {
            if (Debugger.IsAttached)
                return true;

            return AttachVS(Process.GetCurrentProcess(), vsPid);
        }

        public static bool AttachVS(this Process process, int? vsPid)
        {
            if (Debugger.IsAttached)
                return true;

            // The way we attach VS is not compatible with .NET Core 2.1 and .NET Core 3.1, but works in .NET Framework and .NET.
            // So for Core we need to start an external utility, that is built against .NET Framework and attach us from there.
#if !NETFRAMEWORK
            var isNetCore = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.StartsWith(".NET Core");
            if (isNetCore)
            {            
                var attachVsProcess = Process.Start(Path.Combine(AppContext.BaseDirectory, "AttachVS.exe"), $"{process.Id} {vsPid}");
                attachVsProcess.WaitForExit();

                return attachVsProcess.ExitCode == 0;
            }
#endif
            return DebuggerUtility.AttachVSToProcess(process.Id, vsPid);
        }
    }
}
