// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NETSTANDARD1_0

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers
{
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Resources;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.Win32;
    using System;
    using System.IO;

    internal class DotnetHostHelper : IDotnetHostHelper
    {
        public const string MONOEXENAME = "mono";

        private readonly IFileHelper fileHelper;
        private readonly IEnvironment environment;
        private readonly IWindowsRegistryHelper windowsRegistryHelper;
        private readonly IEnvironmentVariableHelper environmentVariableHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="DotnetHostHelper"/> class.
        /// </summary>
        public DotnetHostHelper() : this(new FileHelper(), new PlatformEnvironment(), new WindowsRegistryHelper(), new EnvironmentVariableHelper())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DotnetHostHelper"/> class.
        /// </summary>
        /// <param name="fileHelper">File Helper</param>
        public DotnetHostHelper(
            IFileHelper fileHelper,
            IEnvironment environment,
            IWindowsRegistryHelper windowsRegistryHelper,
            IEnvironmentVariableHelper environmentVariableHelper)
        {
            this.fileHelper = fileHelper;
            this.environment = environment;
            this.windowsRegistryHelper = windowsRegistryHelper;
            this.environmentVariableHelper = environmentVariableHelper;
        }

        /// <inheritdoc />
        public string GetDotnetPath()
        {
            if (!TryGetExecutablePath("dotnet", out var dotnetPath))
            {
                string errorMessage = string.Format(Resources.NoDotnetExeFound, "dotnet");

                EqtTrace.Error(errorMessage);
                throw new FileNotFoundException(errorMessage);
            }

            return dotnetPath;
        }

        public string GetMonoPath()
        {
            if (!TryGetExecutablePath(MONOEXENAME, out var monoPath))
            {
                string errorMessage = string.Format(Resources.NoDotnetExeFound, MONOEXENAME);

                EqtTrace.Error(errorMessage);
                throw new FileNotFoundException(errorMessage);
            }

            return monoPath;
        }

        private bool TryGetExecutablePath(string executableBaseName, out string executablePath)
        {
            if (this.environment.OperatingSystem.Equals(PlatformOperatingSystem.Windows))
            {
                executableBaseName += ".exe";
            }

            executablePath = string.Empty;
            var pathString = Environment.GetEnvironmentVariable("PATH");
            foreach (string path in pathString.Split(Path.PathSeparator))
            {
                string exeFullPath = Path.Combine(path.Trim(), executableBaseName);
                if (this.fileHelper.Exists(exeFullPath))
                {
                    executablePath = exeFullPath;
                    return true;
                }
            }

            return false;
        }

        public string GetDotnetPathByArchitecture(PlatformArchitecture targetArchitecture)
        {
            // We used similar approach of runtime resolver.
            // https://github.com/dotnet/runtime/blob/main/src/native/corehost/fxr_resolver.cpp#L55

            string path = null;
            bool isWinOs = environment.OperatingSystem == PlatformOperatingSystem.Windows;
            string muxerName = $"dotnet{(isWinOs ? ".exe" : "")}";
            EqtTrace.Verbose($"DotnetHostHelper: current platform muxer '{muxerName}'");

            // Try to search using env vars in the order
            // DOTNET_ROOT_{arch}
            // DOTNET_ROOT(x86) if X86 on Win (here we cannot check if current process is WOW64 because this is SDK process arch and not real host arch so it's irrelevant)
            //                  "DOTNET_ROOT(x86) is used instead when running a 32-bit executable on a 64-bit OS."
            // DOTNET_ROOT
            string envVarPrefix = "DOTNET_ROOT";
            string envKey = $"{envVarPrefix}_{targetArchitecture.ToString().ToUpper()}";
            string envVar = this.environmentVariableHelper.GetEnvironmentVariable(envKey);
            if (envVar == null && targetArchitecture == PlatformArchitecture.X86 && this.environment.OperatingSystem == PlatformOperatingSystem.Windows)
            {
                envKey = $"{envVarPrefix}(x86)";
                envVar = this.environmentVariableHelper.GetEnvironmentVariable(envKey);
                if (envVar == null)
                {
                    envKey = envVarPrefix;
                    envVar = this.environmentVariableHelper.GetEnvironmentVariable(envKey);
                }
            }

            if (envVar != null)
            {
                envVar = Path.Combine(envVar, muxerName);
                if (this.fileHelper.Exists(envVar))
                {
                    path = envVar;
                    EqtTrace.Verbose($"DotnetHostHelper: muxer resolved using env var key '{envKey}' in '{path}'");
                    return path;
                }
            }

            // Try to search for global registration
            if (isWinOs)
            {
                // Installed version are always on 32-bit view of registry
                // https://github.com/dotnet/designs/blob/main/accepted/2020/install-locations.md#globally-registered-install-location-new
                // "Note that this registry key is "redirected" that means that 32-bit processes see different copy of the key then 64bit processes.
                // So it's important that both installers and the host access only the 32-bit view of the registry."
                using (IRegistryKey hklm = windowsRegistryHelper.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                {
                    if (hklm != null)
                    {
                        using (IRegistryKey dotnetInstalledVersion = hklm.OpenSubKey(@"SOFTWARE\dotnet\Setup\InstalledVersions"))
                        {
                            if (dotnetInstalledVersion != null)
                            {
                                using (IRegistryKey nativeArch = dotnetInstalledVersion.OpenSubKey(targetArchitecture.ToString().ToLower()))
                                {
                                    string installLocation = nativeArch?.GetValue("InstallLocation")?.ToString();
                                    if (installLocation != null)
                                    {
                                        path = Path.Combine(installLocation.Trim(), muxerName);
                                        EqtTrace.Verbose($"DotnetHostHelper: muxer resolved using win registry in '{path}'");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                string baseInstallLocation = "/etc/dotnet/";

                // We search for architecture specific installation
                string installLocation = $"{baseInstallLocation}install_location_{targetArchitecture.ToString().ToLower()}";

                // TODO: understand if we should access to this one or only arch one
                if (!this.fileHelper.Exists(installLocation))
                {
                    installLocation = $"{baseInstallLocation}install_location";
                }

                if (this.fileHelper.Exists(installLocation))
                {
                    using (Stream stream = this.fileHelper.GetStream(installLocation, FileMode.Open, FileAccess.Read))
                    using (StreamReader streamReader = new StreamReader(stream))
                    {
                        string content = streamReader.ReadToEnd().Trim();
                        EqtTrace.Verbose($"DotnetHostHelper: '{installLocation}' content '{content}'");
                        path = Path.Combine(content, muxerName);
                        EqtTrace.Verbose($"DotnetHostHelper: muxer resolved using '{installLocation}' in '{path}'");
                    }
                }
            }

            // Try on default installation location if exists
            if (path is null)
            {
                path = isWinOs ?

                    this.environment.Architecture == PlatformArchitecture.X86 ?
                    Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles(x86)"), "dotnet", muxerName) :
                    Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles"), "dotnet", muxerName) :

                    this.environment.OperatingSystem == PlatformOperatingSystem.OSX ?
                    // TODO: check if x64 assumptions is correct and if it's ok fallback to default on linux
                    $"/usr/local/share/dotnet/{muxerName}" + ((targetArchitecture == PlatformArchitecture.X64 && environment.Architecture == PlatformArchitecture.ARM64) ? "/x64" : "") :
                    $"/usr/share/dotnet/{muxerName}";

                EqtTrace.Verbose($"DotnetHostHelper: muxer resolved using default installation path in '{path}'");
            }

            if (!this.fileHelper.Exists(path))
            {
                string errorMessage = string.Format(Resources.NoDotnetMuxerFoundForArchitecture, muxerName, targetArchitecture.ToString());

                EqtTrace.Error(errorMessage);
                throw new FileNotFoundException(errorMessage);
            }

            return path;
        }
    }
}

#endif