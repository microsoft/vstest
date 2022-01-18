// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NET451

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;

    // This tests need specific sdks to be installed on arm machine
    // >= ARM 6.0.2xx
    // >= x64 6.0.2xx
    // x64 5.0.4xx for Mac
    // x64 3.1.4XX for Win
    // Manual test './tools/.../dotnet test ./test/Microsoft.TestPlatform.AcceptanceTests/bin/Debug/netcoreapp2.1/Microsoft.TestPlatform.AcceptanceTests.dll --testcasefilter:"DotnetArchitectureSwitchTests"'
    [TestClass]
    [Ignore("Manual tests(for now). Tests in this class need some .NET SDK global installations")]
    public class DotnetArchitectureSwitchTests : AcceptanceTestBase
    {
        private static string privateX64Installation;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            privateX64Installation = Path.Combine(GetResultsDirectory(), "x64");
            CopyAll(new DirectoryInfo(GetX64InstallationFolder), new DirectoryInfo(privateX64Installation));
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            try
            {
                Directory.Delete(privateX64Installation, true);
            }
            catch
            {

            }
        }

        [TestMethod]
        public void GlobalInstallation()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return;
            }

            var projectName = "ArchitectureSwitch.csproj";
            var projectPath = this.GetProjectFullPath(projectName);
            var projectDirectory = Path.GetDirectoryName(projectPath);

            var env = new Dictionary<string, string>
            {
                { "DOTNET_ROOT", null },
                { "DOTNET_MULTILEVEL_LOOKUP", "0" }
            };

            // Verify native architecture
            ExecuteApplication(GetDefaultDotnetMuxerLocation, $"test {projectPath} --framework net6.0", out string stdOut, out string stdError, out int exitCode, env, projectDirectory);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Assert.IsTrue(stdOut.Contains("Runtime location: /usr/local/share/dotnet/shared/Microsoft.NETCore.App"));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.IsTrue(stdOut.Contains($@"Runtime location: {Environment.ExpandEnvironmentVariables("%ProgramFiles%")}\dotnet\shared\Microsoft.NETCore.App"));
            }
            Assert.IsTrue(stdOut.Contains("OSArchitecture: Arm64"));
            Assert.IsTrue(stdOut.Contains("ProcessArchitecture: Arm64"));


            // Verify switch using csproj
            ExecuteApplication(GetDefaultDotnetMuxerLocation, $"test {projectPath} --framework net6.0 --arch x64", out stdOut, out stdError, out exitCode, env, projectDirectory);
            AssertSwitch(stdOut);

            // Verify switch using test container
            var buildAssemblyPath = GetAssetFullPath("ArchitectureSwitch.dll", "net6.0");
            ExecuteApplication(GetDefaultDotnetMuxerLocation, $"test {buildAssemblyPath} --arch x64", out stdOut, out stdError, out exitCode, env, projectDirectory);
            AssertSwitch(stdOut);

            void AssertSwitch(string output)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Assert.IsTrue(output.Contains("Runtime location: /usr/local/share/dotnet/x64/shared/Microsoft.NETCore.App"));
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Assert.IsTrue(output.Contains($@"Runtime location: {Environment.ExpandEnvironmentVariables("%ProgramFiles%")}\dotnet\x64\shared\Microsoft.NETCore.App"));
                }
                Assert.IsTrue(output.Contains("OSArchitecture: X64"));
                Assert.IsTrue(output.Contains("ProcessArchitecture: X64"));
            }
        }

        [TestMethod]
        [DataRow(true, false)]
        [DataRow(false, true)]
        public void DOTNET_ROOTS_EnvironmentVariables(bool dotnetRoot, bool dotnetRootX64)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return;
            }

            var env = new Dictionary<string, string>
            {
                ["DOTNET_ROOT"] = null,
                ["DOTNET_MULTILEVEL_LOOKUP"] = "0"
            };

            var projectName = "ArchitectureSwitch.csproj";
            var projectPath = this.GetProjectFullPath(projectName);
            var projectDirectory = Path.GetDirectoryName(projectPath);

            // Verify native architecture
            ExecuteApplication(GetDefaultDotnetMuxerLocation, $"test {projectPath} --framework net6.0", out string stdOut, out string stdError, out int exitCode, env, projectDirectory);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Assert.IsTrue(stdOut.Contains("Runtime location: /usr/local/share/dotnet/shared/Microsoft.NETCore.App"), "Unexpected runtime location");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.IsTrue(stdOut.Contains($@"Runtime location: {Environment.ExpandEnvironmentVariables("%ProgramFiles%")}\dotnet\shared\Microsoft.NETCore.App"));
            }
            Assert.IsTrue(stdOut.Contains("OSArchitecture: Arm64"), "Unexpected OSArchitecture");
            Assert.IsTrue(stdOut.Contains("ProcessArchitecture: Arm64"), "Unexpected ProcessArchitecture");

            env.Clear();
            env["DOTNET_ROOT"] = null;
            env["DOTNET_MULTILEVEL_LOOKUP"] = "0";
            if (dotnetRoot)
            {
                env["DOTNET_ROOT"] = privateX64Installation;
            }

            if (dotnetRootX64)
            {
                env.Add("DOTNET_ROOT_X64", privateX64Installation);
            }

            // Verify switch using csproj
            ExecuteApplication($"{privateX64Installation}/{GetMuxerName}", $"test {projectPath} --framework net6.0 --arch x64", out stdOut, out stdError, out exitCode, env, projectDirectory);
            AssertSwitch(stdOut);

            // Verify switch using test container
            var buildAssemblyPath = GetAssetFullPath("ArchitectureSwitch.dll", "net6.0");
            ExecuteApplication($"{privateX64Installation}/{GetMuxerName}", $"test {buildAssemblyPath} --framework net6.0 --arch x64", out stdOut, out stdError, out exitCode, env, projectDirectory);
            AssertSwitch(stdOut);

            void AssertSwitch(string output)
            {
                Assert.IsTrue(Regex.IsMatch(output.Replace(@"\", "/"), $"Runtime location: .*{privateX64Installation.Replace(@"\", "/")}.*shared.*Microsoft.NETCore.App"), "Unexpected runtime location");
                Assert.IsTrue(output.Contains("OSArchitecture: X64"), "Unexpected OSArchitecture");
                Assert.IsTrue(output.Contains("ProcessArchitecture: X64"), "Unexpected ProcessArchitecture");
                Assert.IsTrue(dotnetRoot ? output.Contains($"DOTNET_ROOT: {privateX64Installation}") : true, "Unexpected DOTNET_ROOT var");
                Assert.IsTrue(dotnetRootX64 ? output.Contains($"DOTNET_ROOT_X64: {privateX64Installation}") : true, "Unexpected DOTNET_ROOT_X64 var");
            }
        }

        [TestMethod]
        public void PrivateX64BuildToGlobalArmInstallation()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return;
            }

            var env = new Dictionary<string, string>
            {
                ["DOTNET_ROOT"] = null,
                ["DOTNET_MULTILEVEL_LOOKUP"] = "0"
            };
            string privateInstallationMuxer = Path.Combine(privateX64Installation, GetMuxerName);

            var projectName = "ArchitectureSwitch.csproj";
            var projectPath = this.GetProjectFullPath(projectName);
            var projectDirectory = Path.GetDirectoryName(projectPath);

            // Verify native architecture
            ExecuteApplication(privateInstallationMuxer, $"test {projectPath} --framework net6.0", out string stdOut, out string stdError, out int exitCode, env, projectDirectory);
            Assert.IsTrue(Regex.IsMatch(stdOut.Replace(@"\", "/"), $"Runtime location: .*{privateX64Installation.Replace(@"\", "/")}.*shared.*Microsoft.NETCore.App"), "Unexpected runtime location");
            Assert.IsTrue(stdOut.Contains("OSArchitecture: X64"), "Unexpected OSArchitecture");
            Assert.IsTrue(stdOut.Contains("ProcessArchitecture: X64"), "Unexpected ProcessArchitecture");

            // Verify switch using csproj
            ExecuteApplication($"{privateX64Installation}/{GetMuxerName}", $"test {projectPath} --framework net6.0 --arch arm64", out stdOut, out stdError, out exitCode, env, projectDirectory);
            AssertSwitch(stdOut);

            // Verify switch using test container
            var buildAssemblyPath = GetAssetFullPath("ArchitectureSwitch.dll", "net6.0");
            ExecuteApplication($"{privateX64Installation}/{GetMuxerName}", $"test {buildAssemblyPath} --framework net6.0 --arch arm64", out stdOut, out stdError, out exitCode, env, projectDirectory);
            AssertSwitch(stdOut);

            void AssertSwitch(string output)
            {
                Assert.IsTrue(Regex.IsMatch(output.Replace(@"\", "/"), $"Runtime location: .*{GetDefaultLocation.Replace(@"\", "/")}.*shared.*Microsoft.NETCore.App"), "Unexpected runtime location");
                Assert.IsTrue(output.Contains("OSArchitecture: Arm64"), "Unexpected OSArchitecture");
                Assert.IsTrue(output.Contains("ProcessArchitecture: Arm64"), "Unexpected ProcessArchitecture");
            }
        }

        [TestMethod]
        [DataRow(true, false)]
        [DataRow(false, true)]
        public void PrivateX64BuildToDOTNET_ROOTS_EnvironmentVariables(bool dotnetRoot, bool dotnetRootARM64)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return;
            }

            var env = new Dictionary<string, string>
            {
                ["DOTNET_ROOT"] = null,
                ["DOTNET_MULTILEVEL_LOOKUP"] = "0"
            };
            string privateInstallationMuxer = Path.Combine(privateX64Installation, GetMuxerName);

            var projectName = "ArchitectureSwitch.csproj";
            var projectPath = this.GetProjectFullPath(projectName);
            var projectDirectory = Path.GetDirectoryName(projectPath);

            // Verify native architecture
            ExecuteApplication(privateInstallationMuxer, $"test {projectPath} --framework net6.0", out string stdOut, out string stdError, out int exitCode, env, projectDirectory);
            Assert.IsTrue(Regex.IsMatch(stdOut.Replace(@"\", "/"), $"Runtime location: .*{privateX64Installation.Replace(@"\", "/")}.*shared.*Microsoft.NETCore.App"), "Unexpected runtime location");
            Assert.IsTrue(stdOut.Contains("OSArchitecture: X64"), "Unexpected OSArchitecture");
            Assert.IsTrue(stdOut.Contains("ProcessArchitecture: X64"), "Unexpected ProcessArchitecture");

            env.Clear();
            env["DOTNET_ROOT"] = null;
            env["DOTNET_MULTILEVEL_LOOKUP"] = "0";
            if (dotnetRoot)
            {
                env["DOTNET_ROOT"] = GetDefaultLocation;
            }

            if (dotnetRootARM64)
            {
                env["DOTNET_ROOT_ARM64"] = GetDefaultLocation;
            }

            // Verify switch using csproj
            ExecuteApplication($"{privateX64Installation}/{GetMuxerName}", $"test {projectPath} --framework net6.0 --arch arm64", out stdOut, out stdError, out exitCode, env, projectDirectory);
            AssertSwitch(stdOut);

            // Verify switch using test container
            var buildAssemblyPath = GetAssetFullPath("ArchitectureSwitch.dll", "net6.0");
            ExecuteApplication($"{privateX64Installation}/{GetMuxerName}", $"test {buildAssemblyPath} --framework net6.0 --arch arm64", out stdOut, out stdError, out exitCode, env, projectDirectory);
            AssertSwitch(stdOut);

            void AssertSwitch(string output)
            {
                Assert.IsTrue(Regex.IsMatch(output.Replace(@"\", "/"), $"Runtime location: .*{GetDefaultLocation.Replace(@"\", "/")}.*shared.*Microsoft.NETCore.App"), "Unexpected runtime location");
                Assert.IsTrue(output.Contains("OSArchitecture: Arm64"), "Unexpected OSArchitecture");
                Assert.IsTrue(output.Contains("ProcessArchitecture: Arm64"), "Unexpected ProcessArchitecture");
                Assert.IsTrue(dotnetRoot ? output.Contains($"DOTNET_ROOT: {GetDefaultLocation}") : true, "Unexpected DOTNET_ROOT var");
                Assert.IsTrue(dotnetRootARM64 ? output.Contains($"DOTNET_ROOT_ARM64: {GetDefaultLocation}") : true, "Unexpected DOTNET_ROOT_ARM64 var");
            }
        }

        [TestMethod]
        public void SilentlyForceX64()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return;
            }

            var projectName = "ArchitectureSwitch.csproj";
            var projectPath = this.GetProjectFullPath(projectName);
            var projectDirectory = Path.GetDirectoryName(projectPath);

            var env = new Dictionary<string, string>
            {
                ["DOTNET_ROOT"] = null,
                ["DOTNET_MULTILEVEL_LOOKUP"] = "0"
            };

            ExecuteApplication(GetDefaultDotnetMuxerLocation, $"test {projectPath} --framework {GetFrameworkVersionToForceToX64}", out string stdOut, out string stdError, out int exitCode, env, projectDirectory);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Assert.IsTrue(stdOut.Contains("Runtime location: /usr/local/share/dotnet/x64/shared/Microsoft.NETCore.App"));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.IsTrue(stdOut.Contains($@"Runtime location: {Environment.ExpandEnvironmentVariables("%ProgramFiles%")}\dotnet\x64\shared\Microsoft.NETCore.App"));
            }
            Assert.IsTrue(stdOut.Contains("OSArchitecture: X64"));
            Assert.IsTrue(stdOut.Contains("ProcessArchitecture: X64"));
        }

        private static string GetMuxerName => RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "dotnet" : "dotnet.exe";

        private static string GetX64InstallationFolder => RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ?
            "/usr/local/share/dotnet/x64" :
            $@"{Environment.ExpandEnvironmentVariables("%ProgramFiles%")}\dotnet\x64";

        private static string GetFrameworkVersionToForceToX64 => RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ?
            "net5.0" :
            "netcoreapp3.1";

        private static string GetDefaultLocation => RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ?
            $"/usr/local/share/dotnet" :
            $@"{Environment.ExpandEnvironmentVariables("%ProgramFiles%")}\dotnet";

        private static string GetDefaultDotnetMuxerLocation => RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ?
            $"{GetDefaultLocation}/{GetMuxerName}" :
            $@"{GetDefaultLocation}\{GetMuxerName}";

        private static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }
    }
}

#endif
