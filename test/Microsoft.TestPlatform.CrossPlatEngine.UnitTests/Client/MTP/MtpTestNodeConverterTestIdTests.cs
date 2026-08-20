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
/// Covers the <c>VSTEST_TESTCASE_ID_ALGORITHM</c> switch on the Microsoft.Testing.Platform path.
/// </summary>
/// <remarks>
/// <para>
/// On the classic path a test case is built inside the testhost, which receives the environment
/// variables declared in runsettings, so the switch is visible where the id is computed. MTP
/// applications are their own host and their nodes are converted here, in the runner, which does not
/// receive those variables. The runner therefore has to read the declared value itself and pass the
/// choice in, otherwise a runsettings selection is silently ignored on this path only.
/// </para>
/// <para>
/// The algorithm type is compiled into both this assembly and ObjectModel as shared source, and this
/// test assembly can see the internals of both, so spelling the type out here would be ambiguous.
/// Nothing below names it: every choice is inferred from the production resolver, which has the
/// side benefit that the assertions exercise the real resolution path instead of a value built by
/// hand to look like its result.
/// </para>
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

    /// <summary>
    /// The runsettings environment variables of a run declaring <paramref name="value"/>.
    /// </summary>
    private static Dictionary<string, string?> Declaring(string? value)
        => new() { [EnvironmentVariable] = value };

    [TestMethod]
    public void ResolveTestCaseIdAlgorithmReturnsNullWhenNotDeclared()
    {
        Assert.IsNull(MtpTestNodeConverter.ResolveTestCaseIdAlgorithm(null));
        Assert.IsNull(MtpTestNodeConverter.ResolveTestCaseIdAlgorithm(new Dictionary<string, string?>()));
        Assert.IsNull(MtpTestNodeConverter.ResolveTestCaseIdAlgorithm(new Dictionary<string, string?> { ["OTHER"] = "sha1" }));
    }

    [TestMethod]
    [DataRow("SHA1")]
    [DataRow("Sha1")]
    public void ResolveTestCaseIdAlgorithmMatchesSha1CaseInsensitively(string value)
        => Assert.AreEqual(
            MtpTestNodeConverter.ResolveTestCaseIdAlgorithm(Declaring("sha1")),
            MtpTestNodeConverter.ResolveTestCaseIdAlgorithm(Declaring(value)));

    [TestMethod]
    [DataRow("XXHASH128")]
    [DataRow("XxHash128")]
    public void ResolveTestCaseIdAlgorithmMatchesXxHash128CaseInsensitively(string value)
        => Assert.AreEqual(
            MtpTestNodeConverter.ResolveTestCaseIdAlgorithm(Declaring("xxhash128")),
            MtpTestNodeConverter.ResolveTestCaseIdAlgorithm(Declaring(value)));

    [TestMethod]
    public void ResolveTestCaseIdAlgorithmDistinguishesTheTwoAlgorithms()
        => Assert.AreNotEqual(
            MtpTestNodeConverter.ResolveTestCaseIdAlgorithm(Declaring("sha1")),
            MtpTestNodeConverter.ResolveTestCaseIdAlgorithm(Declaring("xxhash128")));

    /// <summary>
    /// A declared but unrecognized value resolves to the default rather than to "not declared".
    /// </summary>
    /// <remarks>
    /// The distinction matters: "not declared" falls back to the runner's own environment, so
    /// treating a typo as "not declared" would let an inherited value take over a run that had said
    /// something explicit about the algorithm.
    /// </remarks>
    [TestMethod]
    [DataRow("")]
    [DataRow("sha256")]
    [DataRow("nonsense")]
    public void ResolveTestCaseIdAlgorithmFallsBackToTheDefaultForUnrecognizedValues(string value)
    {
        var resolved = MtpTestNodeConverter.ResolveTestCaseIdAlgorithm(Declaring(value));

        Assert.IsNotNull(resolved, "An unrecognized value is still a declaration, so it must not read as 'not declared'.");
        Assert.AreEqual(
            MtpTestNodeConverter.ResolveTestCaseIdAlgorithm(Declaring("sha1")),
            resolved,
            "SHA1 is expected to be the default algorithm in this release.");
    }

    [TestMethod]
    public void ResolveTestCaseIdAlgorithmMatchesTheVariableNameCaseInsensitively()
        => Assert.AreEqual(
            MtpTestNodeConverter.ResolveTestCaseIdAlgorithm(Declaring("sha1")),
            MtpTestNodeConverter.ResolveTestCaseIdAlgorithm(new Dictionary<string, string?> { ["vstest_testcase_id_algorithm"] = "sha1" }));

    [TestMethod]
    public void ToTestCaseLeavesTheIdToTestCaseWhenNoAlgorithmIsDeclared()
    {
        // Nothing is declared, so the id must be whatever TestCase itself would have computed under
        // the runner's ambient environment. Comparing against a plain TestCase keeps this independent
        // of which algorithm currently happens to be the default.
        TestCase converted = MtpTestNodeConverter.ToTestCase(Node(), Source, testCaseIdAlgorithm: null);
        var equivalent = new TestCase(converted.FullyQualifiedName, converted.ExecutorUri, converted.Source);

        Assert.AreEqual(equivalent.Id, converted.Id);
    }

    [TestMethod]
    public void ToTestCaseStampsAVersionedUuidWhenXxHash128IsDeclared()
    {
        var xxHash128 = MtpTestNodeConverter.ResolveTestCaseIdAlgorithm(Declaring("xxhash128"));

        Guid id = MtpTestNodeConverter.ToTestCase(Node(), Source, xxHash128).Id;

        string text = id.ToString("D");
        Assert.AreEqual('1', text[0], $"Expected hash version 1 to be embedded in {id}.");
        Assert.AreEqual('8', text[14], $"Expected a version 8 UUID, but got {id}.");
    }

    [TestMethod]
    public void ToTestCaseProducesDifferentIdsForTheTwoAlgorithms()
    {
        var sha1 = MtpTestNodeConverter.ResolveTestCaseIdAlgorithm(Declaring("sha1"));
        var xxHash128 = MtpTestNodeConverter.ResolveTestCaseIdAlgorithm(Declaring("xxhash128"));

        Guid sha1Id = MtpTestNodeConverter.ToTestCase(Node(), Source, sha1).Id;
        Guid xxHash128Id = MtpTestNodeConverter.ToTestCase(Node(), Source, xxHash128).Id;

        Assert.AreNotEqual(sha1Id, xxHash128Id, "Selecting an algorithm must not be a silent no-op.");

        // A SHA1 id is unversioned, so it must not look like the version 8 UUID xxHash128 stamps.
        Assert.AreNotEqual('8', sha1Id.ToString("D")[14], $"SHA1 ids must not be version 8 UUIDs, but got {sha1Id}.");
    }

    [TestMethod]
    [DataRow("sha1")]
    [DataRow("xxhash128")]
    public void ToTestResultHonorsTheDeclaredAlgorithm(string value)
    {
        var algorithm = MtpTestNodeConverter.ResolveTestCaseIdAlgorithm(Declaring(value));

        Assert.AreEqual(
            MtpTestNodeConverter.ToTestCase(Node(), Source, algorithm).Id,
            MtpTestNodeConverter.ToTestResult(Node(), Source, algorithm).TestCase.Id);
    }

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
    [DataRow("sha1")]
    [DataRow("xxhash128")]
    public void ConvertedIdMatchesTheIdTestCaseComputesForItself(string value)
    {
        string? original = Environment.GetEnvironmentVariable(EnvironmentVariable);
        try
        {
            // Drive TestCase through its own ambient path, so the expectation is produced by the
            // production code rather than restated here.
            Environment.SetEnvironmentVariable(EnvironmentVariable, value);
            TestCase.ResetTestIdAlgorithmCache();

            var algorithm = MtpTestNodeConverter.ResolveTestCaseIdAlgorithm(Declaring(value));

            TestCase converted = MtpTestNodeConverter.ToTestCase(Node(), Source, algorithm);
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
