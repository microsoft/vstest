// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.TestHost
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Newtonsoft.Json;
    using System.Linq;
    using System.Reflection.Metadata;

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
                EqtTrace.Error("TestHost: Error occurred during initialization of TestHost : {0}", ex);

                // Throw exception so that vstest.console get the exception message.
                throw;
            }
            finally
            {
                TestPlatformEventSource.Instance.TestHostStop();
                EqtTrace.Info("Testhost process exiting.");
            }
        }

        // In UWP(App models) Run will act as entry point from Application end, so making this method public
        public static void Run(string[] args)
        {
            WaitForDebuggerIfEnabled();
            SetCultureSpecifiedByUser();
            var argsDictionary = CommandLineArgumentsHelper.GetArgumentsDictionary(args);

#if NET6_0_OR_GREATER
            LoadModules();
#endif

            // Invoke the engine with arguments
            GetEngineInvoker(argsDictionary).Invoke(argsDictionary);
        }

        private static IEngineInvoker GetEngineInvoker(IDictionary<string, string> argsDictionary)
        {
            IEngineInvoker invoker = null;
#if NETFRAMEWORK
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
            // Check if native debugging is enabled and OS is windows.
            var nativeDebugEnabled = Environment.GetEnvironmentVariable("VSTEST_HOST_NATIVE_DEBUG");

            if (!string.IsNullOrEmpty(nativeDebugEnabled) && nativeDebugEnabled.Equals("1", StringComparison.Ordinal)
                && new PlatformEnvironment().OperatingSystem.Equals(PlatformOperatingSystem.Windows))
            {
                while (!IsDebuggerPresent())
                {
                    Task.Delay(1000).Wait();
                }

                DebugBreak();
            }
            // else check for host debugging enabled
            else
            {
                var debugEnabled = Environment.GetEnvironmentVariable("VSTEST_HOST_DEBUG");

                if (!string.IsNullOrEmpty(debugEnabled) && debugEnabled.Equals("1", StringComparison.Ordinal))
                {
                    while (!Debugger.IsAttached)
                    {
                        Task.Delay(1000).Wait();
                    }

                    Debugger.Break();
                }
            }
        }

        private static void SetCultureSpecifiedByUser()
        {
            var userCultureSpecified = Environment.GetEnvironmentVariable(CoreUtilities.Constants.DotNetUserSpecifiedCulture);
            if (!string.IsNullOrWhiteSpace(userCultureSpecified))
            {
                try
                {
                    CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(userCultureSpecified);
                }
                catch (Exception)
                {
                    ConsoleOutput.Instance.WriteLine(string.Format("Invalid Culture Info: {0}", userCultureSpecified), OutputLevel.Information);
                }
            }
        }

#if NET6_0_OR_GREATER   
        private static Dictionary<Guid, List<Update>> Modules;
        private static void LoadModules()
        {
            //Debugger.Launch();
            var hotReloadModules = File.ReadAllText(@"C:\repos\hotReload.txt");
            Modules = JsonConvert.DeserializeObject<Dictionary<Guid, List<Update>>>(hotReloadModules);

            File.WriteAllText(@"C:\repos\tempFile.txt", $"loaded modules");

            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyLoad += new AssemblyLoadEventHandler(OnAssemblyLoad);
        }

        private static void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            // ApplyUpdate doesn't work with debuggers attached
            var mvid = args.LoadedAssembly.Modules.FirstOrDefault()?.ModuleVersionId;

            if (mvid != null && Modules.ContainsKey((Guid)mvid))
            {
                foreach (var update in Modules[(Guid)mvid])
                {
                    try
                    {
                        System.Reflection.Metadata.AssemblyExtensions.ApplyUpdate(args.LoadedAssembly, update.MetadataDelta, update.ILDelta, update.PdbDelta);
                        File.AppendAllText(@"C:\repos\tempFile.txt", $"added {args.LoadedAssembly.FullName}");
                    }
                    catch (Exception e)
                    {
                        File.AppendAllText(@"C:\repos\tempFile.txt", $"Module: {args.LoadedAssembly.FullName}\n{e}");
                    }
                }
            }
        }

        [Serializable]
        private struct Update
        {
            public Update(byte[] iLDelta, byte[] metadataDelta, byte[] pdbDelta, int[] updatedMethods)
            {
                this.ILDelta = iLDelta;
                this.MetadataDelta = metadataDelta;
                this.PdbDelta = pdbDelta;
                this.UpdatedMethods = updatedMethods;
            }

            public byte[] ILDelta;
            public byte[] MetadataDelta;
            public byte[] PdbDelta;
            public int[] UpdatedMethods;
        }
#endif

        // Native APIs for enabling native debugging.
        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsDebuggerPresent();

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        internal static extern void DebugBreak();
    }
}
