// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    internal class HangDumperFactory : IHangDumperFactory
    {
        public IHangDumper Create(string targetFramework)
        {
            EqtTrace.Info($"HangDumperFactory: Creating dumper for {RuntimeInformation.OSDescription} with target framework {targetFramework}.");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                EqtTrace.Info($"HangDumperFactory: This is Windows, returning the default WindowsHangDumper that P/Invokes MiniDumpWriteDump.");
                return new WindowsHangDumper();
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (!string.IsNullOrWhiteSpace(targetFramework) && targetFramework.Contains("v2.1"))
                {
                    EqtTrace.Info($"HangDumperFactory: This is Linux on netcoreapp2.1, returning SigtrapDumper.");

                    return new SigtrapDumper();
                }

                EqtTrace.Info($"HangDumperFactory: This is Linux netcoreapp3.1 or newer, returning the standard NETClient library dumper.");
                return new NetClientDumper();
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                if (!string.IsNullOrWhiteSpace(targetFramework) && !targetFramework.Contains("v5.0"))
                {
                    EqtTrace.Info($"HangDumperFactory: This is OSX on {targetFramework}, This combination of OS and framework is not supported.");

                    throw new PlatformNotSupportedException($"Unsupported target framework {targetFramework} on OS {RuntimeInformation.OSDescription}");
                }

                EqtTrace.Info($"HangDumperFactory: This is OSX on net5.0 or newer, returning the standard NETClient library dumper.");

                // enabling dumps on MacOS needs to be done explicitly https://github.com/dotnet/runtime/pull/40105
                Environment.SetEnvironmentVariable("COMPlus_DbgEnableElfDumpOnMacOS", "1");
                return new NetClientDumper();
            }

            throw new PlatformNotSupportedException($"Unsupported operating system: {RuntimeInformation.OSDescription}");
        }
    }
}
