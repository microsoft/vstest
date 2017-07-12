// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.TestUtilities
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Provider for test environment configuration.
    /// Currently reads configuration from environment variables. We may support a
    /// different provider later. E.g. run settings.
    /// </summary>
    public class IntegrationTestEnvironment
    {
        private static Dictionary<string, string> dependencyVersions;

        private readonly string testPlatformRootDirectory;
        private readonly bool runningInCli;

        public IntegrationTestEnvironment()
        {
            // These environment variables are set in scripts/test.ps1 or scripts/test.sh.
            this.testPlatformRootDirectory = Environment.GetEnvironmentVariable("TP_ROOT_DIR");
            this.TargetFramework = Environment.GetEnvironmentVariable("TPT_TargetFramework");
            this.TargetRuntime = Environment.GetEnvironmentVariable("TPT_TargetRuntime");

            // If the variables are not set, valid defaults are assumed.
            if (string.IsNullOrEmpty(this.TargetFramework))
            {
                // Run integration tests for net451 by default.
                this.TargetFramework = "net451";
            }

            if (string.IsNullOrEmpty(this.TargetRuntime))
            {
                this.TargetRuntime = "win7-x64";
            }

            if (string.IsNullOrEmpty(this.testPlatformRootDirectory))
            {
                // Running in VS/IDE. Use artifacts directory as root.
                this.runningInCli = false;

                // Get root directory from test assembly output directory
                this.testPlatformRootDirectory = Path.GetFullPath(@"..\..\..\..\..");
                this.TestAssetsPath = Path.Combine(this.testPlatformRootDirectory, @"test\TestAssets");
            }
            else
            {
                // Running in command line/CI
                this.runningInCli = true;
                this.TestAssetsPath = Path.Combine(this.testPlatformRootDirectory, @"test\TestAssets");
            }

            // There is an assumption that integration tests will always run from a source enlistment.
            // Need to remove this assumption when we move to a CDP.
            this.PackageDirectory = Path.Combine(this.testPlatformRootDirectory, @"packages");
            this.ToolsDirectory = Path.Combine(this.testPlatformRootDirectory, @"tools");
            this.RunnerFramework = "net451";
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

        public Dictionary<string, string> DependencyVersions
        {
            get
            {
                if (dependencyVersions == null)
                {
                    dependencyVersions = GetDependencies(this.testPlatformRootDirectory);
                }

                return dependencyVersions;
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
            get
            {
                string value = string.Empty;
                if (this.runningInCli)
                {
                    value = Path.Combine(
                        this.testPlatformRootDirectory,
                        "artifacts",
                        this.BuildConfiguration,
                        this.RunnerFramework,
                        this.TargetRuntime);
                }
                else
                {
                    value = Path.Combine(
                    this.testPlatformRootDirectory,
                    @"src\package\package\bin",
                    this.BuildConfiguration,
                    this.RunnerFramework,
                    this.TargetRuntime);
                }

                return value;
            }
        }

        /// <summary>
        /// Gets the target framework.
        /// Supported values = <c>net451</c>, <c>netcoreapp1.0</c>.
        /// </summary>
        public string TargetFramework
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the target runtime.
        /// Supported values = <c>win7-x64</c>.
        /// </summary>
        public string TargetRuntime
        {
            get; set;
        }

        /// <summary>
        /// Gets the root directory for test assets.
        /// </summary>
        public string TestAssetsPath
        {
            get;
        }

        /// <summary>
        /// Gets the tools directory for dependent tools
        /// </summary>
        public string ToolsDirectory
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the application type.
        /// Supported values = <c>net451</c>, <c>netcoreapp1.0</c>.
        /// </summary>
        public string RunnerFramework
        {
            get;
            set;
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
        public string GetTestAsset(string assetName, string targetFramework = null)
        {
            var simpleAssetName = Path.GetFileNameWithoutExtension(assetName);
            var assetPath = Path.Combine(
                this.TestAssetsPath,
                simpleAssetName,
                "bin",
                this.BuildConfiguration,
                targetFramework??this.TargetFramework,
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

        private static Dictionary<string, string> GetDependencies(string testPlatformRoot)
        {
            var dependencyPropsFile = Path.Combine(testPlatformRoot, @"scripts\build\TestPlatform.Dependencies.props");
            var dependencyProps = new Dictionary<string, string>();
            if (!File.Exists(dependencyPropsFile))
            {
                throw new FileNotFoundException("Dependency props file not found: " + dependencyPropsFile);
            }

            using (var reader = XmlReader.Create(dependencyPropsFile))
            {
                reader.ReadToFollowing("PropertyGroup");
                using (var props = reader.ReadSubtree())
                {
                    props.MoveToContent();
                    props.Read();   // Read thru the PropertyGroup node
                    while (!props.EOF)
                    {
                        if (props.IsStartElement() && !string.IsNullOrEmpty(props.Name))
                        {
                            dependencyProps.Add(props.Name, props.ReadElementContentAsString());
                        }
                        props.Read();
                    }
                }
            }

            return dependencyProps;
        }
    }
}