// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using NuGet.Frameworks;

    internal class CrashDumperFactory : ICrashDumperFactory
    {
        public ICrashDumper Create(string targetFramework)
        {
            if (targetFramework is null)
            {
                throw new ArgumentNullException(nameof(targetFramework));
            }

            EqtTrace.Info($"CrashDumperFactory: Creating dumper for {RuntimeInformation.OSDescription} with target framework {targetFramework}.");

            var tfm = NuGetFramework.Parse(targetFramework);

            if (tfm == null || tfm.IsUnsupported)
            {
                EqtTrace.Error($"CrashDumperFactory: Could not parse target framework {targetFramework}, to a supported framework version.");
                throw new NotSupportedException($"Could not parse target framework {targetFramework}, to a supported framework version.");
            }

            var isNet50OrNewer = tfm.Framework == ".NETCoreApp" && tfm.Version >= Version.Parse("5.0.0.0");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (!isNet50OrNewer)
                {
                    EqtTrace.Info($"CrashDumperFactory: This is Windows on {targetFramework} which is not net5.0 or newer, returning ProcDumpCrashDumper that uses ProcDump utility.");
                    return new ProcDumpCrashDumper();
                }

                EqtTrace.Info($"CrashDumperFactory: This is Windows on {targetFramework}, returning the .NETClient dumper which uses env variables to collect crashdumps of testhost and any child process.");
                return new NetClientCrashDumper();
            }

            if (isNet50OrNewer)
            {
                EqtTrace.Info($"CrashDumperFactory: This is {RuntimeInformation.OSDescription} on {targetFramework} .NETClient dumper which uses env variables to collect crashdumps of testhost and any child process.");
                return new NetClientCrashDumper();
            }

            throw new PlatformNotSupportedException($"Unsupported operating system: {RuntimeInformation.OSDescription}, and framework: {targetFramework}.");
        }
    }
}
