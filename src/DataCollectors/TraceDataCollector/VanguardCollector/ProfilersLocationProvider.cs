// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TraceCollector
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using Coverage;
    using Coverage.Interfaces;
    using TraceDataCollector.Resources;

    internal class ProfilersLocationProvider : IProfilersLocationProvider
    {
        private const string ClrIeX86InstallDirVariable = "CLRIEX86InstallDir";
        private const string ClrIeX64InstallDirVariable = "CLRIEX64InstallDir";
        private const string ClrIeX86FileName = "MicrosoftInstrumentationEngine_x86.dll";
        private const string ClrIeX64FileName = "MicrosoftInstrumentationEngine_x64.dll";

        private const string VanguardX86ProfilerPath = @"covrun32.dll";
        private const string VanguardX64ProfilerPath = @"amd64\covrun64.dll";
        private const string VanguardX86ProfilerConfigPath = @"VanguardInstrumentationProfiler_x86.config";
        private const string VanguardX64ProfilerConfigPath = @"amd64\VanguardInstrumentationProfiler_x64.config";

        /// <summary>
        /// Vanguard executable name
        /// </summary>
        private const string VanguardExeName = @"CodeCoverage.exe";

        /// <inheritdoc />
        public string GetVanguardPath()
        {
            var vanguardPath = Path.Combine(this.GetVanguardDirectory(), VanguardExeName);
            if (!File.Exists(vanguardPath))
            {
                throw new VanguardException(string.Format(CultureInfo.CurrentUICulture, Resources.VanguardNotFound, vanguardPath));
            }

            return vanguardPath;
        }

        /// <inheritdoc />
        public string GetVanguardProfilerX86Path()
        {
            return Path.Combine(this.GetVanguardDirectory(), VanguardX86ProfilerPath);
        }

        /// <inheritdoc />
        public string GetVanguardProfilerX64Path()
        {
            return Path.Combine(this.GetVanguardDirectory(), VanguardX64ProfilerPath);
        }

        /// <inheritdoc />
        public string GetVanguardProfilerConfigX86Path()
        {
            return Path.Combine(this.GetVanguardDirectory(), VanguardX86ProfilerConfigPath);
        }

        /// <inheritdoc />
        public string GetVanguardProfilerConfigX64Path()
        {
            return Path.Combine(this.GetVanguardDirectory(), VanguardX64ProfilerConfigPath);
        }

        /// <inheritdoc />
        public string GetClrInstrumentationEngineX86Path()
        {
            return this.GetClrInstrumentationEnginePath("x86", ClrIeX86FileName, ClrIeX86InstallDirVariable);
        }

        /// <inheritdoc />
        public string GetClrInstrumentationEngineX64Path()
        {
            return this.GetClrInstrumentationEnginePath("x64", ClrIeX64FileName, ClrIeX64InstallDirVariable);
        }

        private string GetClrInstrumentationEnginePath(string arch, string fileName, string environmentVariableName)
        {
            var installationPath = Environment.GetEnvironmentVariable(environmentVariableName);

            if (!string.IsNullOrEmpty(installationPath))
            {
                return Path.Combine(installationPath, fileName);
            }

            return Path.Combine(this.GetCurrentAssemblyLocation(), "InstrumentationEngine", arch, fileName);
        }

        private string GetVanguardDirectory()
        {
            return Path.Combine(this.GetCurrentAssemblyLocation(), "CodeCoverage");
        }

        private string GetCurrentAssemblyLocation()
        {
            return Path.GetDirectoryName(typeof(ProfilersLocationProvider).GetTypeInfo().Assembly.Location);
        }
    }
}