// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    internal class HangDumperFactory : IHangDumperFactory
    {
        public Action<string> LogWarning { get; set; }

        public IHangDumper Create(string targetFramework)
        {
            if (targetFramework is null)
            {
                throw new ArgumentNullException(nameof(targetFramework));
            }

            EqtTrace.Info($"HangDumperFactory: Creating dumper for {RuntimeInformation.OSDescription} with target framework {targetFramework}.");

            var tfm = Framework.FromString(targetFramework);

            if (tfm == null)
            {
                EqtTrace.Error($"HangDumperFactory: Could not parse target framework {targetFramework}, to a supported framework version.");
                throw new NotSupportedException($"Could not parse target framework {targetFramework}, to a supported framework version.");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                EqtTrace.Info($"HangDumperFactory: This is Windows, returning the default WindowsHangDumper that P/Invokes MiniDumpWriteDump.");
                return new WindowsHangDumper(this.LogWarning);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var isLessThan31 = tfm.FrameworkName == ".NETCoreApp" && Version.Parse(tfm.Version) < Version.Parse("3.1.0.0");
                if (isLessThan31)
                {
                    EqtTrace.Info($"HangDumperFactory: This is Linux on netcoreapp2.1, returning SigtrapDumper.");

                    return new SigtrapDumper();
                }

                EqtTrace.Info($"HangDumperFactory: This is Linux netcoreapp3.1 or newer, returning the standard NETClient library dumper.");
                return new NetClientHangDumper();
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var isLessThan50 = tfm.FrameworkName == ".NETCoreApp" && Version.Parse(tfm.Version) < Version.Parse("5.0.0.0");
                if (isLessThan50)
                {
                    EqtTrace.Info($"HangDumperFactory: This is OSX on {targetFramework}, This combination of OS and framework is not supported.");

                    throw new PlatformNotSupportedException($"Unsupported target framework {targetFramework} on OS {RuntimeInformation.OSDescription}");
                }

                EqtTrace.Info($"HangDumperFactory: This is OSX on net5.0 or newer, returning the standard NETClient library dumper.");
                return new NetClientHangDumper();
            }

            throw new PlatformNotSupportedException($"Unsupported operating system: {RuntimeInformation.OSDescription}");
        }
    }
}
