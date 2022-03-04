// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

using Microsoft.VisualStudio.TestTools.UnitTesting;

#nullable disable

namespace Microsoft.TestPlatform.TestUtilities;

/// <summary>
/// Provider for test environment configuration.
/// Currently reads configuration from environment variables. We may support a
/// different provider later. E.g. run settings.
/// </summary>
public class IntegrationTestEnvironment
{
    public static string TestPlatformRootDirectory { get; private set; } =
        Environment.GetEnvironmentVariable("TP_ROOT_DIR")
        ?? Path.GetFullPath(@"..\..\..\..\..".Replace('\\', Path.DirectorySeparatorChar));

    private static Dictionary<string, string> s_dependencyVersions;

    private string _targetRuntime;

    public IntegrationTestEnvironment()
    {
        // These environment variables are set in scripts/test.ps1 or scripts/test.sh.
        TargetFramework = Environment.GetEnvironmentVariable("TPT_TargetFramework");
        TargetRuntime = Environment.GetEnvironmentVariable("TPT_TargetRuntime");

        // If the variables are not set, valid defaults are assumed.
        if (string.IsNullOrEmpty(TargetFramework))
        {
            // Run integration tests for net451 by default.
            TargetFramework = "net451";
        }

        if (string.IsNullOrEmpty(TestPlatformRootDirectory))
        {
            // Running in VS/IDE. Use artifacts directory as root.
            // Get root directory from test assembly output directory
            TestPlatformRootDirectory = Path.GetFullPath(@"..\..\..\..\..".Replace('\\', Path.DirectorySeparatorChar));
        }

        TestAssetsPath = Path.Combine(TestPlatformRootDirectory, $@"test{Path.DirectorySeparatorChar}TestAssets");

        // There is an assumption that integration tests will always run from a source enlistment.
        // Need to remove this assumption when we move to a CDP.
        PackageDirectory = Path.Combine(TestPlatformRootDirectory, @"packages");
        ToolsDirectory = Path.Combine(TestPlatformRootDirectory, @"tools");
        TestArtifactsDirectory = Path.Combine(TestPlatformRootDirectory, "artifacts", "testArtifacts");
        RunnerFramework = "net451";
    }

    /// <summary>
    /// Gets the build configuration for the test run.
    /// </summary>
    public static string BuildConfiguration
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
        => s_dependencyVersions ??= GetDependencies(TestPlatformRootDirectory);

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
            // this used to switch to src\package\package\bin\based on whether
            // this is running in cli, but that's a bad idea, the console there does not have
            // a runtime config and will fail to start with error testhostpolicy.dll not found
            var publishDirectory = Path.Combine(
                TestPlatformRootDirectory,
                "artifacts",
                BuildConfiguration,
                RunnerFramework,
                TargetRuntime);

            return !Directory.Exists(publishDirectory)
                ? throw new InvalidOperationException($"Path '{publishDirectory}' does not exist, did you build the solution via build.cmd?")
                : publishDirectory;
        }
    }

    /// <summary>
    /// Gets the extensions directory for <c>vstest.console</c> package.
    /// </summary>
    public string ExtensionsDirectory => Path.Combine(PublishDirectory, "Extensions");

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
        get
        {
            if (RunnerFramework == IntegrationTestBase.DesktopRunnerFramework)
            {
                if (string.IsNullOrEmpty(_targetRuntime))
                {
                    _targetRuntime = "win7-x64";
                }
            }
            else
            {
                _targetRuntime = "";
            }

            return _targetRuntime;
        }
        set
        {
            _targetRuntime = value;
        }
    }

    /// <summary>
    /// Gets the inIsolation.
    /// Supported values = <c>/InIsolation</c>.
    /// </summary>
    public string InIsolationValue
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
    /// Gets the test artifacts directory.
    /// </summary>
    public string TestArtifactsDirectory
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

    // A known AzureDevOps env variable meaning we are running in CI.
    public static bool IsCI { get; } = Environment.GetEnvironmentVariable("TF_BUILD") == "True";

    /// <summary>
    /// Gets the full path to a test asset.
    /// </summary>
    /// <param name="assetName">Name of the asset with extension. E.g. <c>SimpleUnitTest.dll</c></param>
    /// <returns>Full path to the test asset.</returns>
    /// <remarks>
    /// Test assets follow several conventions:
    /// (a) They are built for supported frameworks. See <see cref="TargetFramework"/>.
    /// (b) They are built for provided build configuration.
    /// (c) Name of the test asset matches the parent directory name. E.g. <c>TestAssets\SimpleUnitTest\SimpleUnitTest.csproj</c> must
    /// produce <c>TestAssets\SimpleUnitTest\bin\Debug\net451\SimpleUnitTest.dll</c>
    /// </remarks>
    public string GetTestAsset(string assetName)
    {
        return GetTestAsset(assetName, TargetFramework);
    }

    /// <summary>
    /// Gets the full path to a test asset.
    /// </summary>
    /// <param name="assetName">Name of the asset with extension. E.g. <c>SimpleUnitTest.dll</c></param>
    /// <param name="targetFramework">asset project target framework. E.g <c>net451</c></param>
    /// <returns>Full path to the test asset.</returns>
    /// <remarks>
    /// Test assets follow several conventions:
    /// (a) They are built for supported frameworks. See <see cref="TargetFramework"/>.
    /// (b) They are built for provided build configuration.
    /// (c) Name of the test asset matches the parent directory name. E.g. <c>TestAssets\SimpleUnitTest\SimpleUnitTest.csproj</c> must
    /// produce <c>TestAssets\SimpleUnitTest\bin\Debug\net451\SimpleUnitTest.dll</c>
    /// </remarks>
    public string GetTestAsset(string assetName, string targetFramework)
    {
        var simpleAssetName = Path.GetFileNameWithoutExtension(assetName);
        var assetPath = Path.Combine(
            TestAssetsPath,
            simpleAssetName,
            "bin",
            BuildConfiguration,
            targetFramework,
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
        var packagePath = Path.Combine(PackageDirectory, packageSuffix);

        Assert.IsTrue(Directory.Exists(packagePath), "GetNugetPackage: Directory not found: {0}.", packagePath);

        return packagePath;
    }

    private static Dictionary<string, string> GetDependencies(string testPlatformRoot)
    {
        var dependencyPropsFile = Path.Combine(testPlatformRoot, @"scripts\build\TestPlatform.Dependencies.props".Replace('\\', Path.DirectorySeparatorChar));
        var dependencyProps = new Dictionary<string, string>();
        if (!File.Exists(dependencyPropsFile))
        {
            throw new FileNotFoundException("Dependency props file not found: " + dependencyPropsFile);
        }

        using (var reader = XmlReader.Create(dependencyPropsFile))
        {
            reader.ReadToFollowing("PropertyGroup");
            using var props = reader.ReadSubtree();
            props.MoveToContent();
            props.Read();   // Read thru the PropertyGroup node
            while (!props.EOF)
            {
                if (props.IsStartElement() && !string.IsNullOrEmpty(props.Name))
                {
                    if (!dependencyProps.ContainsKey(props.Name))
                    {
                        dependencyProps.Add(props.Name, props.ReadElementContentAsString());
                    }
                    else
                    {
                        dependencyProps[props.Name] = string.Join(", ", dependencyProps[props.Name], props.ReadElementContentAsString());
                    }
                }
                props.Read();
            }
        }

        return dependencyProps;
    }

    /// <summary>
    /// Gets the full path to a test asset.
    /// </summary>
    /// <param name="assetName">Name of the asset with extension. E.g. <c>SimpleUnitTest.csproj</c></param>
    /// <returns>Full path to the test asset.</returns>
    /// <remarks>
    /// Test assets follow several conventions:
    /// (a) They are built for supported frameworks. See <see cref="TargetFramework"/>.
    /// (b) They are built for provided build configuration.
    /// (c) Name of the test asset matches the parent directory name. E.g. <c>TestAssets\SimpleUnitTest\SimpleUnitTest.csproj</c> must
    /// produce <c>TestAssets\SimpleUnitTest\SimpleUnitTest.csproj</c>
    /// </remarks>
    public string GetTestProject(string assetName)
    {
        var simpleAssetName = Path.GetFileNameWithoutExtension(assetName);
        var assetPath = Path.Combine(
            TestAssetsPath,
            simpleAssetName,
            assetName);

        Assert.IsTrue(File.Exists(assetPath), "GetTestAsset: Path not found: {0}.", assetPath);

        return assetPath;
    }
}
