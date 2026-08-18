// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.ObjectModel.UnitTests.Utilities;

/// <summary>
/// Pins <see cref="EqtHash.GuidFromString2(string)"/>, which produces the id of every test case.
/// </summary>
/// <remarks>
/// These ids end up in TRX files, in Azure DevOps and in Test Case work item association, so they
/// must never change by accident. If a change is deliberate, bump the hash version alongside it.
/// </remarks>
[TestClass]
public class EqtHashTests
{
    [TestMethod]
    [DataRow("", "19aa06d3-0147-88d8-a001-c324468d497f")]
    [DataRow("abc", "1dcae961-3d3c-87ca-8340-2c89fa0d3198")]
    [DataRow("abcdbcdecdefdefgefghfghighij", "104a4cb6-4809-8833-8bb8-ad8d0d87f655")]
    [DataRow("executor://mstestadapter/v2MyTest.dllMyNamespace.MyClass.MyMethod", "1bdbbaf9-e478-82dc-bc1a-f161fabee1ee")]
    public void GuidFromString2_ProducesPinnedIds(string data, string expected)
        => Assert.AreEqual(expected, EqtHash.GuidFromString2(data).ToString(), $"Test id for '{data}' changed.");

    [TestMethod]
    public void GuidFromString2_ProducesPinnedId_ForVeryLargeInput()
    {
        // Long enough to exercise the block based path rather than the short input path.
        string data = string.Concat(System.Linq.Enumerable.Repeat("abc", 100_000));

        Assert.AreEqual("154504d8-e373-86f7-b493-b93fb9f2970a", EqtHash.GuidFromString2(data).ToString());
    }

    [TestMethod]
    public void GuidFromString2_IsDeterministic()
        => Assert.AreEqual(EqtHash.GuidFromString2("some.test.name"), EqtHash.GuidFromString2("some.test.name"));

    [TestMethod]
    public void GuidFromString2_ProducesVersion8Uuids()
    {
        Guid id = EqtHash.GuidFromString2("some.test.name");

        string text = id.ToString("D");
        Assert.AreEqual('8', text[14], $"Expected a version 8 UUID but got {id}.");
        Assert.IsTrue(text[19] is '8' or '9' or 'a' or 'b', $"Expected an RFC 9562 variant but got {id}.");
        Assert.AreEqual('1', text[0], $"Expected hash version 1 to be embedded in {id}.");
    }

    [TestMethod]
    public void GuidFromString2_DiffersFromLegacySha1Id()
    {
        // The whole point of the change: the new id is not the old id. If these ever match, the
        // switch silently did not happen.
#pragma warning disable CS0618 // Type or member is obsolete - deliberately comparing against the legacy algorithm.
        Guid legacy = EqtHash.GuidFromString("some.test.name");
#pragma warning restore CS0618

        Assert.AreNotEqual(legacy, EqtHash.GuidFromString2("some.test.name"));
    }

    [TestMethod]
    public void TestCase_Id_UsesTheXxHash128Algorithm()
    {
        var testCase = new TestCase("MyNamespace.MyClass.MyMethod", new Uri("executor://mstestadapter/v2"), "MyTest.dll");

        // TestCase.Id hashes ExecutorUri + fileName(Source) + FullyQualifiedName.
        Guid expected = EqtHash.GuidFromString2("executor://mstestadapter/v2MyTest.dllMyNamespace.MyClass.MyMethod");

        Assert.AreEqual(expected, testCase.Id);
    }
}
