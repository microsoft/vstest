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
    using System.Reflection.PortableExecutable;

    internal class DotnetHostHelper : IDotnetHostHelper
    {
        public const string MONOEXENAME = "mono";

        private readonly IFileHelper fileHelper;
        private readonly IEnvironment environment;
        private readonly IWindowsRegistryHelper windowsRegistryHelper;
        private readonly IEnvironmentVariableHelper environmentVariableHelper;
        private readonly string muxerName;

        /// <summary>
        /// Initializes a new instance of the <see cref="DotnetHostHelper"/> class.
        /// </summary>
        public DotnetHostHelper() : this(new FileHelper(), new PlatformEnvironment(), new WindowsRegistryHelper(), new EnvironmentVariableHelper())
        {
            this.muxerName = $"dotnet{(environment.OperatingSystem == PlatformOperatingSystem.Windows ? ".exe" : "")}";
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

        public bool TryGetDotnetPathByArchitecture(PlatformArchitecture targetArchitecture, out string muxerPath)
        {
            // We used similar approach as the runtime resolver.
            // https://github.com/dotnet/runtime/blob/main/src/native/corehost/fxr_resolver.cpp#L55

            bool isWinOs = environment.OperatingSystem == PlatformOperatingSystem.Windows;
            EqtTrace.Verbose($"DotnetHostHelper: Current platform muxer '{muxerName}'");

            // Try to search using env vars in the order
            // DOTNET_ROOT_{arch}
            // DOTNET_ROOT(x86) if X86 on Win (here we cannot check if current process is WOW64 because this is SDK process arch and not real host arch so it's irrelevant)
            //                  "DOTNET_ROOT(x86) is used instead when running a 32-bit executable on a 64-bit OS."
            // DOTNET_ROOT
            string envVarPrefix = "DOTNET_ROOT";
            string envKey = $"{envVarPrefix}_{targetArchitecture.ToString().ToUpperInvariant()}";
            string envVar = this.environmentVariableHelper.GetEnvironmentVariable(envKey);
            if (envVar == null && targetArchitecture == PlatformArchitecture.X86 && this.environment.OperatingSystem == PlatformOperatingSystem.Windows)
            {
                envKey = $"{envVarPrefix}(x86)";
                envVar = this.environmentVariableHelper.GetEnvironmentVariable(envKey);
            }

            if (envVar == null)
            {
                envKey = envVarPrefix;
                envVar = this.environmentVariableHelper.GetEnvironmentVariable(envKey);
            }

            if (envVar != null)
            {
                envVar = Path.Combine(envVar, muxerName);
                if (this.fileHelper.Exists(envVar))
                {
                    if (IsValidArchitectureMuxer(targetArchitecture, envVar))
                    {
                        muxerPath = envVar;
                        EqtTrace.Verbose($"DotnetHostHelper: Muxer resolved using env var key '{envKey}' in '{muxerPath}'");
                        return true;
                    }
                    else
                    {
                        EqtTrace.Verbose($"DotnetHostHelper: Invalid muxer resolved using env var key '{envKey}' in '{envVar}'");
                    }
                }
            }

            // Try to search for global registration
            if (isWinOs)
            {
                muxerPath = GetMuxerFromGlobalRegistrationWin(targetArchitecture);
            }
            else
            {
                muxerPath = GetMuxerFromGlobalRegistrationOnUnix(targetArchitecture);
            }

            if (muxerPath != null && !IsValidArchitectureMuxer(targetArchitecture, muxerPath))
            {
                EqtTrace.Verbose($"DotnetHostHelper: Invalid muxer resolved using global registration '{muxerPath}'");
                muxerPath = null;
            }

            // Try on default installation location if exists
            if (muxerPath is null)
            {
                if (isWinOs)
                {
                    muxerPath = this.environment.Architecture == PlatformArchitecture.X64 && targetArchitecture == PlatformArchitecture.X86 ?
                        Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles(x86)"), "dotnet", muxerName) :
                        Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles"), "dotnet", muxerName);
                }
                else
                {
                    muxerPath = this.environment.OperatingSystem == PlatformOperatingSystem.OSX ?
                    $"/usr/local/share/dotnet/{muxerName}" :
                    $"/usr/share/dotnet/{muxerName}";
                }

                if (muxerPath != null && this.fileHelper.Exists(muxerPath))
                {
                    if (!IsValidArchitectureMuxer(targetArchitecture, muxerPath))
                    {
                        EqtTrace.Verbose($"DotnetHostHelper: Invalid muxer resolved using default installation path '{muxerPath}'");
                        muxerPath = null;
                    }
                    else
                    {
                        EqtTrace.Verbose($"DotnetHostHelper: Muxer resolved using default installation path in '{muxerPath}'");
                    }
                }
            }

            return muxerPath != null;
        }

        private string GetMuxerFromGlobalRegistrationWin(PlatformArchitecture targetArchitecture)
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
                            using (IRegistryKey nativeArch = dotnetInstalledVersion.OpenSubKey(targetArchitecture.ToString().ToLowerInvariant()))
                            {
                                string installLocation = nativeArch?.GetValue("InstallLocation")?.ToString();

                                if (installLocation != null)
                                {
                                    string path = Path.Combine(installLocation.Trim(), this.muxerName);
                                    EqtTrace.Verbose($"DotnetHostHelper: Muxer resolved using win registry in '{path}'");
                                    return path;
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        private string GetMuxerFromGlobalRegistrationOnUnix(PlatformArchitecture targetArchitecture)
        {
            string baseInstallLocation = "/etc/dotnet/";

            // We search for architecture specific installation
            string installLocation = $"{baseInstallLocation}install_location_{targetArchitecture.ToString().ToLowerInvariant()}";

            // We try to load archless install location file
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
                    string path = Path.Combine(content, this.muxerName);
                    EqtTrace.Verbose($"DotnetHostHelper: Muxer resolved using '{installLocation}' in '{path}'");
                    return path;
                }
            }

            return null;
        }

        private PlatformArchitecture? GetMuxerArchitectureByPEHeaderOnWin(string path)
        {
            try
            {
                using (Stream stream = this.fileHelper.GetStream(path, FileMode.Open, FileAccess.Read))
                using (PEReader peReader = new PEReader(stream))
                {
                    switch (peReader.PEHeaders.CoffHeader.Machine)
                    {
                        case Machine.Amd64:
                            return PlatformArchitecture.X64;
                        case Machine.IA64:
                            return PlatformArchitecture.X64;
                        case Machine.Arm64:
                            return PlatformArchitecture.ARM64;
                        case Machine.Arm:
                            return PlatformArchitecture.ARM;
                        case Machine.I386:
                            return PlatformArchitecture.X86;
                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                EqtTrace.Verbose($"DotnetHostHelper: Failed to get architecture from PEHeader for '{path}'\n{ex}");
            }

            return null;
        }

        // See https://opensource.apple.com/source/xnu/xnu-2050.18.24/EXTERNAL_HEADERS/mach-o/loader.h
        // https://opensource.apple.com/source/xnu/xnu-4570.41.2/osfmk/mach/machine.h.auto.html
        private PlatformArchitecture? GetMuxerArchitectureByMachoOnMac(string path)
        {
            try
            {
                PlatformArchitecture? architecture;
                using (var headerReader = File.OpenRead(path))
                {
                    var magicBytes = new byte[4];
                    var cpuInfoBytes = new byte[4];
                    headerReader.Read(magicBytes, 0, magicBytes.Length);
                    headerReader.Read(cpuInfoBytes, 0, cpuInfoBytes.Length);

                    var magic = BitConverter.ToUInt32(magicBytes, 0);
                    var cpuInfo = BitConverter.ToUInt32(cpuInfoBytes, 0);
                    switch ((MacOsCpuType)cpuInfo)
                    {
                        case MacOsCpuType.Arm64Magic:
                        case MacOsCpuType.Arm64Cigam:
                            architecture = PlatformArchitecture.ARM64;
                            break;
                        case MacOsCpuType.X64Magic:
                        case MacOsCpuType.X64Cigam:
                            architecture = PlatformArchitecture.X64;
                            break;
                        case MacOsCpuType.X86Magic:
                        case MacOsCpuType.X86Cigam:
                            architecture = PlatformArchitecture.X86;
                            break;
                        default:
                            architecture = null;
                            break;
                    }

                    return architecture;
                }
            }
            catch (Exception ex)
            {
                EqtTrace.Verbose($"DotnetHostHelper: Failed to get architecture from Mach-O for '{path}'\n{ex}");
            }

            return null;
        }

        internal enum MacOsCpuType : uint
        {
            Arm64Magic = 0x0100000c,
            Arm64Cigam = 0x0c000001,
            X64Magic = 0x01000007,
            X64Cigam = 0x07000001,
            X86Magic = 0x00000007,
            X86Cigam = 0x07000000
        }

        private bool IsValidArchitectureMuxer(PlatformArchitecture targetArchitecture, string path)
        {
            PlatformArchitecture? muxerPlaform = null;
            if (this.environment.OperatingSystem == PlatformOperatingSystem.Windows)
            {
                muxerPlaform =  GetMuxerArchitectureByPEHeaderOnWin(path);
            }

            if (this.environment.OperatingSystem == PlatformOperatingSystem.OSX)
            {
                muxerPlaform = GetMuxerArchitectureByMachoOnMac(path);
            }

            if (targetArchitecture != muxerPlaform)
            {
                EqtTrace.Verbose($"DotnetHostHelper: Invalid architecture mutex, target architecture '{targetArchitecture}', actual '{muxerPlaform}'");
                return false;
            }

            return true;
        }
    }
}

#endif