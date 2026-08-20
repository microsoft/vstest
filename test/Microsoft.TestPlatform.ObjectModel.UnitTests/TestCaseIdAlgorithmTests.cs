// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.ObjectModel.UnitTests;

/// <summary>
/// Covers the <c>VSTEST_TESTCASE_ID_ALGORITHM</c> switch that selects which algorithm computes
/// <see cref="TestCase.Id"/>.
/// </summary>
/// <remarks>
/// These tests mutate a process wide environment variable and the cached algorithm choice, so each
/// one restores both in a finally block. The cache is deliberately lazy rather than initialized in
/// a static constructor, which is what makes it testable at all.
/// </remarks>
[TestClass]
[DoNotParallelize]
public class TestCaseIdAlgorithmTests
{
    // The id of this test case under each algorithm. Same inputs as TestCaseTests.
    private const string XxHash128Id = "1ea84a1f-f791-8103-bfda-9cccaca2037f";
    private const string Sha1Id = "28e7a7ed-8fb9-05b7-5e90-4a8c52f32b5b";

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
    public void TestCaseIdUsesSha1WhenEnvironmentVariableIsNotSet()
        => RunWithAlgorithm(null, () => Assert.AreEqual(Sha1Id, CreateTestCase().Id.ToString()));

    [TestMethod]
    [DataRow("sha1")]
    [DataRow("SHA1")]
    [DataRow("Sha1")]
    public void TestCaseIdUsesSha1WhenSha1IsSelected(string value)
        => RunWithAlgorithm(value, () => Assert.AreEqual(Sha1Id, CreateTestCase().Id.ToString()));

    [TestMethod]
    [DataRow("xxhash128")]
    [DataRow("XXHASH128")]
    [DataRow("XxHash128")]
    public void TestCaseIdUsesXxHash128WhenXxHash128IsSelected(string value)
        => RunWithAlgorithm(value, () => Assert.AreEqual(XxHash128Id, CreateTestCase().Id.ToString()));

    /// <summary>
    /// An unrecognized value falls back to the default rather than failing the run. This is read on
    /// the way to computing an id, where there is nowhere sensible to surface an error, and failing
    /// a whole run over a typo in an opt-in switch would be a worse outcome than ignoring it.
    /// </summary>
    /// <remarks>
    /// Asserted against the id produced with nothing set, rather than against a literal, so that
    /// this keeps testing "unrecognized means default" rather than quietly becoming a second place
    /// that pins which algorithm the default is.
    /// </remarks>
    [TestMethod]
    [DataRow("")]
    [DataRow("sha")]
    [DataRow("sha256")]
    [DataRow("xxhash")]
    [DataRow("nonsense")]
    public void TestCaseIdUsesTheDefaultForAnyUnrecognizedEnvironmentVariableValue(string value)
    {
        string defaultId = string.Empty;
        RunWithAlgorithm(null, () => defaultId = CreateTestCase().Id.ToString());

        RunWithAlgorithm(value, () => Assert.AreEqual(defaultId, CreateTestCase().Id.ToString()));
    }

    [TestMethod]
    public void TestCaseIdAlgorithmIsReadLazilyRatherThanAtTypeLoad()
    {
        // Touch the type first, then set the variable. A static initializer would have already
        // baked in the wrong answer by this point; a lazy read picks the new value up.
        _ = CreateTestCase().Id;

        (string name, string expected) = NonDefaultAlgorithm();

        RunWithAlgorithm(name, () => Assert.AreEqual(expected, CreateTestCase().Id.ToString()));
    }

    [TestMethod]
    public void TestCaseIdAlgorithmIsCachedSoIdsStayStableWithinAProcess()
    {
        (string name, string expected) = NonDefaultAlgorithm();

        RunWithAlgorithm(name, () =>
        {
            Assert.AreEqual(expected, CreateTestCase().Id.ToString());

            // Changing the variable after the choice has been made must not change ids, otherwise
            // the same test could get two different ids within one run.
            Environment.SetEnvironmentVariable(TestCase.TestCaseIdAlgorithmEnvironmentVariable, null);

            Assert.AreEqual(expected, CreateTestCase().Id.ToString());
        });
    }

    /// <summary>
    /// The algorithm that is <em>not</em> currently the default, and the id it produces.
    /// </summary>
    /// <remarks>
    /// Both tests above have to select an algorithm that differs from the default, otherwise they
    /// hold vacuously: selecting the default proves nothing about whether the selection was read at
    /// all. Which algorithm that is has to be derived rather than written down, so that these keep
    /// testing laziness and caching after the default moves instead of quietly going hollow.
    /// </remarks>
    private static (string Name, string Id) NonDefaultAlgorithm()
    {
        string defaultId = string.Empty;
        RunWithAlgorithm(null, () => defaultId = CreateTestCase().Id.ToString());

        Assert.IsTrue(
            defaultId is Sha1Id or XxHash128Id,
            $"The default produced {defaultId}, which is neither known algorithm's id.");

        return defaultId == Sha1Id ? ("xxhash128", XxHash128Id) : ("sha1", Sha1Id);
    }

    /// <summary>
    /// Runs <paramref name="action"/> with the algorithm environment variable set to
    /// <paramref name="value"/>, restoring the previous value and the cached choice afterwards.
    /// </summary>
    private static void RunWithAlgorithm(string? value, Action action)
    {
        string? original = Environment.GetEnvironmentVariable(TestCase.TestCaseIdAlgorithmEnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(TestCase.TestCaseIdAlgorithmEnvironmentVariable, value);
            TestCase.ResetTestIdAlgorithmCache();

            action();
        }
        finally
        {
            Environment.SetEnvironmentVariable(TestCase.TestCaseIdAlgorithmEnvironmentVariable, original);
            TestCase.ResetTestIdAlgorithmCache();
        }
    }
}
