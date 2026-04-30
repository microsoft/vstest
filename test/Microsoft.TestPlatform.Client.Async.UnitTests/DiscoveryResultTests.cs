// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.Client.Async.UnitTests;

[TestClass]
public class DiscoveryResultTests
{
    [TestMethod]
    public void Constructor_SetsProperties()
    {
        var testCases = new List<TestCase>
        {
            new("Test1", new Uri("executor://test"), "source.dll"),
            new("Test2", new Uri("executor://test"), "source.dll"),
        };

        var result = new DiscoveryResult(testCases, totalCount: 5, isAborted: false);

        Assert.AreEqual(2, result.TestCases.Count);
        Assert.AreEqual(5, result.TotalCount);
        Assert.IsFalse(result.IsAborted);
    }

    [TestMethod]
    public void Constructor_Aborted_SetsFlag()
    {
        var result = new DiscoveryResult(Array.Empty<TestCase>(), totalCount: 0, isAborted: true);

        Assert.IsTrue(result.IsAborted);
        Assert.AreEqual(0, result.TestCases.Count);
    }
}
