// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using TestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;

namespace Microsoft.TestPlatform.CommunicationUtilities.UnitTests.Serialization;

/// <summary>
/// Wire coverage for <see cref="TestCaseProperties.IdAlgorithm"/>.
/// </summary>
/// <remarks>
/// <para>
/// The property is useless unless it reaches the consumer, and reaching it is not obvious: v1 and
/// v2+ use different converters, and a test case travels to a consumer on two different routes -
/// discovery reports test cases, execution reports results that each carry one.
/// </para>
/// <para>
/// Versions are picked to cover both converters and both ends of the range each one serves: 1 and 3
/// use the v1 converter, 2 and 7 the v2 one.
/// </para>
/// </remarks>
[TestClass]
[TestCategory("Serialization")]
public class TestCaseIdAlgorithmSerializationTests
{
    private static TestCase ComputedIdTestCase()
        => new("sampleTestClass.sampleTestCase", new Uri("executor://sampleTestExecutor"), "sampleTest.dll");

    private static TestCase SelfAssignedIdTestCase()
        => new("sampleTestClass.sampleTestCase", new Uri("executor://sampleTestExecutor"), "sampleTest.dll")
        {
            Id = new Guid("be78d6fc-61b0-4882-9d07-40d796fd96ce"),
        };

    private static string? IdAlgorithmOf(TestCase testCase)
        => testCase.GetPropertyValue<string?>(TestCaseProperties.IdAlgorithm, null);

    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(7)]
    public void ATestCaseCarriesItsIdAlgorithmAcrossTheWire(int version)
    {
        TestCase testCase = ComputedIdTestCase();
        string? expected = IdAlgorithmOf(testCase);

        var roundTripped = Deserialize<TestCase>(Serialize(testCase, version), version);

        Assert.AreEqual(expected, IdAlgorithmOf(roundTripped));
        Assert.AreEqual(testCase.Id, roundTripped.Id, "The id itself must be unaffected.");
    }

    /// <summary>
    /// A self assigned id must still read as self assigned after a round trip.
    /// </summary>
    /// <remarks>
    /// Deserializing restores the id through the same setter an adapter uses, which is what records
    /// self assignment in the first place, so the two could plausibly fight. This is the arm where
    /// they happen to agree; the interesting one is the test below, where they must not.
    /// </remarks>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(7)]
    public void ASelfAssignedIdStaysSelfAssignedAcrossTheWire(int version)
    {
        var roundTripped = Deserialize<TestCase>(Serialize(SelfAssignedIdTestCase(), version), version);

        Assert.AreEqual(TestCaseIdAlgorithms.SelfAssigned, IdAlgorithmOf(roundTripped));
    }

    /// <summary>
    /// A payload that states nothing must still state nothing after being read.
    /// </summary>
    /// <remarks>
    /// That is what a test case sent by a vstest older than this property looks like, and the
    /// receiving process must not invent an answer on the sender's behalf. It would invent
    /// <c>SelfAssigned</c> - the one value that tells a consumer its cached ids are safe - so
    /// getting this wrong is worse than saying nothing.
    /// </remarks>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(7)]
    public void APayloadThatStatesNoAlgorithmIsReadAsStatingNone(int version)
    {
        // Serialize a test case, then strip the property back out, which is exactly the payload an
        // older vstest writes. Producing it by removal rather than by hand keeps the rest of the
        // payload identical to what this version really sends.
        TestCase testCase = ComputedIdTestCase();
        testCase.RemovePropertyValue(TestCaseProperties.IdAlgorithm);

        var roundTripped = Deserialize<TestCase>(Serialize(testCase, version), version);

        Assert.IsNull(IdAlgorithmOf(roundTripped));
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(7)]
    public void ATestResultCarriesTheIdAlgorithmOfItsTestCase(int version)
    {
        var result = new TestResult(ComputedIdTestCase())
        {
            Outcome = TestOutcome.Passed,
        };
        string? expected = IdAlgorithmOf(result.TestCase);

        var roundTripped = Deserialize<TestResult>(Serialize(result, version), version);

        Assert.AreEqual(expected, IdAlgorithmOf(roundTripped.TestCase));
    }

    /// <summary>
    /// The discovery route, as a message payload rather than a bare object.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(7)]
    public void DiscoveredTestCasesCarryTheIdAlgorithm(int version)
    {
        TestCase testCase = ComputedIdTestCase();
        string? expected = IdAlgorithmOf(testCase);

        var raw = JsonDataSerializer.Instance.SerializePayload(
            MessageType.TestCasesFound, new List<TestCase> { testCase }, version);
        var message = JsonDataSerializer.Instance.DeserializeMessage(raw);
        var received = JsonDataSerializer.Instance.DeserializePayload<IEnumerable<TestCase>>(message);

        Assert.IsNotNull(received);
        Assert.AreEqual(expected, IdAlgorithmOf(received.Single()));
    }

    private static string Serialize<T>(T data, int version)
        => JsonDataSerializer.Instance.Serialize(data, version);

    private static T Deserialize<T>(string json, int version)
        => JsonDataSerializer.Instance.Deserialize<T>(json, version)!;
}
