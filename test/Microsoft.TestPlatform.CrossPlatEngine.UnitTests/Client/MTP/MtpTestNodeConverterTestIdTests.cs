// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.Testing.Platform.ServerMode.Client;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.MTP;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.UnitTests.Client.MTP;

/// <summary>
/// Covers the <c>VSTEST_TESTCASE_ID_ALGORITHM</c> opt-out on the Microsoft.Testing.Platform path.
/// </summary>
/// <remarks>
/// On the classic path a test case is built inside the testhost, which receives the environment
/// variables declared in runsettings, so the opt-out is visible where the id is computed. MTP
/// applications are their own host and their nodes are converted here, in the runner, which does not
/// receive those variables. The runner therefore has to read the declared value itself and pass the
/// choice in, otherwise a runsettings opt-out is silently ignored on this path only.
/// </remarks>
[TestClass]
public class MtpTestNodeConverterTestIdTests
{
    private const string Source = @"C:\tests\MtpApp.dll";
    private const string EnvironmentVariable = "VSTEST_TESTCASE_ID_ALGORITHM";

    private static MtpTestNodeUpdate Node()
        => new(
            new Dictionary<string, object?>
            {
                ["uid"] = "MtpApp.Tests.SomeTest",
                ["display-name"] = "SomeTest",
                ["node-type"] = "action",
            },
            parentUid: null);

    [TestMethod]
    public void ResolveUseLegacySha1TestIdsReturnsNullWhenNotDeclared()
    {
        Assert.IsNull(MtpTestNodeConverter.ResolveUseLegacySha1TestIds(null));
        Assert.IsNull(MtpTestNodeConverter.ResolveUseLegacySha1TestIds(new Dictionary<string, string?>()));
        Assert.IsNull(MtpTestNodeConverter.ResolveUseLegacySha1TestIds(new Dictionary<string, string?> { ["OTHER"] = "sha1" }));
    }

    [TestMethod]
    [DataRow("sha1")]
    [DataRow("SHA1")]
    [DataRow("Sha1")]
    public void ResolveUseLegacySha1TestIdsReturnsTrueForLegacyAlgorithm(string value)
        => Assert.IsTrue(MtpTestNodeConverter.ResolveUseLegacySha1TestIds(new Dictionary<string, string?> { [EnvironmentVariable] = value }));

    [TestMethod]
    [DataRow("")]
    [DataRow("xxhash128")]
    [DataRow("sha256")]
    [DataRow("nonsense")]
    public void ResolveUseLegacySha1TestIdsReturnsFalseForAnyOtherDeclaredValue(string value)
        => Assert.IsFalse(MtpTestNodeConverter.ResolveUseLegacySha1TestIds(new Dictionary<string, string?> { [EnvironmentVariable] = value }));

    [TestMethod]
    public void ResolveUseLegacySha1TestIdsMatchesTheVariableCaseInsensitively()
        => Assert.IsTrue(MtpTestNodeConverter.ResolveUseLegacySha1TestIds(new Dictionary<string, string?> { ["vstest_testcase_id_algorithm"] = "sha1" }));

    [TestMethod]
    public void ToTestCaseUsesTheDefaultAlgorithmWhenNoChoiceIsPassed()
    {
        Guid id = MtpTestNodeConverter.ToTestCase(Node(), Source, useLegacySha1TestIds: null).Id;

        // Version 8 UUID carrying hash version 1, which is what the default xxHash128 scheme stamps.
        string text = id.ToString("D");
        Assert.AreEqual('1', text[0], $"Expected the default algorithm to be used, but got {id}.");
        Assert.AreEqual('8', text[14], $"Expected a version 8 UUID, but got {id}.");
    }

    [TestMethod]
    public void ToTestCaseUsesTheDefaultAlgorithmWhenLegacyIsNotRequested()
        => Assert.AreEqual(
            MtpTestNodeConverter.ToTestCase(Node(), Source, useLegacySha1TestIds: null).Id,
            MtpTestNodeConverter.ToTestCase(Node(), Source, useLegacySha1TestIds: false).Id);

    [TestMethod]
    public void ToTestCaseUsesLegacyAlgorithmWhenRequested()
    {
        Guid legacyId = MtpTestNodeConverter.ToTestCase(Node(), Source, useLegacySha1TestIds: true).Id;

        Assert.AreNotEqual(
            MtpTestNodeConverter.ToTestCase(Node(), Source, useLegacySha1TestIds: null).Id,
            legacyId,
            "Requesting the legacy algorithm must not produce the default id.");

        // A SHA1 id is unversioned, so it must not look like the version 8 UUID the default stamps.
        Assert.AreNotEqual('8', legacyId.ToString("D")[14], $"Legacy ids must not be version 8 UUIDs, but got {legacyId}.");
    }

    [TestMethod]
    public void ToTestResultHonorsTheRequestedAlgorithm()
        => Assert.AreEqual(
            MtpTestNodeConverter.ToTestCase(Node(), Source, useLegacySha1TestIds: true).Id,
            MtpTestNodeConverter.ToTestResult(Node(), Source, useLegacySha1TestIds: true).TestCase.Id);

    /// <summary>
    /// The id the runner computes for an MTP node must be exactly the id a test case would have
    /// computed for itself, for both algorithms.
    /// </summary>
    /// <remarks>
    /// This is the drift guard for the fact that the MTP path composes the hash seed itself rather
    /// than letting TestCase do it. It has already earned its keep: the first version of the fix
    /// seeded the hash with the raw executor uri string, but TestCase seeds it with the parsed
    /// <see cref="Uri"/>, which normalizes the scheme and host to lower case. The ids differed and
    /// only an end to end comparison like this one showed it.
    /// </remarks>
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ConvertedIdMatchesTheIdTestCaseComputesForItself(bool useLegacySha1)
    {
        string? original = Environment.GetEnvironmentVariable(EnvironmentVariable);
        try
        {
            // Drive TestCase through its own ambient path, so the expectation is produced by the
            // production code rather than restated here.
            Environment.SetEnvironmentVariable(EnvironmentVariable, useLegacySha1 ? "sha1" : null);
            TestCase.ResetTestIdAlgorithmCache();

            TestCase converted = MtpTestNodeConverter.ToTestCase(Node(), Source, useLegacySha1);
            var equivalent = new TestCase(converted.FullyQualifiedName, converted.ExecutorUri, converted.Source);

            Assert.AreEqual(equivalent.Id, converted.Id);
        }
        finally
        {
            Environment.SetEnvironmentVariable(EnvironmentVariable, original);
            TestCase.ResetTestIdAlgorithmCache();
        }
    }
}
