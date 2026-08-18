// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.ObjectModel.UnitTests;

/// <summary>
/// Covers the <c>VSTEST_TESTCASE_ID_ALGORITHM</c> opt-out that selects the legacy SHA1 test ids.
/// </summary>
/// <remarks>
/// These tests mutate a process wide environment variable and the cached algorithm choice, so each
/// one restores both in a finally block. The cache is deliberately lazy rather than initialized in
/// a static constructor, which is what makes it testable at all.
/// </remarks>
[TestClass]
public class TestCaseIdAlgorithmTests
{
    // The id of this test case under each algorithm. Same inputs as TestCaseTests.
    private const string XxHash128Id = "1ea84a1f-f791-8103-bfda-9cccaca2037f";
    private const string Sha1Id = "28e7a7ed-8fb9-05b7-5e90-4a8c52f32b5b";

    private static TestCase CreateTestCase()
        => new("sampleTestClass.sampleTestCase", new Uri("executor://sampleTestExecutor"), "sampleTest.dll");

    [TestMethod]
    public void TestCaseIdUsesXxHash128WhenEnvironmentVariableIsNotSet()
        => RunWithAlgorithm(null, () => Assert.AreEqual(XxHash128Id, CreateTestCase().Id.ToString()));

    [TestMethod]
    [DataRow("sha1")]
    [DataRow("SHA1")]
    [DataRow("Sha1")]
    public void TestCaseIdUsesLegacySha1WhenEnvironmentVariableIsSet(string value)
        => RunWithAlgorithm(value, () => Assert.AreEqual(Sha1Id, CreateTestCase().Id.ToString()));

    [TestMethod]
    [DataRow("")]
    [DataRow("xxhash128")]
    [DataRow("sha")]
    [DataRow("sha256")]
    [DataRow("nonsense")]
    public void TestCaseIdUsesXxHash128ForAnyOtherEnvironmentVariableValue(string value)
        => RunWithAlgorithm(value, () => Assert.AreEqual(XxHash128Id, CreateTestCase().Id.ToString()));

    [TestMethod]
    public void TestCaseIdAlgorithmIsReadLazilyRatherThanAtTypeLoad()
    {
        // Touch the type first, then set the variable. A static initializer would have already
        // baked in the wrong answer by this point; a lazy read picks the new value up.
        _ = CreateTestCase().Id;

        RunWithAlgorithm("sha1", () => Assert.AreEqual(Sha1Id, CreateTestCase().Id.ToString()));
    }

    [TestMethod]
    public void TestCaseIdAlgorithmIsCachedSoIdsStayStableWithinAProcess()
    {
        RunWithAlgorithm("sha1", () =>
        {
            Assert.AreEqual(Sha1Id, CreateTestCase().Id.ToString());

            // Changing the variable after the choice has been made must not change ids, otherwise
            // the same test could get two different ids within one run.
            Environment.SetEnvironmentVariable(TestCase.TestCaseIdAlgorithmEnvironmentVariable, null);

            Assert.AreEqual(Sha1Id, CreateTestCase().Id.ToString());
        });
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
