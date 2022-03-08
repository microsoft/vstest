// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Linq;

using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Discovery;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.CrossPlatEngine.UnitTests.Discovery;

[TestClass]
public class DiscoverySourceStatusCacheTests
{
    [DataTestMethod]
    [DataRow(DiscoveryStatus.FullyDiscovered)]
    [DataRow(DiscoveryStatus.PartiallyDiscovered)]
    [DataRow(DiscoveryStatus.NotDiscovered)]
    public void MarkSourcesWithStatusWhenSourcesIsNullDoesNothing(DiscoveryStatus discoveryStatus)
    {
        // Arrange
        var cache = new DiscoverySourceStatusCache();

        // Act
        cache.MarkSourcesWithStatus(null, discoveryStatus);

        // Assert
        Assert.AreEqual(0, cache.GetSourcesWithStatus(DiscoveryStatus.FullyDiscovered).Count);
        Assert.AreEqual(0, cache.GetSourcesWithStatus(DiscoveryStatus.PartiallyDiscovered).Count);
        Assert.AreEqual(0, cache.GetSourcesWithStatus(DiscoveryStatus.NotDiscovered).Count);
    }

    [DataTestMethod]
    [DataRow(DiscoveryStatus.FullyDiscovered)]
    [DataRow(DiscoveryStatus.PartiallyDiscovered)]
    [DataRow(DiscoveryStatus.NotDiscovered)]
    public void MarkSourcesWithStatusIgnoresNullSources(DiscoveryStatus discoveryStatus)
    {
        // Arrange
        var cache = new DiscoverySourceStatusCache();
        var sources = new[] { "a", null, "b" };

        // Sanity check
        Assert.AreEqual(0, cache.GetSourcesWithStatus(discoveryStatus).Count);

        // Act
        cache.MarkSourcesWithStatus(sources, discoveryStatus);

        // Assert
        CollectionAssert.AreEquivalent(new[] { "a", "b" }, cache.GetSourcesWithStatus(discoveryStatus));
    }

    [DataTestMethod]
    [DataRow(DiscoveryStatus.FullyDiscovered)]
    [DataRow(DiscoveryStatus.PartiallyDiscovered)]
    public void MarkSourcesWithStatusWhenSourceAddedAndStatusDifferentFromNotDiscoveredLogsWarning(DiscoveryStatus discoveryStatus)
    {
        // Arrange
        var cache = new DiscoverySourceStatusCache();

        // Act
        cache.MarkSourcesWithStatus(new[] { "a" }, discoveryStatus);

        // Assert
        CollectionAssert.AreEquivalent(new[] { "a" }, cache.GetSourcesWithStatus(discoveryStatus));
    }

    [DataTestMethod]
    [DataRow(DiscoveryStatus.NotDiscovered)]
    [DataRow(DiscoveryStatus.PartiallyDiscovered)]
    public void MarkSourcesWithStatusWhenSourceStatusWasFullyDiscoveredAndIsDowngradedLogsWarning(DiscoveryStatus discoveryStatus)
    {
        // Arrange
        var cache = new DiscoverySourceStatusCache();
        cache.MarkSourcesWithStatus(new[] { "a" }, DiscoveryStatus.FullyDiscovered);

        // Act
        cache.MarkSourcesWithStatus(new[] { "a" }, discoveryStatus);

        // Assert
        CollectionAssert.AreEquivalent(new[] { "a" }, cache.GetSourcesWithStatus(discoveryStatus));
    }

    [TestMethod]
    public void MarkSourcesWithStatusWhenSourceStatusWasPartiallyDiscoveredAndIsDowngradedLogsWarning()
    {
        // Arrange
        var cache = new DiscoverySourceStatusCache();
        cache.MarkSourcesWithStatus(new[] { "a" }, DiscoveryStatus.PartiallyDiscovered);

        // Act
        cache.MarkSourcesWithStatus(new[] { "a" }, DiscoveryStatus.NotDiscovered);

        // Assert
        CollectionAssert.AreEquivalent(new[] { "a" }, cache.GetSourcesWithStatus(DiscoveryStatus.NotDiscovered));
    }

    [TestMethod]
    public void MarkSourcesBasedOnDiscoveredTestCasesHandlesTestCasesFromMultipleSources()
    {
        // Arrange
        var cache = new DiscoverySourceStatusCache();
        var testCases = new TestCase[]
        {
            new() { Source = "a" },
            new() { Source = "a" },
            new() { Source = "a" },
            new() { Source = "b" },
            new() { Source = "c" },
        };

        // Act
        cache.MarkSourcesBasedOnDiscoveredTestCases(testCases, false);

        // Assert
        CollectionAssert.AreEquivalent(new[] { "a", "b" }, cache.GetSourcesWithStatus(DiscoveryStatus.FullyDiscovered));
        CollectionAssert.AreEquivalent(new[] { "c" }, cache.GetSourcesWithStatus(DiscoveryStatus.PartiallyDiscovered));
    }

    [TestMethod]
    public void MarkSourcesBasedOnDiscoveredTestCasesReuseLastDiscoveredSource()
    {
        // Arrange
        var cache = new DiscoverySourceStatusCache();
        cache.MarkSourcesBasedOnDiscoveredTestCases(new TestCase[] { new() { Source = "a" } }, false);

        // Act
        cache.MarkSourcesBasedOnDiscoveredTestCases(new TestCase[] { new() { Source = "b" }, }, false);

        // Assert
        CollectionAssert.AreEquivalent(new[] { "a" }, cache.GetSourcesWithStatus(DiscoveryStatus.FullyDiscovered));
        CollectionAssert.AreEquivalent(new[] { "b" }, cache.GetSourcesWithStatus(DiscoveryStatus.PartiallyDiscovered));
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void MarkSourcesBasedOnDiscoveredTestCasesWhenCompleteMarksLastSourceAsFullyDiscovered(bool trueIsEmptyFalseIsNull)
    {
        // Arrange
        var cache = new DiscoverySourceStatusCache();
        cache.MarkSourcesBasedOnDiscoveredTestCases(new TestCase[] { new() { Source = "a" } }, false);

        // Sanity check
        CollectionAssert.AreEquivalent(new[] { "a" }, cache.GetSourcesWithStatus(DiscoveryStatus.PartiallyDiscovered));
        Assert.AreEqual(0, cache.GetSourcesWithStatus(DiscoveryStatus.FullyDiscovered).Count);

        // Act
        cache.MarkSourcesBasedOnDiscoveredTestCases(trueIsEmptyFalseIsNull ? Enumerable.Empty<TestCase>() : null, true);

        // Assert
        CollectionAssert.AreEquivalent(new[] { "a" }, cache.GetSourcesWithStatus(DiscoveryStatus.FullyDiscovered));
        Assert.AreEqual(0, cache.GetSourcesWithStatus(DiscoveryStatus.PartiallyDiscovered).Count);
    }

    [TestMethod]
    public void MarkSourcesBasedOnDiscoveredTestCasesWhenCompleteMarksAllSourcesAsFullyDiscovered()
    {
        // Arrange
        var cache = new DiscoverySourceStatusCache();
        cache.MarkSourcesBasedOnDiscoveredTestCases(new TestCase[] { new() { Source = "a" } }, false);

        // Sanity check
        CollectionAssert.AreEquivalent(new[] { "a" }, cache.GetSourcesWithStatus(DiscoveryStatus.PartiallyDiscovered));
        Assert.AreEqual(0, cache.GetSourcesWithStatus(DiscoveryStatus.FullyDiscovered).Count);

        // Act
        cache.MarkSourcesBasedOnDiscoveredTestCases(new TestCase[]
            {
                new() { Source = "b" },
                new() { Source = "c" },
            }, true);

        // Assert
        CollectionAssert.AreEquivalent(new[] { "a", "b", "c" }, cache.GetSourcesWithStatus(DiscoveryStatus.FullyDiscovered));
        Assert.AreEqual(0, cache.GetSourcesWithStatus(DiscoveryStatus.PartiallyDiscovered).Count);
    }

    [DataTestMethod]
    [DataRow(DiscoveryStatus.FullyDiscovered)]
    [DataRow(DiscoveryStatus.PartiallyDiscovered)]
    [DataRow(DiscoveryStatus.NotDiscovered)]
    public void GetSourcesWithStatusWhenEmptyDictionaryReturnsEmptyList(DiscoveryStatus discoveryStatus)
    {
        var instanceSources = new DiscoverySourceStatusCache().GetSourcesWithStatus(discoveryStatus);
        Assert.AreEqual(0, instanceSources.Count);

        var givenDicSources = DiscoverySourceStatusCache.GetSourcesWithStatus(discoveryStatus, new());
        Assert.AreEqual(0, givenDicSources.Count);
    }

    [TestMethod]
    public void GetSourcesWithStatusCorrectlyFiltersSources()
    {
        // Arrange
        var dic = new ConcurrentDictionary<string, DiscoveryStatus>
        {
            ["fully1"] = DiscoveryStatus.FullyDiscovered,
            ["fully2"] = DiscoveryStatus.FullyDiscovered,
            ["fully3"] = DiscoveryStatus.FullyDiscovered,
            ["fully4"] = DiscoveryStatus.FullyDiscovered,
            ["fully5"] = DiscoveryStatus.FullyDiscovered,

            ["partially1"] = DiscoveryStatus.PartiallyDiscovered,
            ["partially2"] = DiscoveryStatus.PartiallyDiscovered,
            ["partially3"] = DiscoveryStatus.PartiallyDiscovered,
            ["partially4"] = DiscoveryStatus.PartiallyDiscovered,

            ["not1"] = DiscoveryStatus.NotDiscovered,
            ["not2"] = DiscoveryStatus.NotDiscovered,
            ["not3"] = DiscoveryStatus.NotDiscovered,
        };

        // Act
        var fullyDiscovered = DiscoverySourceStatusCache.GetSourcesWithStatus(DiscoveryStatus.FullyDiscovered, dic);
        var partiallyDiscovered = DiscoverySourceStatusCache.GetSourcesWithStatus(DiscoveryStatus.PartiallyDiscovered, dic);
        var notDiscovered = DiscoverySourceStatusCache.GetSourcesWithStatus(DiscoveryStatus.NotDiscovered, dic);

        // Assert
        CollectionAssert.AreEquivalent(
            new[]
            {
                "fully1",
                "fully2",
                "fully3",
                "fully4",
                "fully5",
            }, fullyDiscovered);

        CollectionAssert.AreEquivalent(
            new[]
            {
                "partially1",
                "partially2",
                "partially3",
                "partially4",
            }, partiallyDiscovered);

        CollectionAssert.AreEquivalent(
            new[]
            {
                "not1",
                "not2",
                "not3",
            }, notDiscovered);
    }
}
