// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;

using Microsoft.TestPlatform.TestUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.AcceptanceTests;

/// <summary>
/// Regression test for https://github.com/microsoft/vstest/issues/16151.
///
/// When <c>vstest.console.exe</c> (the .NET Framework console) is invoked directly - e.g. by
/// Visual Studio or by hand - rather than through <c>dotnet test</c>, the SDK does not provide
/// the <c>VSTEST_DOTNET_ROOT_PATH</c> hint that normally disambiguates the runtime location.
///
/// If the surrounding environment has an architecture-less <c>DOTNET_ROOT</c> pointing at an x64
/// installation and an x86 test host is launched (<c>testhost.x86.exe</c>), the x86 apphost would,
/// without the fix, pick up that architecture-less <c>DOTNET_ROOT</c> and try to load the x64
/// <c>hostfxr.dll</c> into the 32-bit process, failing with <c>0x800700C1</c>
/// (<c>ERROR_BAD_EXE_FORMAT</c>) before any test runs.
///
/// With the fix, <c>DotnetTestHostManager</c> promotes the architecture-less <c>DOTNET_ROOT</c> to
/// the architecture specific <c>DOTNET_ROOT_X64</c> it actually points at and hides the ambiguous
/// <c>DOTNET_ROOT</c> from the testhost, which then resolves the (globally installed) x86 runtime
/// and runs the tests normally.
/// </summary>
[TestClass]
// testhost.x86.exe and the apphost runtime resolution this test exercises only exist on Windows.
[TestCategory("Windows-Review")]
public class DotnetRootArchitectureMismatchTests : AcceptanceTestBase
{
    [TestMethod]
    public void RunningX86NetTestThroughVsTestConsoleExeWithX64DotnetRootResolvesX86Runtime()
    {
        // Desktop runner => vstest.console.exe (.NET Framework) is invoked directly, which is the
        // scenario where the SDK does not pass VSTEST_DOTNET_ROOT_PATH. Target net11.0 + /Platform:x86
        // makes vstest launch testhost.x86.exe, an architecture specific apphost that resolves its
        // runtime from the DOTNET_ROOT* environment variables.
        SetTestEnvironment(_testEnvironment, new RunnerInfo
        {
            RunnerFramework = DesktopRunnerFramework,
            TargetFramework = Core11TargetFramework,
            InIsolationValue = InIsolation,
        });

        // The x64 installation we point the architecture-less DOTNET_ROOT at. ".dotnet/dotnet.exe"
        // is the x64 muxer installed by the build, so its parent is a real x64 DOTNET_ROOT.
        var x64DotnetRoot = Path.GetDirectoryName(GetDownloadedDotnetMuxerFromTools("X64"))!;

        var arguments = PrepareArguments(
            GetTestDllForFramework("SimpleTestProject.dll", Core11TargetFramework, automaticallyResolveCompatibilityTestAsset: false),
            GetTestAdapterPath(),
            string.Empty,
            FrameworkArgValue,
            _testEnvironment.InIsolationValue,
            resultsDirectory: TempDirectory.Path);
        arguments = string.Concat(arguments, " /Platform:x86");

        var environmentVariables = new Dictionary<string, string?>
        {
            // The only DOTNET_ROOT* variable that is set, and it points at an x64 install. This is
            // exactly the ambiguous configuration from issue #16151.
            ["DOTNET_ROOT"] = x64DotnetRoot,

            // Clear any architecture specific overrides that CI may have set so they cannot mask the
            // bug. Without these cleared the x86 apphost would honor DOTNET_ROOT_X86 / DOTNET_ROOT(x86)
            // and never fall through to the mismatched architecture-less DOTNET_ROOT.
            ["DOTNET_ROOT_X86"] = null,
            ["DOTNET_ROOT(x86)"] = null,

            // Ensure we exercise the direct vstest.console.exe invocation path and not the SDK
            // (dotnet test) path that already disambiguates the runtime for us.
            ["VSTEST_DOTNET_ROOT_PATH"] = null,
        };

        InvokeVsTest(arguments, environmentVariables);

        // SimpleTestProject has 1 passing, 1 failing and 1 skipped test. Getting a complete summary
        // proves the x86 testhost resolved its runtime and actually executed the tests; before the
        // fix the testhost crashed with 0x800700C1 and produced no summary at all.
        ValidateSummaryStatus(1, 1, 1);
    }
}
