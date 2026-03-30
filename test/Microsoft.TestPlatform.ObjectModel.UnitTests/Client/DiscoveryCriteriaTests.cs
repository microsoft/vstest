// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET
using System;
using System.Linq;
using System.Text.Json;

using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.ObjectModel.UnitTests.Client;

[TestClass]
public class DiscoveryCriteriaTests
{
    private readonly DiscoveryCriteria _discoveryCriteria;
    private static readonly JsonSerializerOptions Settings = new()
    {
    };

    public DiscoveryCriteriaTests()
    {
        _discoveryCriteria = new DiscoveryCriteria(
            new[] { "sampleTest.dll" },
            100,
            "<RunConfiguration></RunConfiguration>");
        _discoveryCriteria.TestCaseFilter = "TestFilter";
    }

    [TestMethod]
    public void DiscoveryCriteriaSerializesToExpectedJson()
    {
        var json = JsonSerializer.Serialize(_discoveryCriteria, Settings);

        // Verify key properties are present in JSON
        Assert.IsTrue(json.Contains("\"Sources\""), "Missing Sources");
        Assert.IsTrue(json.Contains("sampleTest.dll"), "Missing source path");
        Assert.IsTrue(json.Contains("\"FrequencyOfDiscoveredTestsEvent\":100"), "Missing frequency");
        Assert.IsTrue(json.Contains("\"TestCaseFilter\":\"TestFilter\""), "Missing filter");
        Assert.IsTrue(json.Contains("\"AdapterSourceMap\""), "Missing adapter map");
        Assert.IsTrue(json.Contains("RunConfiguration"), "Missing run settings");
    }

    [TestMethod]
    public void DiscoveryCriteriaShouldBeDeserializable()
    {
        // Raw STJ roundtrip — TimeSpan does not roundtrip without a custom converter,
        // so we verify the other properties. TimeSpan roundtrip is covered by the
        // wire-format tests in CommunicationUtilities.UnitTests via DiscoveryCriteriaConverter.
        var json = JsonSerializer.Serialize(_discoveryCriteria, Settings);

        var criteria = JsonSerializer.Deserialize<DiscoveryCriteria>(json, Settings)!;

        Assert.AreEqual(100, criteria.FrequencyOfDiscoveredTestsEvent);
        Assert.AreEqual("<RunConfiguration></RunConfiguration>", criteria.RunSettings);
        Assert.AreEqual("TestFilter", criteria.TestCaseFilter);
        Assert.AreEqual("sampleTest.dll", criteria.AdapterSourceMap["_none_"].Single());
        CollectionAssert.AreEqual(new[] { "sampleTest.dll" }, criteria.Sources.ToArray());
    }
}
#endif
