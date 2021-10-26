// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CoreUtilities.UnitTests.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Win32;
    using Moq;

    [TestClass]
    public class DotnetHostHelperTest : IDisposable
    {
        private readonly Mock<IFileHelper> fileHelper = new Mock<IFileHelper>();
        private readonly Mock<IProcessHelper> processHelper = new Mock<IProcessHelper>();
        private readonly Mock<IEnvironment> environmentHelper = new Mock<IEnvironment>();
        private readonly Mock<IWindowsRegistryHelper> windowsRegistrytHelper = new Mock<IWindowsRegistryHelper>();
        private readonly Mock<IEnvironmentVariableHelper> environmentVariableHelper = new Mock<IEnvironmentVariableHelper>();
        private readonly MockMuxerHelper muxerHelper = new MockMuxerHelper();

        [TestMethod]
        public void GetDotnetPathByArchitecture_SameArchitecture()
        {
            // Arrange
            string finalMuxerPath = muxerHelper.RenameMuxerAndReturnPath(PlatformOperatingSystem.Windows, PlatformArchitecture.X64);
            var dotnetHostHelper = new DotnetHostHelper(fileHelper.Object, environmentHelper.Object, windowsRegistrytHelper.Object, environmentVariableHelper.Object, processHelper.Object);
            environmentHelper.SetupGet(x => x.OperatingSystem).Returns(PlatformOperatingSystem.Windows);
            environmentHelper.SetupGet(x => x.Architecture).Returns(PlatformArchitecture.X64);
            processHelper.Setup(x => x.GetCurrentProcessFileName()).Returns(finalMuxerPath);

            // Act & Assert
            Assert.IsTrue(dotnetHostHelper.TryGetDotnetPathByArchitecture(PlatformArchitecture.X64, out string muxerPath));
            Assert.AreEqual(finalMuxerPath, muxerPath);
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
            string DOTNET_ROOT_X64 = muxerHelper.RenameMuxerAndReturnPath(platformSystem, PlatformArchitecture.X64);
            string DOTNET_ROOT_ARM64 = muxerHelper.RenameMuxerAndReturnPath(platformSystem, PlatformArchitecture.ARM64);
            string DOTNET_ROOT_X86 = null;
            if (platformSystem == PlatformOperatingSystem.Windows)
            {
                DOTNET_ROOT_X86 = muxerHelper.RenameMuxerAndReturnPath(platformSystem, PlatformArchitecture.X86);
            }
            string DOTNET_ROOT = muxerHelper.RenameMuxerAndReturnPath(platformSystem, targetArchitecture);
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
            Assert.AreEqual(found ? envVars[envVar] : null, muxerPath);
        }

        [DataTestMethod]
        [DataRow("DOTNET_ROOT_ARM64", "DOTNET_ROOT", PlatformArchitecture.ARM64, PlatformArchitecture.X64)]
        [DataRow("DOTNET_ROOT(x86)", "DOTNET_ROOT", PlatformArchitecture.X86, PlatformArchitecture.X64)]
        public void GetDotnetPathByArchitecture_EnvVars_DirectoryNotExists_TryNext(string notExists, string nextEnv, PlatformArchitecture targetAchitecture, PlatformArchitecture platformArchitecture)
        {
            // Arrange
            string DOTNET_ROOT_X64 = muxerHelper.RenameMuxerAndReturnPath(PlatformOperatingSystem.Windows, PlatformArchitecture.X64);
            string DOTNET_ROOT_ARM64 = muxerHelper.RenameMuxerAndReturnPath(PlatformOperatingSystem.Windows, PlatformArchitecture.ARM64);
            string DOTNET_ROOT_X86 = muxerHelper.RenameMuxerAndReturnPath(PlatformOperatingSystem.Windows, PlatformArchitecture.X86);
            string DOTNET_ROOT = muxerHelper.RenameMuxerAndReturnPath(PlatformOperatingSystem.Windows, targetAchitecture);
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
        }

        [DataTestMethod]
        [DataRow(PlatformArchitecture.X64, PlatformArchitecture.X64, true)]
        [DataRow(PlatformArchitecture.X64, PlatformArchitecture.X86, false)]
        public void GetDotnetPathByArchitecture_GlobalInstallation_Windows(PlatformArchitecture muxerArchitecture, PlatformArchitecture targetArchitecture, bool found)
        {
            // Arrange
            string dotnetMuxer = muxerHelper.RenameMuxerAndReturnPath(PlatformOperatingSystem.Windows, muxerArchitecture);
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
            Assert.AreEqual(found ? dotnetMuxer : null, muxerPath);
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

        [DataTestMethod]
        [DataRow(PlatformArchitecture.ARM64, "/etc/dotnet/install_location_arm64", true)]
        [DataRow(PlatformArchitecture.X64, "/etc/dotnet/install_location_x64", true)]
        [DataRow(PlatformArchitecture.ARM64, "/etc/dotnet/install_location", true)]
        [DataRow(PlatformArchitecture.X64, "/etc/dotnet/install_location", true)]
        [DataRow(PlatformArchitecture.ARM64, "/etc/dotnet/install_location_x64", false)]
        public void GetDotnetPathByArchitecture_GlobalInstallation_Mac(PlatformArchitecture targetArchitecture, string install_location, bool found)
        {
            // Arrange
            string dotnetMuxer = muxerHelper.RenameMuxerAndReturnPath(PlatformOperatingSystem.OSX, targetArchitecture);
            this.environmentHelper.SetupGet(x => x.OperatingSystem).Returns(PlatformOperatingSystem.OSX);
            this.fileHelper.Setup(x => x.Exists(install_location)).Returns(true);
            this.fileHelper.Setup(x => x.Exists(dotnetMuxer)).Returns(true);
            this.fileHelper.Setup(x => x.GetStream(install_location, FileMode.Open, FileAccess.Read)).Returns(new MemoryStream(Encoding.UTF8.GetBytes(Path.GetDirectoryName(dotnetMuxer))));
            if (found)
            {
                this.fileHelper.Setup(x => x.GetStream(dotnetMuxer, FileMode.Open, FileAccess.Read)).Returns(File.OpenRead(dotnetMuxer));
            }

            //Act & Assert
            var dotnetHostHelper = new DotnetHostHelper(fileHelper.Object, environmentHelper.Object, windowsRegistrytHelper.Object, environmentVariableHelper.Object, processHelper.Object);
            Assert.AreEqual(found, dotnetHostHelper.TryGetDotnetPathByArchitecture(targetArchitecture, out string muxerPath));
            Assert.AreEqual(found ? dotnetMuxer : null, muxerPath);
        }

        [DataTestMethod]
        [DataRow(PlatformArchitecture.X86, PlatformArchitecture.X64, "ProgramFiles(x86)", "dotnet", true)]
        [DataRow(PlatformArchitecture.X64, PlatformArchitecture.ARM64, "ProgramFiles", @"dotnet\x64", true)]
        [DataRow(PlatformArchitecture.X64, PlatformArchitecture.X64, "ProgramFiles", "dotnet", true)]
        [DataRow(PlatformArchitecture.X86, PlatformArchitecture.X86, "ProgramFiles", "dotnet", true)]
        [DataRow(PlatformArchitecture.X64, PlatformArchitecture.X64, "ProgramFiles", "dotnet", false)]
        public void GetDotnetPathByArchitecture_DefaultInstallation_Win(PlatformArchitecture targetArchitecture, PlatformArchitecture platformArchitecture, string envVar, string subfolder, bool found)
        {
            // Arrange
            string dotnetMuxer = muxerHelper.RenameMuxerAndReturnPath(PlatformOperatingSystem.Windows, targetArchitecture, subfolder);
            this.environmentVariableHelper.Setup(x => x.GetEnvironmentVariable(envVar)).Returns(dotnetMuxer.Replace(Path.Combine(subfolder, "dotnet.exe"), string.Empty));
            this.environmentHelper.Setup(x => x.Architecture).Returns(platformArchitecture);
            if (found)
            {
                this.fileHelper.Setup(x => x.Exists(dotnetMuxer)).Returns(true);
                this.fileHelper.Setup(x => x.GetStream(dotnetMuxer, FileMode.Open, FileAccess.Read)).Returns(new MemoryStream(Encoding.UTF8.GetBytes(Path.GetDirectoryName(dotnetMuxer))));
                this.fileHelper.Setup(x => x.GetStream(dotnetMuxer, FileMode.Open, FileAccess.Read)).Returns(File.OpenRead(dotnetMuxer));
            }

            //Act & Assert
            var dotnetHostHelper = new DotnetHostHelper(fileHelper.Object, environmentHelper.Object, windowsRegistrytHelper.Object, environmentVariableHelper.Object, processHelper.Object);
            Assert.AreEqual(found, dotnetHostHelper.TryGetDotnetPathByArchitecture(targetArchitecture, out string muxerPath));
            Assert.AreEqual(found ? dotnetMuxer : null, muxerPath);
        }

        [DataTestMethod]
        [DataRow(PlatformArchitecture.X64, PlatformArchitecture.X64, "/usr/local/share/dotnet", "", true)]
        [DataRow(PlatformArchitecture.X64, PlatformArchitecture.ARM64, "/usr/local/share/dotnet/x64", "", true)]
        [DataRow(PlatformArchitecture.ARM64, PlatformArchitecture.X64, "/usr/local/share/dotnet", "", true)]
        [DataRow(PlatformArchitecture.X64, PlatformArchitecture.X64, "/usr/local/share/dotnet", "", false)]
        public void GetDotnetPathByArchitecture_DefaultInstallation_Mac(PlatformArchitecture targetArchitecture, PlatformArchitecture platformArchitecture, string expectedFolder, string subfolder, bool found)
        {
            // Arrange
            string dotnetMuxer = muxerHelper.RenameMuxerAndReturnPath(PlatformOperatingSystem.OSX, targetArchitecture, subfolder);
            this.environmentHelper.SetupGet(x => x.OperatingSystem).Returns(PlatformOperatingSystem.OSX);
            this.environmentHelper.Setup(x => x.Architecture).Returns(platformArchitecture);
            string expectedMuxerPath = Path.Combine(expectedFolder, "dotnet");
            if (found)
            {
                this.fileHelper.Setup(x => x.Exists(expectedMuxerPath)).Returns(true);
                this.fileHelper.Setup(x => x.GetStream(expectedMuxerPath, FileMode.Open, FileAccess.Read)).Returns(new MemoryStream(Encoding.UTF8.GetBytes(Path.GetDirectoryName(dotnetMuxer))));
                this.fileHelper.Setup(x => x.GetStream(expectedMuxerPath, FileMode.Open, FileAccess.Read)).Returns(File.OpenRead(dotnetMuxer));
            }

            //Act & Assert
            var dotnetHostHelper = new DotnetHostHelper(fileHelper.Object, environmentHelper.Object, windowsRegistrytHelper.Object, environmentVariableHelper.Object, processHelper.Object);
            Assert.AreEqual(found, dotnetHostHelper.TryGetDotnetPathByArchitecture(targetArchitecture, out string muxerPath));
            Assert.AreEqual(found ? expectedMuxerPath : null, muxerPath);
        }

        public void Dispose() => this.muxerHelper.Dispose();


        class MockMuxerHelper : IDisposable
        {
            private static string DotnetMuxerWinX86 = "TestAssets/dotnetWinX86.exe";
            private static string DotnetMuxerWinX64 = "TestAssets/dotnetWinX64.exe";
            private static string DotnetMuxerWinArm64 = "TestAssets/dotnetWinArm64.exe";
            private static string DotnetMuxerMacArm64 = "TestAssets/dotnetMacArm64";
            private static string DotnetMuxerMacX64 = "TestAssets/dotnetMacX64";
            private readonly List<string> muxers = new List<string>();

            public MockMuxerHelper()
            {
                Assert.IsTrue(File.Exists(DotnetMuxerWinX86));
                Assert.IsTrue(File.Exists(DotnetMuxerWinX64));
                Assert.IsTrue(File.Exists(DotnetMuxerWinArm64));
                Assert.IsTrue(File.Exists(DotnetMuxerMacArm64));
                Assert.IsTrue(File.Exists(DotnetMuxerMacX64));
            }

            public string RenameMuxerAndReturnPath(PlatformOperatingSystem platform, PlatformArchitecture architecture, string subfolder = "")
            {
                string tmpDirectory = Path.GetTempPath();
                string muxerPath;
                switch (platform)
                {
                    case PlatformOperatingSystem.Windows:
                        {
                            muxerPath = Path.Combine(tmpDirectory, Guid.NewGuid().ToString("N"), subfolder, "dotnet.exe");
                            Directory.CreateDirectory(Path.GetDirectoryName(muxerPath));
                            if (architecture == PlatformArchitecture.ARM64)
                            {
                                File.Copy(DotnetMuxerWinArm64, muxerPath);
                                break;
                            }
                            else if (architecture == PlatformArchitecture.X64)
                            {
                                File.Copy(DotnetMuxerWinX64, muxerPath);
                                break;
                            }
                            else if (architecture == PlatformArchitecture.X86)
                            {
                                File.Copy(DotnetMuxerWinX86, muxerPath);
                                break;
                            }

                            throw new NotSupportedException($"Unsupported architecture '{architecture}'");
                        }
                    case PlatformOperatingSystem.OSX:
                        {
                            muxerPath = Path.Combine(tmpDirectory, Guid.NewGuid().ToString("N"), subfolder, "dotnet");
                            Directory.CreateDirectory(Path.GetDirectoryName(muxerPath));
                            if (architecture == PlatformArchitecture.ARM64)
                            {
                                File.Copy(DotnetMuxerMacArm64, muxerPath);
                                break;
                            }
                            else if (architecture == PlatformArchitecture.X64)
                            {
                                File.Copy(DotnetMuxerMacX64, muxerPath);
                                break;
                            }

                            throw new NotSupportedException($"Unsupported architecture '{architecture}'");
                        }
                    case PlatformOperatingSystem.Unix:
                    default:
                        throw new NotSupportedException($"Unsupported OS '{platform}'");
                }

                muxers.Add(muxerPath);
                return muxerPath;
            }

            public void Dispose()
            {
                foreach (var muxer in muxers)
                {
                    Directory.Delete(Path.GetDirectoryName(muxer), true);
                }
            }
        }
    }
}
