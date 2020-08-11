// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    internal class CrashDumperFactory : ICrashDumperFactory
    {
        public ICrashDumper Create(string targetFramework)
        {
            EqtTrace.Info($"CrashDumperFactory: Creating dumper for {RuntimeInformation.OSDescription} with target framework {targetFramework}.");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                EqtTrace.Info($"CrashDumperFactory: This is Windows, returning ProcDumpCrashDumper that uses ProcDump utility.");
                return new ProcDumpCrashDumper();

                // enable this once crashdump can trigger itself on exceptions that originate from task, then we can avoid using procdump
                // if (!string.IsNullOrWhiteSpace(targetFramework) && !targetFramework.Contains("v5.0"))
                // {
                //     EqtTrace.Info($"CrashDumperFactory: This is Windows on {targetFramework} which is not net5.0 or newer, returning ProcDumpCrashDumper that uses ProcDump utility.");
                //     return new ProcDumpCrashDumper();
                // }

                // EqtTrace.Info($"CrashDumperFactory: This is Windows on {targetFramework}, returning the .NETClient dumper which uses env variables to collect crashdumps of testhost and any child process.");
                // return new NetClientCrashDumper();
            }

            if (!string.IsNullOrWhiteSpace(targetFramework) && !targetFramework.Contains("v5.0"))
            {
                EqtTrace.Info($"CrashDumperFactory: This is {RuntimeInformation.OSDescription} on {targetFramework} .NETClient dumper which uses env variables to collect crashdumps of testhost and any child process.");
                return new NetClientCrashDumper();
            }

            throw new PlatformNotSupportedException($"Unsupported operating system: {RuntimeInformation.OSDescription}, and framework: {targetFramework}.");
        }
    }
}
