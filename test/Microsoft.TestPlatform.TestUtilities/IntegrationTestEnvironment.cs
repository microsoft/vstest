// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.TestUtilities
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Xml;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Provider for test environment configuration.
    /// Currently reads configuration from environment variables. We may support a
    /// different provider later. E.g. run settings.
    /// </summary>
    public class IntegrationTestEnvironment
    {
        public static string TestPlatformRootDirectory = ResolveRepositoryRoot();

        /// <summary>
        /// Resolves the repository root by searching upward for a marker that only the root carries,
        /// rather than by counting directory levels.
        /// </summary>
        /// <remarks>
        /// This file is the anchor every other path in the integration suite is built from, and it
        /// used to resolve that anchor by counting: Path.GetFullPath(@"..\..\..\..\..").
        /// <para>
        /// A counted walk encodes the DEPTH of the output directory, so it silently resolves to the
        /// wrong directory whenever that depth changes - and Arcade changes it, by inserting a
        /// &lt;ProjectName&gt; segment that the historic layout did not have. It cannot report that it
        /// was wrong, because five levels up from anywhere is still a valid path. The same defect in
        /// two leaf tests produced a DirectoryNotFoundException naming an artifacts path that never
        /// existed, and a sibling that swallowed it and asserted on Actual:&lt;(null)&gt;.
        /// </para>
        /// <para>
        /// Searching for a marker is depth-independent by construction, and when it fails it fails
        /// LOUDLY, naming what it looked for and where it started. A wrong path that exists is worse
        /// than no path at all: it is the form of this defect that reaches an assertion.
        /// </para>
        /// <para>
        /// TP_ROOT_DIR is still authoritative when set - CI sets it, and this must not second-guess
        /// it. It is tested with IsNullOrEmpty rather than a null check, so the resolution does not
        /// depend on whether an empty environment variable reads back as null or as "". Two
        /// measurements of that API on different runtimes disagreed; treating empty and unset the
        /// same makes the disagreement irrelevant instead of picking a side.
        /// </para>
        /// </remarks>
        private static string ResolveRepositoryRoot()
        {
            var fromEnvironment = Environment.GetEnvironmentVariable("TP_ROOT_DIR");
            if (!string.IsNullOrEmpty(fromEnvironment))
            {
                return fromEnvironment;
            }

            // Anchored at the assembly rather than the current directory. The counted walk used
            // Path.GetFullPath on a relative path, which resolves against the PROCESS working
            // directory - so its answer depended on how the runner was launched, not on where the
            // test binary lives. The current directory is still tried as a second start point,
            // because some hosts run tests from the repository root.
            const string RootMarker = "TestPlatform.sln";
            var startingPoints = new List<string>();

            var assemblyPath = typeof(IntegrationTestEnvironment).GetTypeInfo().Assembly.Location;
            if (!string.IsNullOrEmpty(assemblyPath))
            {
                startingPoints.Add(Path.GetDirectoryName(assemblyPath));
            }

            startingPoints.Add(Directory.GetCurrentDirectory());

            foreach (var start in startingPoints)
            {
                var directory = string.IsNullOrEmpty(start) ? null : new DirectoryInfo(start);
                while (directory != null)
                {
                    if (File.Exists(Path.Combine(directory.FullName, RootMarker)))
                    {
                        return directory.FullName;
                    }

                    directory = directory.Parent;
                }
            }

            throw new InvalidOperationException(
                $"Could not locate the repository root. Searched for '{RootMarker}' in every parent of: " +
                $"{string.Join(" and ", startingPoints)}. Set TP_ROOT_DIR to the repository root to resolve it explicitly.");
        }

        private static Dictionary<string, string> dependencyVersions;

        private string targetRuntime;

        public IntegrationTestEnvironment()
        {
            // These environment variables are set in scripts/test.ps1 or scripts/test.sh.
            this.TargetFramework = Environment.GetEnvironmentVariable("TPT_TargetFramework");
            this.TargetRuntime = Environment.GetEnvironmentVariable("TPT_TargetRuntime");

            // If the variables are not set, valid defaults are assumed.
            if (string.IsNullOrEmpty(this.TargetFramework))
            {
                // Run integration tests for net451 by default.
                this.TargetFramework = "net451";
            }

            // The second copy of the counted walk used to be here, inside
            //     if (string.IsNullOrEmpty(TestPlatformRootDirectory)) { ... }
            // It is gone, and deliberately not replaced with a call to the resolver.
            //
            // ResolveRepositoryRoot already returns a non-empty path or throws, so re-testing its
            // result here could only ever be dead code - and dead code shaped like initialisation is
            // an invitation to add real initialisation that never runs.
            //
            // 15.9 carried the more interesting form of this: their block ASSIGNS the public static
            // it just tested, so it is taken by the FIRST instance constructed in the process and by
            // no other. Anything derived from that condition is order-dependent rather than
            // environment-dependent, which is why they removed a "how was I launched" flag that was
            // derived from it.

            this.TestAssetsPath = Path.Combine(TestPlatformRootDirectory, $@"test{Path.DirectorySeparatorChar}TestAssets");

            // Where the test assets are actually BUILT, as opposed to where their sources live.
            // Arcade puts every project's output under artifacts\bin\<ProjectName>\<config>\<tfm>,
            // so the historic test\TestAssets\<name>\bin\<config>\<tfm> convention no longer names
            // anything. See the remark on GetTestAsset.
            this.TestAssetsBinPath = Path.Combine(TestPlatformRootDirectory, "artifacts", "bin");

            // There is an assumption that integration tests will always run from a source enlistment.
            // Need to remove this assumption when we move to a CDP.
            //
            // Under Arcade the restore cache is NUGET_PACKAGES, not <root>\packages, so the original
            // path does not exist and asset lookups fail with "GetNugetPackage: Directory not found".
            // Both layouts are <id>\<version>\..., the global folder differing only by lower-casing
            // the package id, which does not matter on Windows. The original path is kept as the
            // fallback so this works under the old script as well as under Arcade.
            var nugetPackages = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
            this.PackageDirectory = !string.IsNullOrEmpty(nugetPackages) && Directory.Exists(nugetPackages)
                ? nugetPackages
                : Path.Combine(TestPlatformRootDirectory, @"packages");
            this.ToolsDirectory = Path.Combine(TestPlatformRootDirectory, @"tools");
            this.TestArtifactsDirectory = Path.Combine(TestPlatformRootDirectory, "artifacts", "testArtifacts");
            this.RunnerFramework = "net451";
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
        {
            get
            {
                if (dependencyVersions == null)
                {
                    dependencyVersions = GetDependencies(TestPlatformRootDirectory);
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
                // this used to switch to src\package\package\bin\based on whether 
                // this is running in cli, but that's a bad idea, the console there does not have 
                // a runtime config and will fail to start with error testhostpolicy.dll not found
                var publishDirectory = Path.Combine(
                    TestPlatformRootDirectory,
                    "artifacts",
                    BuildConfiguration,
                    this.RunnerFramework,
                    this.TargetRuntime);

                if (!Directory.Exists(publishDirectory))
                {
                    throw new InvalidOperationException($"Path '{publishDirectory}' does not exist, did you build the solution via build.cmd?");
                }

                // PRESENCE IS NOT USABILITY, and on this tree that is measured rather than feared.
                //
                // Directory.Exists asks whether a directory is THERE, not whether it is the one that
                // works. Under artifacts\Release there are directories with the same shape as the
                // publish output that cannot run a test:
                //
                //   net451\win7-x64   228 files   runner yes   provider yes    usable
                //   netcoreapp2.1      38 files   runner yes   provider yes    usable
                //   net45\win7-x64      9 files   runner NO    provider NO     Directory.Exists: yes
                //   netcoreapp1.0       7 files   runner NO    provider NO     Directory.Exists: yes
                //
                // THE CASE THAT MAKES THE PROVIDER CHECK LOAD-BEARING is not in that list, because
                // all four of those fail on the runner too - a runner-only check would reject them
                // and the provider requirement would look redundant. This one does not:
                //
                //   artifacts\bin\package\Release\net451\win7-x64   runner YES   provider NO
                //
                // Raw compiler output. It exists on this tree, it holds vstest.console.exe, and it
                // has no Extensions\ - so it passes both Directory.Exists AND a runner-only probe,
                // and cannot run a test. 15.9 shipped a fix gated on the runner alone and it accepted
                // exactly this directory; theirs was saved only by trying the good candidate first.
                //
                // Population check of the predicate below. The FIRST version of this check scanned
                // artifacts\Release and reported 139 candidates with 2 accepted - and it would have
                // shown the provider requirement doing nothing at all, because ZERO of the
                // discriminating directories live in that subtree. Scoping a population to the tree
                // you expect the answer in is the same defect as filtering before listing, one level
                // up, and this comment committed it while naming it.
                //
                // Over every directory under artifacts\ - 9,667 of them:
                //
                //   runner + provider        ACCEPT             2   the two usable layouts
                //   runner YES, provider NO  DISCRIMINATING    12   where the provider check works
                //   provider YES, runner NO                     2
                //   neither                                9,651
                //   2 + 12 + 2 + 9,651 = 9,667
                //
                // The 12 are under artifacts\bin\, artifacts\obj\ and artifacts\tmp\ContainerSigning\
                // - raw build output of vstest.console and its test projects, and signing staging.
                // 0 of the 12 are under artifacts\Release. 15.9 measured 13 on their line, in the
                // same three subtrees, having started from the same too-narrow scope.
                //
                // Returning an unusable directory would start vstest.console from a directory holding
                // the executable and not the things it needs, and the run fails with a PRODUCT
                // message - "No suitable test runtime provider found for this run", or on the older
                // layout "testhostpolicy.dll not found". Both name the product's state and hide that
                // the wrong directory was chosen. They share no searchable token, so someone
                // investigating one would never find the other. 15.9 lost hours to exactly that,
                // reading it as a product failure across three CI builds.
                //
                // So the requirement is the runner AND the provider it cannot start without, and the
                // failure names which one was missing - so "not built" and "built into a layout that
                // cannot run" are distinguishable rather than both reading as absence.
                var hasRunner =
                    File.Exists(Path.Combine(publishDirectory, "vstest.console.exe")) ||
                    File.Exists(Path.Combine(publishDirectory, "vstest.console.dll"));
                var providerPath = Path.Combine(publishDirectory, "Extensions", "Microsoft.TestPlatform.TestHostRuntimeProvider.dll");

                if (!hasRunner || !File.Exists(providerPath))
                {
                    throw new InvalidOperationException(
                        $"Path '{publishDirectory}' exists but is not a usable publish layout: " +
                        $"vstest.console.exe or vstest.console.dll {(hasRunner ? "found" : "NOT FOUND")}, " +
                        $"Extensions\\Microsoft.TestPlatform.TestHostRuntimeProvider.dll {(File.Exists(providerPath) ? "found" : "NOT FOUND")}. " +
                        "A directory that merely exists is not the one that runs - did you build the solution via build.cmd?");
                }

                return publishDirectory;
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
                if (this.RunnerFramework == IntegrationTestBase.DesktopRunnerFramework)
                {
                    if (string.IsNullOrEmpty(this.targetRuntime))
                    {

                        this.targetRuntime = "win7-x64";
                    }
                }
                else
                {
                    this.targetRuntime = "";
                }

                return this.targetRuntime;
            }
            set
            {
                this.targetRuntime = value;
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
        /// Gets the root directory the test assets are BUILT into, as opposed to
        /// <see cref="TestAssetsPath"/>, which is where their sources live. Under Arcade this is
        /// artifacts\bin, and each project's output is at
        /// artifacts\bin\&lt;ProjectName&gt;\&lt;config&gt;\&lt;tfm&gt;.
        /// </summary>
        public string TestAssetsBinPath
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
        /// produce <c>artifacts\bin\SimpleUnitTest\Debug\net451\SimpleUnitTest.dll</c>
        /// </remarks>
        public string GetTestAsset(string assetName)
        {
            return GetTestAsset(assetName, this.TargetFramework);
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
        /// produce <c>artifacts\bin\SimpleUnitTest\Debug\net451\SimpleUnitTest.dll</c>
        ///
        /// The output path is composed from <see cref="TestAssetsBinPath"/> first and from
        /// <see cref="TestAssetsPath"/> as a fallback, because BOTH layouts are produced. Arcade
        /// inserts a project-name segment and writes to
        /// artifacts\bin\&lt;ProjectName&gt;\&lt;config&gt;\&lt;tfm&gt;, which is where every
        /// ordinary asset project lands. Projects that set their own relative OutputPath defeat
        /// that redirection and keep the historic
        /// test\TestAssets\&lt;name&gt;\bin\&lt;config&gt;\&lt;tfm&gt; layout; CILProject.proj is
        /// one, with &lt;OutputPath&gt;bin\$(Configuration)&lt;/OutputPath&gt;. The sources always
        /// live under <see cref="TestAssetsPath"/>, which is why GetTestProject uses it directly.
        /// </remarks>
        public string GetTestAsset(string assetName, string targetFramework)
        {
            var simpleAssetName = Path.GetFileNameWithoutExtension(assetName);

            // Arcade layout, used by every asset project that does not override OutputPath.
            var arcadePath = Path.Combine(
                this.TestAssetsBinPath,
                simpleAssetName,
                BuildConfiguration,
                targetFramework,
                assetName);

            if (File.Exists(arcadePath))
            {
                return arcadePath;
            }

            // Historic layout, still produced by asset projects that set their own relative
            // OutputPath. CILProject.proj is one: it sets <OutputPath>bin\$(Configuration)</OutputPath>,
            // which is relative to the project and therefore defeats the artifacts redirection.
            var historicPath = Path.Combine(
                this.TestAssetsPath,
                simpleAssetName,
                "bin",
                BuildConfiguration,
                targetFramework,
                assetName);

            // Both candidates are named on failure on purpose. The previous version reported only
            // the one path it composed, which made "the asset was not built" and "the resolver
            // looked in the wrong place" indistinguishable - and they were confused, for 6 failures.
            Assert.IsTrue(
                File.Exists(historicPath),
                "GetTestAsset: Path not found. Looked in the Arcade output layout at {0} and in the historic layout at {1}.",
                arcadePath,
                historicPath);

            return historicPath;
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
            var dependencyPropsFile = Path.Combine(testPlatformRoot, @"scripts\build\TestPlatform.Dependencies.props".Replace('\\', Path.DirectorySeparatorChar));
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
                this.TestAssetsPath,
                simpleAssetName,
                assetName);

            Assert.IsTrue(File.Exists(assetPath), "GetTestAsset: Path not found: {0}.", assetPath);

            return assetPath;
        }
    }
}