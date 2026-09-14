// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.TestPlatform.Hashing;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.ObjectModel.UnitTests;

/// <summary>
/// Covers the <c>VSTEST_DISABLE_XXHASH128_TESTCASE_ID</c> feature flag, which decides which
/// algorithm computes <see cref="TestCase.Id"/>.
/// </summary>
/// <remarks>
/// <para>
/// Only the tests that genuinely depend on the ambient environment - the default, laziness, and
/// caching - mutate the process wide environment variable and the cached flag value, restoring both
/// in a finally block. Everything that only asks "which algorithm does this declared value select"
/// goes through <see cref="TestCaseIdAlgorithmResolver.Resolve(string)"/> instead, which is a pure
/// function of its argument and touches no shared state, so those tests do not need to coordinate
/// with anything else in this assembly.
/// </para>
/// <para>
/// See <see href="https://github.com/microsoft/vstest/issues/16433"/> for why the ambient tests
/// cannot be converted the same way.
/// </para>
/// </remarks>
[TestClass]
[DoNotParallelize]
public class TestCaseIdAlgorithmTests
{
    // The id of this test case under each algorithm. Same inputs as TestCaseTests.
    private const string XxHash128Id = "1ea84a1f-f791-8103-bfda-9cccaca2037f";
    private const string Sha1Id = "28e7a7ed-8fb9-05b7-5e90-4a8c52f32b5b";

    // The value that opts in to xxHash128, and the canonical value that opts out. Any value other
    // than the first one opts out, so the second is a convention rather than a rule.
    private const string OptIn = TestCase.XxHash128OptInValue;
    private const string OptOut = "1";

    private static TestCase CreateTestCase()
        => new("sampleTestClass.sampleTestCase", new Uri("executor://sampleTestExecutor"), "sampleTest.dll");

    /// <summary>
    /// The default algorithm, asserted on its own rather than only implied by the tests below.
    /// </summary>
    /// <remarks>
    /// xxHash128 ships available but not default, so this release changes no id at all. Flipping the
    /// default is the entire behavioural change of a later release, and this test is what makes that
    /// flip impossible to make by accident: it fails unless the default is also changed here
    /// deliberately, at which point the flip has been stated in two places that have to agree.
    /// </remarks>
    [TestMethod]
    public void TestCaseIdUsesSha1WhenTheFeatureFlagIsNotSet()
        => RunWithFlag(null, () => Assert.AreEqual(Sha1Id, CreateTestCase().Id.ToString()));

    /// <summary>
    /// A declared value that opts in to xxHash128 resolves to that algorithm.
    /// </summary>
    /// <remarks>
    /// Goes through <see cref="TestCaseIdAlgorithmResolver.Resolve(string)"/> directly rather than
    /// through <see cref="TestCase.Id"/>, so this asserts the resolution rule itself without touching
    /// the environment or the feature flag cache. <see cref="TestCaseIdUsesSha1WhenTheFeatureFlagIsNotSet"/>
    /// and the tests below it are what actually exercise the ambient path.
    /// </remarks>
    [TestMethod]
    [DataRow(OptIn)]
    [DataRow(" 0 ")]
    public void ResolveSelectsXxHash128WhenTheDeclaredValueOptsIn(string value)
        => Assert.AreEqual(TestCaseIdAlgorithm.XxHash128, TestCaseIdAlgorithmResolver.Resolve(value));

    /// <summary>
    /// Every value other than <c>0</c> resolves to SHA1.
    /// </summary>
    /// <remarks>
    /// <para>
    /// There is deliberately no notion of an unrecognized value: this is exactly how
    /// <c>FeatureFlag</c> reads every other <c>VSTEST_DISABLE_*</c> flag, so a typo means "disabled"
    /// here just as it does everywhere else, rather than meaning something this one flag invented for
    /// itself. The empty string is not covered here because Windows deletes a variable set to it, so
    /// the case is indistinguishable from "unset"; it is covered on the declared-value path in
    /// MtpTestNodeConverterTestIdTests, where it is a dictionary entry rather than a variable. A
    /// whitespace-only value <em>is</em> covered, because it survives on both operating systems and
    /// is the one input where this and the declared-value path could silently drift apart.
    /// </para>
    /// <para>
    /// Goes through <see cref="TestCaseIdAlgorithmResolver.Resolve(string)"/> directly for the same
    /// reason as <see cref="ResolveSelectsXxHash128WhenTheDeclaredValueOptsIn"/>: it is a pure
    /// function of the declared value and needs no ambient state to assert against.
    /// </para>
    /// </remarks>
    [TestMethod]
    [DataRow(OptOut)]
    [DataRow(" 1 ")]
    [DataRow("   ")]
    [DataRow("true")]
    [DataRow("nonsense")]
    [DataRow("00")]
    public void ResolveSelectsSha1ForEveryDeclaredValueOtherThanZero(string value)
        => Assert.AreEqual(TestCaseIdAlgorithm.Sha1, TestCaseIdAlgorithmResolver.Resolve(value));

    [TestMethod]
    public void TestCaseIdAlgorithmIsReadLazilyRatherThanAtTypeLoad()
    {
        // Touch the type first, then set the flag. A static initializer would have already baked in
        // the wrong answer by this point; a lazy read picks the new value up.
        _ = CreateTestCase().Id;

        (string value, string expected) = NonDefaultAlgorithm();

        RunWithFlag(value, () => Assert.AreEqual(expected, CreateTestCase().Id.ToString()));
    }

    [TestMethod]
    public void TestCaseIdAlgorithmIsCachedSoIdsStayStableWithinAProcess()
    {
        (string value, string expected) = NonDefaultAlgorithm();

        RunWithFlag(value, () =>
        {
            Assert.AreEqual(expected, CreateTestCase().Id.ToString());

            // Changing the flag after the choice has been made must not change ids, otherwise the
            // same test could get two different ids within one run.
            Environment.SetEnvironmentVariable(TestCase.TestCaseIdAlgorithmFeatureFlag, null);

            Assert.AreEqual(expected, CreateTestCase().Id.ToString());
        });
    }

    /// <summary>
    /// The flag value that selects the algorithm that is <em>not</em> currently the default, and the
    /// id it produces.
    /// </summary>
    /// <remarks>
    /// Both tests above have to select an algorithm that differs from the default, otherwise they
    /// hold vacuously: selecting the default proves nothing about whether the flag was read at all.
    /// Which value that is has to be derived rather than written down, so that these keep testing
    /// laziness and caching after the default moves instead of quietly going hollow.
    /// </remarks>
    private static (string Value, string Id) NonDefaultAlgorithm()
    {
        string defaultId = string.Empty;
        RunWithFlag(null, () => defaultId = CreateTestCase().Id.ToString());

        Assert.IsTrue(
            defaultId is Sha1Id or XxHash128Id,
            $"The default produced {defaultId}, which is neither known algorithm's id.");

        return defaultId == Sha1Id
            ? (OptIn, XxHash128Id)
            : (OptOut, Sha1Id);
    }

    /// <summary>
    /// Runs <paramref name="action"/> with the feature flag set to <paramref name="value"/>,
    /// restoring the previous value and the cached choice afterwards.
    /// </summary>
    private static void RunWithFlag(string? value, Action action)
    {
        string? original = Environment.GetEnvironmentVariable(TestCase.TestCaseIdAlgorithmFeatureFlag);
        try
        {
            Environment.SetEnvironmentVariable(TestCase.TestCaseIdAlgorithmFeatureFlag, value);
            ResetFeatureFlagCache();

            action();
        }
        finally
        {
            Environment.SetEnvironmentVariable(TestCase.TestCaseIdAlgorithmFeatureFlag, original);
            ResetFeatureFlagCache();
        }
    }

#pragma warning disable CS0618 // ResetFeatureFlagCacheForTesting is what its name says it is.
    private static void ResetFeatureFlagCache() => TestCase.ResetFeatureFlagCacheForTesting();
#pragma warning restore CS0618
}
