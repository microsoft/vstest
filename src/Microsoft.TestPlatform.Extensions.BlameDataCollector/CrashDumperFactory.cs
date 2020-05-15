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
            }

            throw new PlatformNotSupportedException($"Unsupported operating system: {RuntimeInformation.OSDescription}");
        }
    }
}
