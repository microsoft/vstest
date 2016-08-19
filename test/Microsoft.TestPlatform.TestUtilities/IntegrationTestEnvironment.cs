// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.TestPlatform.TestUtilities
{
    using System;
    using System.IO;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Provider for test environment configuration.
    /// Currently reads configuration from environment variables. We may support a
    /// different provider later. E.g. run settings.
    /// </summary>
    public class IntegrationTestEnvironment
    {
        private readonly string testPlatformRootDirectory;

        public IntegrationTestEnvironment()
        {
            // These environment variables are set in scripts/test.ps1 or scripts/test.sh.
            this.testPlatformRootDirectory = Environment.GetEnvironmentVariable("TP_ROOT");
            this.TargetFramework = Environment.GetEnvironmentVariable("TPT_TargetFramework");
            this.TargetRuntime = Environment.GetEnvironmentVariable("TPT_TargetRuntime");

            // If the variables are not set, valid defaults are assumed.
            if (string.IsNullOrEmpty(this.TargetFramework))
            {
                // Run integration tests for net46 by default.
                this.TargetFramework = "net46";
            }

            if (string.IsNullOrEmpty(this.TargetRuntime))
            {
                this.TargetRuntime = "win7-x64";
            }

            if (string.IsNullOrEmpty(this.testPlatformRootDirectory))
            {
                // Running in VS/IDE. Use artifacts directory as root.
                this.testPlatformRootDirectory = @"..\..\..";
                this.TestAssetsPath = Path.Combine(this.testPlatformRootDirectory, @"artifacts\test\TestAssets");
                this.PublishDirectory = Path.Combine(
                    this.testPlatformRootDirectory,
                    "artifacts",
                    @"src\Microsoft.TestPlatform.VSIXCreator\bin",
                    this.BuildConfiguration,
                    "net461",
                    this.TargetRuntime);
            }
            else
            {
                // Running in command line/CI
                this.TestAssetsPath = Path.Combine(this.testPlatformRootDirectory, @"test\TestAssets");
                this.PublishDirectory = Path.Combine(
                    this.testPlatformRootDirectory,
                    "artifacts",
                    this.BuildConfiguration,
                    this.TargetFramework,
                    this.TargetRuntime);
            }

            // There is an assumption that integration tests will always run from a source enlistment.
            // Need to remove this assumption when we move to a CDP.
            this.PackageDirectory = Path.Combine(this.testPlatformRootDirectory, @"packages");
        }

        /// <summary>
        /// Gets the build configuration for the test run.
        /// </summary>
        public string BuildConfiguration
        {
            get
            {
#if DEBUG
                return "Debug";
#else
                return "Release";
#endif
            }
        }

        /// <summary>
        /// Gets the nuget packages directory for enlistment.
        /// </summary>
        public string PackageDirectory
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the publish directory for <c>vstest.console</c> package.
        /// </summary>
        public string PublishDirectory
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the target framework.
        /// Supported values = <c>net46</c>, <c>netcoreapp1.0</c>.
        /// </summary>
        public string TargetFramework
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the target runtime.
        /// Supported values = <c>win7-x64</c>.
        /// </summary>
        public string TargetRuntime
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the root directory for test assets.
        /// </summary>
        public string TestAssetsPath
        {
            get;
        }

        /// <summary>
        /// Gets the full path to a test asset.
        /// </summary>
        /// <param name="assetName">Name of the asset with extension. E.g. <c>SimpleUnitTest.dll</c></param>
        /// <returns>Full path to the test asset.</returns>
        /// <remarks>
        /// Test assets follow several conventions:
        /// (a) They are built for supported frameworks. See <see cref="TargetFramework"/>.
        /// (b) They are built for provided build configuration.
        /// (c) Name of the test asset matches the parent directory name. E.g. <c>TestAssets\SimpleUnitTest\SimpleUnitTest.xproj</c> must 
        /// produce <c>TestAssets\SimpleUnitTest\bin\Debug\SimpleUnitTest.dll</c>
        /// </remarks>
        public string GetTestAsset(string assetName)
        {
            var simpleAssetName = Path.GetFileNameWithoutExtension(assetName);
            var assetPath = Path.Combine(
                this.TestAssetsPath,
                simpleAssetName,
                "bin",
                this.BuildConfiguration,
                this.TargetFramework,
                assetName);

            Assert.IsTrue(File.Exists(assetPath), "GetTestAsset: Path not found: {0}.", assetPath);

            return assetPath;
        }

        /// <summary>
        /// Gets the full path to a nuget package.
        /// </summary>
        /// <param name="packageSuffix">Suffix for the nuget package.</param>
        /// <returns>Complete path to a nuget package.</returns>
        /// <remarks>GetNugetPackage("foobar") will return a path to packages\foobar.</remarks>
        public string GetNugetPackage(string packageSuffix)
        {
            var packagePath = Path.Combine(this.PackageDirectory, packageSuffix);

            Assert.IsTrue(Directory.Exists(packagePath), "GetNugetPackage: Directory not found: {0}.", packagePath);

            return packagePath;
        }

        /// <summary>
        /// Gets the path to <c>vstest.console.exe</c>.
        /// </summary>
        /// <returns>Full path to <c>vstest.console.exe</c></returns>
        public string GetConsoleRunnerPath()
        {
            var vstestConsolePath = Path.Combine(this.PublishDirectory, "vstest.console.exe");

            Assert.IsTrue(File.Exists(vstestConsolePath), "GetConsoleRunnerPath: Path not found: {0}", vstestConsolePath);

            return vstestConsolePath;
        }
    }
}