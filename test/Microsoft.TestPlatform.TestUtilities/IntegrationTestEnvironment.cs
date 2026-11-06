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
        public static string TestPlatformRootDirectory = Environment.GetEnvironmentVariable("TP_ROOT_DIR");

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

            if (string.IsNullOrEmpty(TestPlatformRootDirectory))
            {
                // Running in VS/IDE, where TP_ROOT_DIR is not set. Get the root directory from the
                // test assembly output directory.
                //
                // Note that this ASSIGNS the public static it just tested, so the branch is taken
                // by the first instance in the process and by no other. Nothing may derive a
                // "how was I launched" flag from this condition - PublishDirectory used to, and
                // the result depended on construction order rather than on the environment.
                TestPlatformRootDirectory = Path.GetFullPath(@"..\..\..\..\..");
            }

            this.TestAssetsPath = Path.Combine(TestPlatformRootDirectory, @"test\TestAssets");

            // There is an assumption that integration tests will always run from a source enlistment.
            // Need to remove this assumption when we move to a CDP.
            //
            // The old build restored packages into <root>\packages with nuget.exe -PackagesDirectory,
            // which is why that path is hard-coded below. Arcade restores to the NuGet global packages
            // folder instead - NUGET_PACKAGES, which eng\common\tools.ps1 sets - so on an Arcade build
            // <root>\packages does not exist and every test calling GetNugetPackage fails with
            // "Directory not found". Both layouts are <id>\<version>\..., and the global folder
            // lower-cases the id, which does not matter on Windows.
            //
            // Prefer NUGET_PACKAGES when it is set and keep the old path as the fallback, so this
            // works under both build systems. This assembly is test-only: it appears in no nuspec and
            // has no entry in the shipped VSIX, so changing it cannot affect the delivered artifact.
            var nugetPackages = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
            this.PackageDirectory = !string.IsNullOrEmpty(nugetPackages) && Directory.Exists(nugetPackages)
                ? nugetPackages
                : Path.Combine(TestPlatformRootDirectory, @"packages");
            this.ToolsDirectory = Path.Combine(TestPlatformRootDirectory, @"tools");
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
                // Probe the candidate layouts in order of correctness instead of branching on
                // runningInCli.
                //
                // WHY THE BRANCH WAS WRONG: runningInCli was derived from TestPlatformRootDirectory,
                // a PUBLIC STATIC that this class's own constructor assigns when it finds it empty.
                // So only the FIRST IntegrationTestEnvironment built in a process ever saw it unset
                // and took the false branch; every later instance found the value the first one had
                // just written and took the true branch. The condition tested a field the same block
                // then populated, which makes the flag a function of construction order rather than
                // of the environment.
                //
                // MEASURED on build 3064282, Microsoft.TestPlatform.PerformanceTests:
                //   17:55:15.114  FAILED  ExecutionPerformanceTest      artifacts\bin\package\Release\...
                //   17:55:15.876  passed  DiscoveryPerformanceTest      artifacts\Release\...
                //   17:55:17.425  passed  VsTestConsolePerformanceTest  artifacts\Release\...
                //   17:55:19.415  passed  TestHostPerformanceTest       artifacts\Release\...
                //   17:55:20.961  passed  MsTestV2AdapterPerformanceTest artifacts\Release\...
                // Same class, same arguments, five different instances. The first test in the
                // assembly resolved a different runner from the other four and was the only failure,
                // in three consecutive builds, always at ~0.7s. TP_ROOT_DIR is set by the historic
                // scripts/test.ps1 and scripts/build.ps1 only, so under Arcade it is never set and
                // the flag was false for the first instance and true - untruthfully - for the rest.
                var candidates = new[]
                {
                    // The PUBLISH output. This is the only layout that carries the extensions and
                    // testhost beside vstest.console, so it is the only one that can actually run a
                    // test. The one below is raw compiler output: a runner started from there
                    // prints its banner, finds no ITestRuntimeProvider, and exits 1 with
                    // "No suitable test runtime provider found for this run." - which reads as a
                    // product failure rather than as the wrong runner having been selected.
                    Path.Combine(TestPlatformRootDirectory, "artifacts", BuildConfiguration, this.RunnerFramework, this.TargetRuntime),

                    // Arcade puts every project's output under artifacts\bin\<project>\, so the
                    // package project lands here.
                    Path.Combine(TestPlatformRootDirectory, @"artifacts\bin\package", BuildConfiguration, this.RunnerFramework, this.TargetRuntime),
                };

                var diagnostics = new List<string>();

                foreach (var candidate in candidates)
                {
                    // REQUIRE THE THING WHOSE ABSENCE CAUSED THE ORIGINAL FAILURE, not just the
                    // runner.
                    //
                    // Probing for vstest.console alone reproduces the defect this whole property was
                    // rewritten to fix. POPULATION MEASURED on this tree - listed, then filtered,
                    // rather than illustrated with hand-picked examples:
                    //
                    //   directories under artifacts\ a probe could land on   8579
                    //     runner + provider          ACCEPT                     6
                    //     runner YES, provider NO    DISCRIMINATING           13   <- the work
                    //     provider YES, runner NO                              3
                    //     neither                                           8557
                    //     sum check  6 + 13 + 3 + 8557 = 8579                 OK
                    //
                    // And the discrimination is not hypothetical for THIS candidate list. Both
                    // runner frameworks land on a discriminating directory at candidate 2:
                    //
                    //   artifacts\Release\net451\win7-x64              ACCEPT   123 files
                    //   artifacts\bin\package\Release\net451\win7-x64  runner, NO provider,  32 files
                    //   artifacts\Release\netcoreapp2.0                ACCEPT    35 files
                    //   artifacts\bin\package\Release\netcoreapp2.0    runner, NO provider
                    //
                    // Extensions\Microsoft.TestPlatform.TestHostRuntimeProvider.dll is the
                    // ITestRuntimeProvider whose absence produces exactly "No suitable test runtime
                    // provider found for this run." So a file-existence probe SELECTS THE BROKEN
                    // LAYOUT whenever the good one is absent, and fails with the product's message
                    // rather than a resolution error.
                    //
                    // The population matters and not just the examples: a control whose
                    // justification is illustrated only by cases that would ALSO fail a weaker
                    // control has not been justified. The case that has to be written down is the
                    // one separating the check that exists from the check that was almost written -
                    // here, the 13 runner-without-provider directories, two of which this candidate
                    // list can reach. Point made by 16.11, whose own table listed four directories
                    // that all failed on the runner too.
                    //
                    // Same mistake as the Directory.Exists selection this replaced, one level down:
                    // an existence test asks whether a thing is THERE, not whether it is the one
                    // that works. 16.11's tree carries a comment where the equivalent branch used to
                    // be - "the console there does not have a runtime config and will fail to start
                    // with error testhostpolicy.dll not found". Same defect, different era, a
                    // different symptom string, and they deleted the branch rather than guarding it.
                    //
                    // A THIRD CANDIDATE, src\package\package\bin, WAS REMOVED 2026-09-02. It was
                    // kept as "the historic layout, for a tree built by scripts\build.ps1", and that
                    // justification was measurably false on this branch:
                    //
                    //   Arcade RepoLayout.props:38  ArtifactsBinDir = artifacts\bin  - applies to
                    //                               EVERY project in the tree
                    //   after a full scripts\build.ps1 -s Publish,Vsix,NugetPackages run:
                    //     artifacts\bin\package\Release   EXISTS
                    //     src\package\package\bin         DOES NOT EXIST
                    //
                    // So even the historic invocation produces the Arcade layout here, and the
                    // candidate was unreachable - a path that could only ever be selected if the two
                    // above were absent, at which point it would resolve nothing. Upstream 17.14
                    // went further and removed candidate selection entirely, resolving one
                    // unconditional path, with that same comment above it as live code.
                    //
                    // Fewer candidates is the stronger fix: the defect above existed because a
                    // wrong candidate could be reached when a better one was missing. A candidate
                    // that cannot be produced is a latent version of exactly that.
                    var console = Path.Combine(candidate, "vstest.console.exe");
                    var consoleDll = Path.Combine(candidate, "vstest.console.dll");
                    var runtimeProvider = Path.Combine(candidate, "Extensions", "Microsoft.TestPlatform.TestHostRuntimeProvider.dll");

                    var hasDir = Directory.Exists(candidate);
                    var hasRunner = File.Exists(console) || File.Exists(consoleDll);
                    var hasProvider = File.Exists(runtimeProvider);

                    if (hasDir && hasRunner && hasProvider)
                    {
                        return candidate;
                    }

                    // Record WHICH requirement each candidate failed, not merely that it failed.
                    // "directory ABSENT" means the layout was never built; "runner present, provider
                    // ABSENT" means it was built into a layout that cannot run. Those have different
                    // causes and different fixes, and a bare list of paths makes them identical.
                    diagnostics.Add(string.Format(
                        "{0}{1}      directory {2}, runner {3}, runtime provider {4}",
                        candidate,
                        Environment.NewLine,
                        hasDir ? "present" : "ABSENT",
                        hasRunner ? "present" : "ABSENT",
                        hasProvider ? "present" : "ABSENT"));
                }

                // Name every place that was looked at, what was required, and which requirement each
                // one failed. The original code returned a path it had merely composed and left the
                // caller to fail later, on a message that described the product's state and not the
                // search.
                throw new DirectoryNotFoundException(
                    "Could not locate a usable vstest.console. Each candidate must contain vstest.console.exe or vstest.console.dll AND Extensions\\Microsoft.TestPlatform.TestHostRuntimeProvider.dll, because a runner without that provider starts and then fails with \"No suitable test runtime provider found for this run.\" - which names the product's state rather than the resolution mistake. Candidates:"
                    + Environment.NewLine + "  " + string.Join(Environment.NewLine + "  ", diagnostics));
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
        /// (c) Name of the test asset matches the parent directory name. E.g. <c>TestAssets\SimpleUnitTest\SimpleUnitTest.csproj</c> must
        /// produce <c>TestAssets\SimpleUnitTest\bin\Debug\net451\SimpleUnitTest.dll</c>
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
        /// produce <c>TestAssets\SimpleUnitTest\bin\Debug\net451\SimpleUnitTest.dll</c>
        /// </remarks>
        public string GetTestAsset(string assetName, string targetFramework)
        {
            var simpleAssetName = Path.GetFileNameWithoutExtension(assetName);
            var assetPath = Path.Combine(
                this.TestAssetsPath,
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