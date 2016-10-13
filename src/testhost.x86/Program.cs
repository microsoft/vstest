// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.TestHost
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using System.IO;

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
                var debugEnabled = Environment.GetEnvironmentVariable("VSTEST_HOST_DEBUG");
                if (!string.IsNullOrEmpty(debugEnabled) && debugEnabled.Equals("1", StringComparison.Ordinal))
                {
                    ConsoleOutput.Instance.WriteLine("Waiting for debugger attach...", OutputLevel.Information);

                    var currentProcess = Process.GetCurrentProcess();
                    ConsoleOutput.Instance.WriteLine(
                        string.Format("Process Id: {0}, Name: {1}", currentProcess.Id, currentProcess.ProcessName),
                        OutputLevel.Information);

                    while (!Debugger.IsAttached)
                    {
                        System.Threading.Thread.Sleep(1000);
                    }

                    Debugger.Break();
                }

                RunArgs(args);
            }
            catch (Exception ex)
            {
                EqtTrace.Error("TestHost: Error occured during initialization of TestHost : {0}", ex);
            }
        }

        private static void RunArgs(string[] args)
        {
            var argsDictionary = ParseArgsIntoDictionary(args);
            IEngineInvoker invoker = null;

#if NET46
            // If Args contains test source argument, invoker Engine in new appdomain 
            if (argsDictionary.ContainsKey(TestSourceArgumentString))
            {
                string testSourcePath = argsDictionary[TestSourceArgumentString];

                // remove the test source arg from dictionary
                argsDictionary.Remove(TestSourceArgumentString);

                // Only DLL and EXEs can have app.configs or ".exe.config" or ".dll.config"
                if (File.Exists(testSourcePath) && (testSourcePath.EndsWith(".dll") || testSourcePath.EndsWith(".exe")))
                {
                    invoker = new AppDomainEngineInvoker<DefaultEngineInvoker>(testSourcePath);
                }
            }
#endif
            invoker = invoker ?? new DefaultEngineInvoker();
            invoker.Invoke(argsDictionary);
        }

        /// <summary>
        /// The get args dictionary.
        /// </summary>
        /// <param name="args">
        /// args Ex: { "--port", "12312", "--parentprocessid", "2312" }
        /// </param>
        /// <returns>
        /// The <see cref="IDictionary"/>.
        /// </returns>
        private static IDictionary<string, string> ParseArgsIntoDictionary(string[] args)
        {
            IDictionary<string, string> argsDictionary = new Dictionary<string, string>();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("-"))
                {
                    if (i < args.Length - 1 && !args[i + 1].StartsWith("-"))
                    {
                        argsDictionary.Add(args[i], args[i + 1]);
                        i++;
                    }
                    else
                    {
                        argsDictionary.Add(args[i], null);
                    }
                }
            }

            return argsDictionary;
        }
    }
}
