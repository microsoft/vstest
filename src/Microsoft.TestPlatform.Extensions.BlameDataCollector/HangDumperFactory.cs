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

            // this is not supported yet
            // if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            // {

            // if (frameworkVersion != default && frameworkVersion <= new Version("5.0"))
            // {
            //    return new SigtrapDumper();
            // }

            // EqtTrace.Info($"HangDumperFactory: This is OSX on netcoreapp3.1 or newer, returning the standard NETClient library dumper.");
            // return new NetClientDumper();
            // }
            throw new PlatformNotSupportedException($"Unsupported operating system: {RuntimeInformation.OSDescription}");
        }
    }
}
