// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.TestHost
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    /// <summary>
    /// The program.
    /// </summary>
    public class Program
    {
        private const string TestSourceArgumentString = "--testsourcepath";

        /// <summary>
        /// The main.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        public static void Main(string[] args)
        {
            try
            {
                TestPlatformEventSource.Instance.TestHostStart();
                Run(args);
            }
            catch (Exception ex)
            {
                EqtTrace.Error("TestHost: Error occured during initialization of TestHost : {0}", ex);

                // Throw exception so that vstest.console get the exception message.
                throw;
            }
            finally
            {
                TestPlatformEventSource.Instance.TestHostStop();
            }
        }

        // In UWP(App models) Run will act as entry point from Application end, so making this method public
        public static void Run(string[] args)
        {
            WaitForDebuggerIfEnabled();
            var argsDictionary = CommandLineArgumentsHelper.GetArgumentsDictionary(args);
            
            // Invoke the engine with arguments
            GetEngineInvoker(argsDictionary).Invoke(argsDictionary);
        }

        private static IEngineInvoker GetEngineInvoker(IDictionary<string, string> argsDictionary)
        {
            IEngineInvoker invoker = null;
#if NET451
            // If Args contains test source argument, invoker Engine in new appdomain 
            string testSourcePath;
            if (argsDictionary.TryGetValue(TestSourceArgumentString, out testSourcePath) && !string.IsNullOrWhiteSpace(testSourcePath))
            {
                // remove the test source arg from dictionary
                argsDictionary.Remove(TestSourceArgumentString);

                // Only DLLs and EXEs can have app.configs or ".exe.config" or ".dll.config"
                if (System.IO.File.Exists(testSourcePath) &&
                        (testSourcePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                        || testSourcePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)))
                {
                    invoker = new AppDomainEngineInvoker<DefaultEngineInvoker>(testSourcePath);
                }
            }
#endif
            return invoker ?? new DefaultEngineInvoker();
        }

        private static void WaitForDebuggerIfEnabled()
        {
            IProcessHelper processHelper = new ProcessHelper();
            var debugEnabled = Environment.GetEnvironmentVariable("VSTEST_HOST_DEBUG");
            if (!string.IsNullOrEmpty(debugEnabled) && debugEnabled.Equals("1", StringComparison.Ordinal))
            {
                ConsoleOutput.Instance.WriteLine("Waiting for debugger attach...", OutputLevel.Information);

                var currentProcessId = processHelper.GetCurrentProcessId();
                var currentProcessName = processHelper.GetProcessName(currentProcessId);
                ConsoleOutput.Instance.WriteLine(
                    string.Format("Process Id: {0}, Name: {1}", currentProcessId, currentProcessName),
                    OutputLevel.Information);

                while (!Debugger.IsAttached)
                {
                    System.Threading.Tasks.Task.Delay(1000).Wait();
                }

                Debugger.Break();
            }
        }
    }
}
