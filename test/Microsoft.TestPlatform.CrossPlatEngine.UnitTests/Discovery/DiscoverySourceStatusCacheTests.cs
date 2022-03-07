// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
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
        CollectionAssert.AreEquivalent(new List<string> { "a", "b" }, cache.GetSourcesWithStatus(discoveryStatus));
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
        CollectionAssert.AreEquivalent(new List<string> { "a" }, cache.GetSourcesWithStatus(discoveryStatus));
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
        CollectionAssert.AreEquivalent(new List<string> { "a" }, cache.GetSourcesWithStatus(discoveryStatus));
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
        CollectionAssert.AreEquivalent(new List<string> { "a" }, cache.GetSourcesWithStatus(DiscoveryStatus.NotDiscovered));
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void MarkTheLastChunkSourcesAsFullyDiscoveredWhenLastChunkIsNullOrEmptyUsesPreviousSource(bool isEmpty)
    {
        // Arrange
        var cache = new DiscoverySourceStatusCache();
        cache.MarkSourcesBasedOnDiscoveredTestCases(new TestCase[] { new() { Source = "a" } });

        // Sanity check
        CollectionAssert.AreEquivalent(new List<string> { "a" }, cache.GetSourcesWithStatus(DiscoveryStatus.PartiallyDiscovered));
        Assert.AreEqual(0, cache.GetSourcesWithStatus(DiscoveryStatus.FullyDiscovered).Count);

        // Act
        cache.MarkTheLastChunkSourcesAsFullyDiscovered(isEmpty ? Enumerable.Empty<TestCase>() : null);

        // Assert
        CollectionAssert.AreEquivalent(new List<string> { "a" }, cache.GetSourcesWithStatus(DiscoveryStatus.FullyDiscovered));
        Assert.AreEqual(0, cache.GetSourcesWithStatus(DiscoveryStatus.PartiallyDiscovered).Count);
    }

    [TestMethod]
    public void MarkTheLastChunkSourcesAsFullyDiscoveredMarkAllSourcesAsFullyDiscoveredRegardlessOfPreviousState()
    {
        // Arrange
        var cache = new DiscoverySourceStatusCache();
        cache.MarkSourcesWithStatus(new[] { "old" }, DiscoveryStatus.NotDiscovered);
        var testCases = new TestCase[]
        {
            new() { Source = "a" },
            new() { Source = "b" },
            new() { Source = "c" },
        };

        // Sanity check
        CollectionAssert.AreEquivalent(new List<string> { "old" }, cache.GetSourcesWithStatus(DiscoveryStatus.NotDiscovered));
        Assert.AreEqual(0, cache.GetSourcesWithStatus(DiscoveryStatus.PartiallyDiscovered).Count);
        Assert.AreEqual(0, cache.GetSourcesWithStatus(DiscoveryStatus.FullyDiscovered).Count);

        // Act
        cache.MarkTheLastChunkSourcesAsFullyDiscovered(testCases);

        // Assert
        CollectionAssert.AreEquivalent(new List<string> { "old" }, cache.GetSourcesWithStatus(DiscoveryStatus.NotDiscovered));
        Assert.AreEqual(0, cache.GetSourcesWithStatus(DiscoveryStatus.PartiallyDiscovered).Count);
        CollectionAssert.AreEquivalent(new List<string> { "a", "b", "c", }, cache.GetSourcesWithStatus(DiscoveryStatus.FullyDiscovered));
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
        cache.MarkSourcesBasedOnDiscoveredTestCases(testCases);

        // Assert
        CollectionAssert.AreEquivalent(new List<string> { "a", "b" }, cache.GetSourcesWithStatus(DiscoveryStatus.FullyDiscovered));
        CollectionAssert.AreEquivalent(new List<string> { "c" }, cache.GetSourcesWithStatus(DiscoveryStatus.PartiallyDiscovered));
    }
    [TestMethod]
    public void MarkSourcesBasedOnDiscoveredTestCasesReuseLastDiscoveredSource()
    {
        // Arrange
        var cache = new DiscoverySourceStatusCache();
        cache.MarkSourcesBasedOnDiscoveredTestCases(new TestCase[] { new() { Source = "a" } });

        // Act
        cache.MarkSourcesBasedOnDiscoveredTestCases(new TestCase[] { new() { Source = "b" }, });

        // Assert
        CollectionAssert.AreEquivalent(new List<string> { "a" }, cache.GetSourcesWithStatus(DiscoveryStatus.FullyDiscovered));
        CollectionAssert.AreEquivalent(new List<string> { "b" }, cache.GetSourcesWithStatus(DiscoveryStatus.PartiallyDiscovered));
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

    [DataTestMethod]
    [DataRow(DiscoveryStatus.FullyDiscovered)]
    [DataRow(DiscoveryStatus.PartiallyDiscovered)]
    [DataRow(DiscoveryStatus.NotDiscovered)]
    public void GetSourcesWithStatusWhenNullDictionaryReturnsEmptyList(DiscoveryStatus discoveryStatus)
    {
        var givenDicSources = DiscoverySourceStatusCache.GetSourcesWithStatus(discoveryStatus, null);
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
            new List<string>
            {
                "fully1",
                "fully2",
                "fully3",
                "fully4",
                "fully5",
            }, fullyDiscovered);

        CollectionAssert.AreEquivalent(
            new List<string>
            {
                "partially1",
                "partially2",
                "partially3",
                "partially4",
            }, partiallyDiscovered);

        CollectionAssert.AreEquivalent(
            new List<string>
            {
                "not1",
                "not2",
                "not3",
            }, notDiscovered);
    }
}
