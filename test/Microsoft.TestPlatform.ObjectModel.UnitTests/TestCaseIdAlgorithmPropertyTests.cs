// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.ObjectModel.UnitTests;

/// <summary>
/// Covers <see cref="TestCaseProperties.IdAlgorithm"/>: the record of how the id a test case carries
/// was produced.
/// </summary>
/// <remarks>
/// <para>
/// It exists so that a consumer caching test ids - Test Explorer, most visibly - can tell that the
/// platform has started hashing ids a different way and that the ids it holds have to be discovered
/// again. Without it the only symptom is results that silently fail to match anything cached.
/// </para>
/// <para>
/// These tests mutate a process wide environment variable and the cached flag value, so each one
/// restores both in a finally block.
/// </para>
/// </remarks>
[TestClass]
[DoNotParallelize]
public class TestCaseIdAlgorithmPropertyTests
{
    private const string OptIn = TestCase.XxHash128OptInValue;
    private const string OptOut = "1";

    private static TestCase CreateTestCase()
        => new("sampleTestClass.sampleTestCase", new Uri("executor://sampleTestExecutor"), "sampleTest.dll");

    private static string? IdAlgorithmOf(TestCase testCase)
        => testCase.GetPropertyValue<string?>(TestCaseProperties.IdAlgorithm, null);

    [TestMethod]
    public void APlatformComputedIdIsRecordedAsSha1WhenTheFlagOptsOut()
        => RunWithFlag(OptOut, () => Assert.AreEqual(TestCaseIdAlgorithms.Sha1, IdAlgorithmOf(CreateTestCase())));

    [TestMethod]
    public void APlatformComputedIdIsRecordedAsXxHash128WhenTheFlagOptsIn()
        => RunWithFlag(OptIn, () => Assert.AreEqual(TestCaseIdAlgorithms.XxHash128, IdAlgorithmOf(CreateTestCase())));

    /// <summary>
    /// The recorded value has to name the algorithm that actually produced the id, not merely be
    /// some plausible value, so it is checked against the id the two algorithms produce.
    /// </summary>
    /// <remarks>
    /// Asserted for both settings of the flag rather than only the default, so that this keeps
    /// meaning something after the default moves.
    /// </remarks>
    [TestMethod]
    [DataRow(OptIn)]
    [DataRow(OptOut)]
    public void TheRecordedAlgorithmNamesTheOneThatProducedTheId(string flagValue)
        => RunWithFlag(flagValue, () =>
        {
            TestCase testCase = CreateTestCase();

            // An xxHash128 id is an RFC 9562 version 8 UUID carrying the hashing scheme version in
            // its top nibble. A SHA1 id is unversioned and looks like neither.
            string id = testCase.Id.ToString("D");
            bool looksLikeXxHash128 = id[0] == '1' && id[14] == '8';

            Assert.AreEqual(
                looksLikeXxHash128 ? TestCaseIdAlgorithms.XxHash128 : TestCaseIdAlgorithms.Sha1,
                IdAlgorithmOf(testCase),
                $"The recorded algorithm disagrees with the id '{id}' that was actually produced.");
        });

    /// <summary>
    /// An adapter that assigns an id has hashed it itself, or not hashed anything at all, so the id
    /// does not move when the platform changes algorithm. MSTest v3 and later work this way.
    /// </summary>
    [TestMethod]
    public void AnAssignedIdIsRecordedAsSelfAssigned()
    {
        TestCase testCase = CreateTestCase();
        testCase.Id = new Guid("be78d6fc-61b0-4882-9d07-40d796fd96ce");

        Assert.AreEqual(TestCaseIdAlgorithms.SelfAssigned, IdAlgorithmOf(testCase));
    }

    [TestMethod]
    public void AnIdAssignedThroughThePropertyBagIsAlsoRecordedAsSelfAssigned()
    {
        TestCase testCase = CreateTestCase();
        testCase.SetPropertyValue(TestCaseProperties.Id, new Guid("be78d6fc-61b0-4882-9d07-40d796fd96ce"));

        Assert.AreEqual(TestCaseIdAlgorithms.SelfAssigned, IdAlgorithmOf(testCase));
    }

    /// <summary>
    /// The serialization constructor records nothing, and assigning an id to such a test case must
    /// not make it start recording something.
    /// </summary>
    /// <remarks>
    /// This is what keeps a payload from a vstest that predates the property honest. Deserializing
    /// one restores its id through the same setter an adapter uses, and if that invented a value the
    /// receiving process would be reporting a claim the sender never made - and reporting it as
    /// <c>SelfAssigned</c>, which is precisely the value that tells a consumer it has nothing to
    /// worry about.
    /// </remarks>
    [TestMethod]
    public void TheSerializationConstructorRecordsNothingAndAssigningAnIdDoesNotChangeThat()
    {
        var testCase = new TestCase
        {
            Id = new Guid("be78d6fc-61b0-4882-9d07-40d796fd96ce"),
        };

        Assert.IsNull(IdAlgorithmOf(testCase));
        Assert.DoesNotContain(
            "TestCase.IdAlgorithm",
            testCase.GetProperties().Select(p => p.Key.Id),
            "Nothing must be recorded, so that a consumer can tell 'not stated' from 'self assigned'.");
    }

    /// <summary>
    /// Changing something the id is hashed from invalidates the id, but not the algorithm: the id is
    /// still going to be computed, and still by this process.
    /// </summary>
    [TestMethod]
    public void ChangingTheSeedDoesNotChangeTheRecordedAlgorithm()
    {
        TestCase testCase = CreateTestCase();
        string? before = IdAlgorithmOf(testCase);

        testCase.FullyQualifiedName = "other.Test";
        testCase.Source = "other.dll";

        Assert.AreEqual(before, IdAlgorithmOf(testCase));
    }

    /// <summary>
    /// The property travels in the property bag rather than as a core field, which is what lets it
    /// reach a consumer on every protocol version without a serializer knowing about it.
    /// </summary>
    [TestMethod]
    public void TheAlgorithmIsRecordedInThePropertyBag()
    {
        TestCase testCase = CreateTestCase();

        var entry = testCase.GetProperties().Single(p => p.Key.Id == "TestCase.IdAlgorithm");

        Assert.AreEqual("System.String", entry.Key.ValueType);
        Assert.IsTrue(
            entry.Value is TestCaseIdAlgorithms.Sha1 or TestCaseIdAlgorithms.XxHash128,
            $"A computed id must be recorded as one of the known algorithms, but got '{entry.Value}'.");
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
