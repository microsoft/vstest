// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.Execution;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    /// <summary>
    /// Main entry point for the command line runner.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Main entry point. Hands off execution to the executor class.
        /// </summary>
        /// <param name="args">Arguments provided on the command line.</param>
        /// <returns>0 if everything was successful and 1 otherwise.</returns>
        public static int Main(string[] args)
        {
            DebuggerBreakpoint.WaitForDebugger("VSTEST_RUNNER_DEBUG");
            ForwardDotnetRootEnvVarToNewVersion();
            UILanguageOverride.SetCultureSpecifiedByUser();
            return new Executor(ConsoleOutput.Instance).Execute(args);
        }

        /// <summary>
        /// Forwarding of DOTNET_ROOT/DOTNET_ROOT(x86) env vars populted by SDK, this is needed to allow to --arch feature to work
        /// as expected. If we use old SDK and new TP it won't work without env vars forwarding.
        /// </summary>
        private static void ForwardDotnetRootEnvVarToNewVersion()
        {
            var switchVars = Environment.GetEnvironmentVariable("VSTEST_TMP_SWITCH_DOTNETROOTS_ENVVARS");
            if (switchVars != null && int.Parse(switchVars) == 1)
            {
                var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
                if (dotnetRoot != null)
                {
# if DEBUG
                    Console.WriteLine($"Forwarding DOTNET_ROOT to VSTEST_WINAPPHOST_DOTNET_ROOT '{dotnetRoot}'");
#endif
                    Environment.SetEnvironmentVariable("DOTNET_ROOT", null);
                    Environment.SetEnvironmentVariable("VSTEST_WINAPPHOST_DOTNET_ROOT", dotnetRoot);
#if DEBUG
                    Console.WriteLine($"Current DOTNET_ROOT '{Environment.GetEnvironmentVariable("DOTNET_ROOT")}'");
#endif
                }

                var dotnetRootX86 = Environment.GetEnvironmentVariable("DOTNET_ROOT(x86)");
                if(dotnetRootX86 != null)
                {
#if DEBUG
                    Console.WriteLine($"Forwarding DOTNET_ROOT(x86) to VSTEST_WINAPPHOST_DOTNET_ROOT(x86) '{dotnetRootX86}'");
#endif
                    Environment.SetEnvironmentVariable("DOTNET_ROOT(x86)", null);
                    Environment.SetEnvironmentVariable("VSTEST_WINAPPHOST_DOTNET_ROOT(x86)", dotnetRootX86);
#if DEBUG
                    Console.WriteLine($"Current DOTNET_ROOT(x86) '{Environment.GetEnvironmentVariable("DOTNET_ROOT(x86)")}'");
#endif
                }
            }
        }
    }
}
