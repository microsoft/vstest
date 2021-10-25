// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CoreUtilities.UnitTests.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Win32;
    using Moq;

    [TestClass]
    public class DotnetHostHelperTest
    {
        private static string DotnetMuxerWinX86 = "TestAssets/dotnetWinX86.exe";
        private static string DotnetMuxerWinX64 = "TestAssets/dotnetWinX64.exe";
        private static string DotnetMuxerWinArm64 = "TestAssets/dotnetWinArm64.exe";
        private static string DotnetMuxerMacArm64 = "TestAssets/dotnetMacArm64";
        private static string DotnetMuxerMacX64 = "TestAssets/dotnetMacX64";

        private readonly Mock<IFileHelper> fileHelper = new Mock<IFileHelper>();
        private readonly Mock<IProcessHelper> processHelper = new Mock<IProcessHelper>();
        private readonly Mock<IEnvironment> environmentHelper = new Mock<IEnvironment>();
        private readonly Mock<IWindowsRegistryHelper> windowsRegistrytHelper = new Mock<IWindowsRegistryHelper>();
        private readonly Mock<IEnvironmentVariableHelper> environmentVariableHelper = new Mock<IEnvironmentVariableHelper>();

        public DotnetHostHelperTest()
        {
            Assert.IsTrue(File.Exists(DotnetMuxerWinX86));
            Assert.IsTrue(File.Exists(DotnetMuxerWinX64));
            Assert.IsTrue(File.Exists(DotnetMuxerWinArm64));
            Assert.IsTrue(File.Exists(DotnetMuxerMacArm64));
            Assert.IsTrue(File.Exists(DotnetMuxerMacX64));
        }

        private string RenameMuxerAndReturnPath(PlatformOperatingSystem platform, PlatformArchitecture architecture)
        {
            string tmpDirectory = Path.GetTempPath();
            switch (platform)
            {
                case PlatformOperatingSystem.Windows:
                    {
                        string muxerPath = Path.Combine(tmpDirectory, Guid.NewGuid().ToString("N"), "dotnet.exe");
                        Directory.CreateDirectory(Path.GetDirectoryName(muxerPath));
                        if (architecture == PlatformArchitecture.ARM64)
                        {
                            File.Copy(DotnetMuxerWinArm64, muxerPath);
                            return muxerPath;
                        }
                        else if (architecture == PlatformArchitecture.X64)
                        {
                            File.Copy(DotnetMuxerWinX64, muxerPath);
                            return muxerPath;
                        }
                        else if (architecture == PlatformArchitecture.X86)
                        {
                            File.Copy(DotnetMuxerWinX86, muxerPath);
                            return muxerPath;
                        }

                        throw new NotSupportedException($"Unsupported architecture '{architecture}'");
                    }
                case PlatformOperatingSystem.OSX:
                    {
                        string muxerPath = Path.Combine(tmpDirectory, Guid.NewGuid().ToString("N"), "dotnet");
                        Directory.CreateDirectory(Path.GetDirectoryName(muxerPath));
                        if (architecture == PlatformArchitecture.ARM64)
                        {
                            File.Copy(DotnetMuxerMacArm64, muxerPath);
                            return muxerPath;
                        }
                        else if (architecture == PlatformArchitecture.X64)
                        {
                            File.Copy(DotnetMuxerMacX64, muxerPath);
                            return muxerPath;
                        }

                        throw new NotSupportedException($"Unsupported architecture '{architecture}'");
                    }
                case PlatformOperatingSystem.Unix:
                default:
                    throw new NotSupportedException($"Unsupported OS '{platform}'");
            }
        }

        [TestMethod]
        public void GetDotnetPathByArchitecture_SameArchitecture()
        {
            // Arrange
            string finalMuxerPath = RenameMuxerAndReturnPath(PlatformOperatingSystem.Windows, PlatformArchitecture.X64);
            var dotnetHostHelper = new DotnetHostHelper(fileHelper.Object, environmentHelper.Object, windowsRegistrytHelper.Object, environmentVariableHelper.Object, processHelper.Object);
            environmentHelper.SetupGet(x => x.OperatingSystem).Returns(PlatformOperatingSystem.Windows);
            environmentHelper.SetupGet(x => x.Architecture).Returns(PlatformArchitecture.X64);
            processHelper.Setup(x => x.GetCurrentProcessFileName()).Returns(finalMuxerPath);

            // Act & Assert
            Assert.IsTrue(dotnetHostHelper.TryGetDotnetPathByArchitecture(PlatformArchitecture.X64, out string muxerPath));
            Assert.AreEqual(finalMuxerPath, muxerPath);

            // Cleanup
            Directory.Delete(Path.GetDirectoryName(finalMuxerPath), true);
        }

        [DataTestMethod]
        [DataRow(PlatformArchitecture.X86, PlatformArchitecture.X64, PlatformOperatingSystem.Windows, "DOTNET_ROOT(x86)")]
        [DataRow(PlatformArchitecture.X86, PlatformArchitecture.X64, PlatformOperatingSystem.Windows, "DOTNET_ROOT")]

        [DataRow(PlatformArchitecture.ARM64, PlatformArchitecture.X64, PlatformOperatingSystem.Windows, "DOTNET_ROOT(x86)", false)]
        [DataRow(PlatformArchitecture.ARM64, PlatformArchitecture.X64, PlatformOperatingSystem.Windows, "DOTNET_ROOT_ARM64")]
        [DataRow(PlatformArchitecture.ARM64, PlatformArchitecture.X64, PlatformOperatingSystem.Windows, "DOTNET_ROOT")]

        [DataRow(PlatformArchitecture.X64, PlatformArchitecture.X64, PlatformOperatingSystem.Windows, "DOTNET_ROOT(x86)", false)]
        [DataRow(PlatformArchitecture.X64, PlatformArchitecture.ARM64, PlatformOperatingSystem.Windows, "DOTNET_ROOT_X64")]
        [DataRow(PlatformArchitecture.X64, PlatformArchitecture.ARM64, PlatformOperatingSystem.Windows, "DOTNET_ROOT")]

        [DataRow(PlatformArchitecture.ARM64, PlatformArchitecture.X64, PlatformOperatingSystem.OSX, "DOTNET_ROOT_ARM64")]
        [DataRow(PlatformArchitecture.ARM64, PlatformArchitecture.X64, PlatformOperatingSystem.OSX, "DOTNET_ROOT")]

        [DataRow(PlatformArchitecture.X64, PlatformArchitecture.ARM64, PlatformOperatingSystem.OSX, "DOTNET_ROOT_X64")]
        [DataRow(PlatformArchitecture.X64, PlatformArchitecture.ARM64, PlatformOperatingSystem.OSX, "DOTNET_ROOT")]
        public void GetDotnetPathByArchitecture_EnvVars(PlatformArchitecture targetArchitecture,
            PlatformArchitecture platformArchitecture,
            PlatformOperatingSystem platformSystem,
            string envVar,
            bool found = true)
        {
            // Arrange
            string DOTNET_ROOT_X64 = RenameMuxerAndReturnPath(platformSystem, PlatformArchitecture.X64);
            string DOTNET_ROOT_ARM64 = RenameMuxerAndReturnPath(platformSystem, PlatformArchitecture.ARM64);
            string DOTNET_ROOT_X86 = null;
            if (platformSystem == PlatformOperatingSystem.Windows)
            {
                DOTNET_ROOT_X86 = RenameMuxerAndReturnPath(platformSystem, PlatformArchitecture.X86);
            }
            string DOTNET_ROOT = RenameMuxerAndReturnPath(platformSystem, targetArchitecture);
            Dictionary<string, string> envVars = new Dictionary<string, string>()
            {
                { "DOTNET_ROOT_X64" , DOTNET_ROOT_X64},
                { "DOTNET_ROOT_ARM64" , DOTNET_ROOT_ARM64},
                { "DOTNET_ROOT(x86)" , DOTNET_ROOT_X86},
                { "DOTNET_ROOT" , DOTNET_ROOT},
            };

            environmentHelper.SetupGet(x => x.Architecture).Returns(platformArchitecture);
            environmentHelper.SetupGet(x => x.OperatingSystem).Returns(platformSystem);
            environmentVariableHelper.Setup(x => x.GetEnvironmentVariable(envVar)).Returns(Path.GetDirectoryName(envVars[envVar]));
            environmentVariableHelper.Setup(x => x.GetEnvironmentVariable("ProgramFiles")).Returns("notfound");
            fileHelper.Setup(x => x.DirectoryExists(Path.GetDirectoryName(envVars[envVar]))).Returns(true);
            fileHelper.Setup(x => x.Exists(envVars[envVar])).Returns(true);
            if (found)
            {
                fileHelper.Setup(x => x.GetStream(envVars[envVar], FileMode.Open, FileAccess.Read)).Returns(File.OpenRead(envVars[envVar]));
            }

            // Act & Assert
            var dotnetHostHelper = new DotnetHostHelper(fileHelper.Object, environmentHelper.Object, windowsRegistrytHelper.Object, environmentVariableHelper.Object, processHelper.Object);
            Assert.AreEqual(found, dotnetHostHelper.TryGetDotnetPathByArchitecture(targetArchitecture, out string muxerPath));
            if (found)
            {
                Assert.AreEqual(envVars[envVar], muxerPath);
            }

            // Cleanup
            Directory.Delete(Path.GetDirectoryName(DOTNET_ROOT_X64), true);
            if (platformSystem == PlatformOperatingSystem.Windows)
            {
                Directory.Delete(Path.GetDirectoryName(DOTNET_ROOT_X86), true);
            }
            Directory.Delete(Path.GetDirectoryName(DOTNET_ROOT), true);
            Directory.Delete(Path.GetDirectoryName(DOTNET_ROOT_ARM64), true);
        }

        [DataTestMethod]
        [DataRow("DOTNET_ROOT_ARM64", "DOTNET_ROOT", PlatformArchitecture.ARM64, PlatformArchitecture.X64)]
        [DataRow("DOTNET_ROOT(x86)", "DOTNET_ROOT", PlatformArchitecture.X86, PlatformArchitecture.X64)]
        public void GetDotnetPathByArchitecture_EnvVars_DirectoryNotExists_TryNext(string notExists, string nextEnv, PlatformArchitecture targetAchitecture, PlatformArchitecture platformArchitecture)
        {
            // Arrange
            string DOTNET_ROOT_X64 = RenameMuxerAndReturnPath(PlatformOperatingSystem.Windows, PlatformArchitecture.X64);
            string DOTNET_ROOT_ARM64 = RenameMuxerAndReturnPath(PlatformOperatingSystem.Windows, PlatformArchitecture.ARM64);
            string DOTNET_ROOT_X86 = RenameMuxerAndReturnPath(PlatformOperatingSystem.Windows, PlatformArchitecture.X86);
            string DOTNET_ROOT = RenameMuxerAndReturnPath(PlatformOperatingSystem.Windows, targetAchitecture);
            Dictionary<string, string> envVars = new Dictionary<string, string>()
            {
                { "DOTNET_ROOT_X64" , DOTNET_ROOT_X64},
                { "DOTNET_ROOT_ARM64" , DOTNET_ROOT_ARM64},
                { "DOTNET_ROOT(x86)" , DOTNET_ROOT_X86},
                { "DOTNET_ROOT" , DOTNET_ROOT},
            };

            environmentHelper.SetupGet(x => x.Architecture).Returns(platformArchitecture);
            environmentHelper.SetupGet(x => x.OperatingSystem).Returns(PlatformOperatingSystem.Windows);
            environmentVariableHelper.Setup(x => x.GetEnvironmentVariable(notExists)).Returns(Path.GetDirectoryName(envVars[notExists]));
            environmentVariableHelper.Setup(x => x.GetEnvironmentVariable(nextEnv)).Returns(Path.GetDirectoryName(envVars[nextEnv]));
            fileHelper.Setup(x => x.DirectoryExists(Path.GetDirectoryName(envVars[nextEnv]))).Returns(true);
            fileHelper.Setup(x => x.Exists(envVars[nextEnv])).Returns(true);
            fileHelper.Setup(x => x.GetStream(envVars[nextEnv], FileMode.Open, FileAccess.Read)).Returns(File.OpenRead(envVars[nextEnv]));

            //Act & Assert
            var dotnetHostHelper = new DotnetHostHelper(fileHelper.Object, environmentHelper.Object, windowsRegistrytHelper.Object, environmentVariableHelper.Object, processHelper.Object);
            Assert.IsTrue(dotnetHostHelper.TryGetDotnetPathByArchitecture(targetAchitecture, out string muxerPath));
            Assert.AreEqual(envVars[nextEnv], muxerPath);

            // Cleanup
            Directory.Delete(Path.GetDirectoryName(DOTNET_ROOT_X64), true);
            Directory.Delete(Path.GetDirectoryName(DOTNET_ROOT_X86), true);
            Directory.Delete(Path.GetDirectoryName(DOTNET_ROOT), true);
            Directory.Delete(Path.GetDirectoryName(DOTNET_ROOT_ARM64), true);
        }

        [DataTestMethod]
        [DataRow(PlatformArchitecture.X64, PlatformArchitecture.X64, true)]
        [DataRow(PlatformArchitecture.X64, PlatformArchitecture.X86, false)]
        public void GetDotnetPathByArchitecture_GlobalInstallation_Windows(PlatformArchitecture muxerArchitecture, PlatformArchitecture targetArchitecture, bool found)
        {
            // Arrange
            string dotnetMuxer = RenameMuxerAndReturnPath(PlatformOperatingSystem.Windows, muxerArchitecture);
            Mock<IRegistryKey> installedVersionKey = new Mock<IRegistryKey>();
            Mock<IRegistryKey> architectureSubKey = new Mock<IRegistryKey>();
            Mock<IRegistryKey> nativeArchSubKey = new Mock<IRegistryKey>();
            installedVersionKey.Setup(x => x.OpenSubKey(It.IsAny<string>())).Returns(architectureSubKey.Object);
            architectureSubKey.Setup(x => x.OpenSubKey(It.IsAny<string>())).Returns(nativeArchSubKey.Object);
            nativeArchSubKey.Setup(x => x.GetValue(It.IsAny<string>())).Returns(Path.GetDirectoryName(dotnetMuxer));
            this.windowsRegistrytHelper.Setup(x => x.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)).Returns(installedVersionKey.Object);
            this.fileHelper.Setup(x => x.Exists(dotnetMuxer)).Returns(true);
            this.fileHelper.Setup(x => x.GetStream(dotnetMuxer, FileMode.Open, FileAccess.Read)).Returns(File.OpenRead(dotnetMuxer));

            //Act & Assert
            var dotnetHostHelper = new DotnetHostHelper(fileHelper.Object, environmentHelper.Object, windowsRegistrytHelper.Object, environmentVariableHelper.Object, processHelper.Object);
            Assert.AreEqual(found, dotnetHostHelper.TryGetDotnetPathByArchitecture(targetArchitecture, out string muxerPath));
            if (found)
            {
                Assert.AreEqual(dotnetMuxer, muxerPath);
            }

            // Cleanup
            Directory.Delete(Path.GetDirectoryName(dotnetMuxer), true);
        }

        [DataTestMethod]
        [DataRow(true, false, false, false)]
        [DataRow(false, true, false, false)]
        [DataRow(false, false, true, false)]
        [DataRow(false, false, false, true)]
        public void GetDotnetPathByArchitecture_GlobalInstallation_NullSubkeys(bool nullInstalledVersion, bool nullArchitecture, bool nullNative, bool nullInstallLocation)
        {
            // Arrange
            Mock<IRegistryKey> installedVersionKey = new Mock<IRegistryKey>();
            Mock<IRegistryKey> architectureSubKey = new Mock<IRegistryKey>();
            Mock<IRegistryKey> nativeArchSubKey = new Mock<IRegistryKey>();
            installedVersionKey.Setup(x => x.OpenSubKey(It.IsAny<string>())).Returns(nullArchitecture ? null : architectureSubKey.Object);
            architectureSubKey.Setup(x => x.OpenSubKey(It.IsAny<string>())).Returns(nullNative ? null : nativeArchSubKey.Object);
            nativeArchSubKey.Setup(x => x.GetValue(It.IsAny<string>())).Returns(nullInstallLocation ? null : "");
            this.windowsRegistrytHelper.Setup(x => x.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)).Returns(nullInstalledVersion ? null : installedVersionKey.Object);
            this.environmentVariableHelper.Setup(x => x.GetEnvironmentVariable("ProgramFiles")).Returns("notfound");

            //Act & Assert
            var dotnetHostHelper = new DotnetHostHelper(fileHelper.Object, environmentHelper.Object, windowsRegistrytHelper.Object, environmentVariableHelper.Object, processHelper.Object);
            Assert.IsFalse(dotnetHostHelper.TryGetDotnetPathByArchitecture(PlatformArchitecture.X64, out string muxerPath));
        }
    }
}
