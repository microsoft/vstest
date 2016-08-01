// Copyright (c) Microsoft. All rights reserved.

namespace TestPlatform.TestUtilities
{
    using System.Diagnostics;
    using System.IO;

    /// <summary>
    /// The execution manager.
    /// </summary>
    public static class ExecutionManager
    {
#if DEBUG
        private const string RelativeVsTestPath = @"artifacts\src\Microsoft.TestPlatform.VSIXCreator\bin\Debug\net461\win7-x64";
#else
        private const string RelativeVsTestPath = @"artifacts\src\Microsoft.TestPlatform.VSIXCreator\bin\Release\net461\win7-x64";
#endif

        /// <summary>
        /// Gets the executing location.
        /// </summary>
        public static string ExecutingLocation
        {
            get
            {
                return Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            }
        }

        /// <summary>
        /// The execute.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        /// <param name="stdOut">
        /// The standard out.
        /// </param>
        /// <param name="stdError">
        /// The standard error.
        /// </param>
        public static void Execute(string args, out string stdOut, out string stdError)
        {
            stdError = string.Empty;
            stdOut = string.Empty;

            using (Process vstestconsole = new Process())
            {
                vstestconsole.StartInfo.FileName = GetVstestConsolePath();
                vstestconsole.StartInfo.Arguments = args;
                vstestconsole.StartInfo.UseShellExecute = false;
                vstestconsole.StartInfo.WorkingDirectory = GetBaseDirectory();
                vstestconsole.StartInfo.RedirectStandardError = true;
                vstestconsole.StartInfo.RedirectStandardOutput = true;
                vstestconsole.StartInfo.CreateNoWindow = true;
                vstestconsole.Start();

                stdError = vstestconsole.StandardError.ReadToEnd();
                stdOut = vstestconsole.StandardOutput.ReadToEnd();

                vstestconsole.WaitForExit();
            }
        }

        /// <summary>
        /// The get VS test console path.
        /// </summary>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public static string GetVstestConsolePath()
        {            
            var path = Path.Combine(GetBaseDirectory(), "vstest.console.exe");
            return path;
        }

        /// <summary>
        /// The get test adapter path.
        /// </summary>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public static string GetTestAdapterPath()
        {
            var path = Path.Combine(GetBaseDirectory(), "Adapter");
            return path;
        }

        public static string GetBaseDirectory()
        {
            var baseDirectory = string.Empty;
            
            var directoryInfo = new DirectoryInfo(Directory.GetCurrentDirectory());
            baseDirectory = Path.Combine(directoryInfo.Parent?.Parent.FullName, RelativeVsTestPath);

            return baseDirectory;
        }
    }
}
