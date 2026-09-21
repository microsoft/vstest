// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.TestPlatform.Library.IntegrationTests.TranslationLayerTests.EventHandler;
using Microsoft.TestPlatform.TestUtilities;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.Library.IntegrationTests.TranslationLayerTests;

/// <summary>
/// End to end coverage of <see cref="TestCaseProperties.IdAlgorithm"/> reaching a consumer.
/// </summary>
/// <remarks>
/// <para>
/// The property exists for Visual Studio's Test Explorer, which reads discovered test cases through
/// the translation layer - this API. Unit tests can show that a test case records the right value
/// and that the value survives a round trip through each serializer, but not that the chain from the
/// testhost that computed the id, through vstest.console, to a caller holding a <c>TestCase</c>
/// keeps it. This is that observation.
/// </para>
/// <para>
/// It uses <c>SimpleTestProject4</c> for the same reason
/// <c>TestCaseIdAlgorithmScenarioTests</c> does: its adapter builds a <c>TestCase</c> without
/// assigning <c>Id</c>, so the platform computes it and the recorded algorithm is the one the run
/// selected. Every MSTest based asset in the repo would report <c>SelfAssigned</c> instead, which is
/// a real case but not this one.
/// </para>
/// </remarks>
[TestClass]
public class TestCaseIdAlgorithmPropertyTests : AcceptanceTestBase
{
    private const string FeatureFlagName = "VSTEST_DISABLE_XXHASH128_TESTCASE_ID";
    private const int ExpectedTestCount = 3;

    [TestMethod]
    [TestMatrix(console: Net, testHost: Net)]
    public void DiscoveredTestCasesReportTheAlgorithmThatComputedTheirId(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);

        var optedIn = DiscoverAndReadIdAlgorithms("0");
        var optedOut = DiscoverAndReadIdAlgorithms("1");
        var notDeclared = DiscoverAndReadIdAlgorithms(null);

        Assert.IsTrue(
            optedIn.All(a => a == TestCaseIdAlgorithms.XxHash128),
            $"Opting in must report xxHash128, but got [{string.Join(", ", optedIn)}].");

        Assert.IsTrue(
            optedOut.All(a => a == TestCaseIdAlgorithms.Sha1),
            $"Opting out must report SHA1, but got [{string.Join(", ", optedOut)}].");

        // Pins the default in the same terms as the unit tests: not declaring the flag has to report
        // exactly what pinning it to 1 reports, which is what makes this release a no-op for ids and
        // what a later release deliberately changes.
        CollectionAssert.AreEquivalent(
            optedOut,
            notDeclared,
            "Not declaring the flag must report the same algorithm as opting out, because xxHash128 " +
            "ships available but not default.");
    }

    private List<string?> DiscoverAndReadIdAlgorithms(string? featureFlagValue)
    {
        var assemblyPath = GetAssetFullPath("SimpleTestProject4.dll");

        var declaration = featureFlagValue is null
            ? string.Empty
            : $"<{FeatureFlagName}>{featureFlagValue}</{FeatureFlagName}>";

        // Keeping the element present but empty in the not-declared arm, rather than omitting it,
        // keeps every other consequence of declaring variables identical across the three arms.
        var runSettings =
            "<RunSettings><RunConfiguration><EnvironmentVariables>" +
            declaration +
            "</EnvironmentVariables></RunConfiguration></RunSettings>";

        // Clear the flag out of the inherited environment. vstest.console passes its own environment
        // down to the testhost, so a developer who has the flag exported - exactly the person
        // evaluating this feature - would otherwise see the not-declared arm pick their value up.
        var wrapper = GetVsTestConsoleWrapper(new Dictionary<string, string?> { [FeatureFlagName] = null });
        try
        {
            // The adapter ships next to the test assembly rather than in a package.
            wrapper.InitializeExtensions(
                Directory.EnumerateFiles(Path.GetDirectoryName(assemblyPath)!, "*.TestAdapter.dll").ToList());

            var handler = new DiscoveryEventHandler();
            wrapper.DiscoverTests(new[] { assemblyPath }, runSettings, handler);

            // Pinning the count is what stops an arm that discovered nothing from satisfying every
            // assertion vacuously - with no tests, "all of them report X" holds over an empty list.
            Assert.HasCount(ExpectedTestCount, handler.DiscoveredTestCases);

            return handler.DiscoveredTestCases
                .Select(t => t.GetPropertyValue<string?>(TestCaseProperties.IdAlgorithm, null))
                .ToList();
        }
        finally
        {
            wrapper.EndSession();
        }
    }
}
