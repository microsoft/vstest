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

    public class DotnetHostHelper : IDotnetHostHelper
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

        public bool TryGetNativeMuxerPath(IntPtr processHandle, out string path)
            => TryGetMuxerPath(processHandle, this.environment.Architecture, out path);

        public bool TryGetMuxerPath(IntPtr processHandle, PlatformArchitecture targetArchitecture, out string path)
        {
            // We used similar approach of runtime resolver.
            // https://github.com/dotnet/runtime/blob/main/src/native/corehost/fxr_resolver.cpp#L55

            path = null;
            bool isWinOs = environment.OperatingSystem == PlatformOperatingSystem.Windows;
            string muxerName = $"dotnet{(isWinOs ? ".exe" : "")}";

            // Search using env vars
            string envVarPrefix = "DOTNET_ROOT";
            string envKey = $"{envVarPrefix}_{targetArchitecture.ToString().ToUpper()}";
            string envVar = this.environmentVariableHelper.GetEnvironmentVariable(envKey);
            if (envVar == null && targetArchitecture == PlatformArchitecture.X86)
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
                    return true;
                }
            }

            // Search for global registration
            if (isWinOs)
            {
                // Installed version are always on 32-bit view of registry
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
                                        path = Path.Combine(installLocation, muxerName);
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
                string nativeInstallLocation = $"/etc/dotnet/install_location_{targetArchitecture.ToString().ToLower()}";
                if (!this.fileHelper.Exists(nativeInstallLocation))
                {
                    nativeInstallLocation = "/etc/dotnet/install_location";
                }

                if (this.fileHelper.Exists(nativeInstallLocation))
                {
                    using (Stream stream = this.fileHelper.GetStream(nativeInstallLocation, FileMode.Open, FileAccess.Read))
                    using (StreamReader streamReader = new StreamReader(stream))
                    {
                        path = Path.Combine(streamReader.ReadToEnd(), muxerName);
                        EqtTrace.Verbose($"DotnetHostHelper: muxer resolved using '{nativeInstallLocation}' in '{path}'");
                    }
                }
            }

            // Fallback to default installation location if exists
            if (path is null)
            {
                path = isWinOs ?

                    this.environment.Architecture == PlatformArchitecture.X86 ?
                    Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles(x86)"), "dotnet", muxerName) :
                    Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles"), "dotnet", muxerName) :

                    this.environment.OperatingSystem == PlatformOperatingSystem.OSX ?
                    // TODO: to elaborate
                    $"/usr/local/share/dotnet/{muxerName}" + ((targetArchitecture == PlatformArchitecture.X64 && environment.Architecture == PlatformArchitecture.ARM64) ? "/x64" : "") :
                    $"/usr/share/dotnet/{muxerName}";

                EqtTrace.Verbose($"DotnetHostHelper: muxer resolved using default installation path in '{path}'");
            }

            bool found = this.fileHelper.Exists(path);
            path = found ? path : null;
            EqtTrace.Verbose($"DotnetHostHelper: muxer {(found ? "found" : "not found")} in path '{path}'");
            return found;
        }
    }
}

#endif