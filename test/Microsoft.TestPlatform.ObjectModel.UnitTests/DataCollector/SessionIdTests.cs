// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.ObjectModel.UnitTests;

// Regression tests for https://github.com/microsoft/vstest/issues/16585 (Task 1).
// SessionId overrode Equals/GetHashCode but did not define operator ==/!=, so C#
// fell back to reference equality for == comparisons between distinct instances
// carrying the same underlying Guid. These tests exercise the operators directly
// (rather than via Assert.AreEqual) to guard against that regression.
#pragma warning disable MSTEST0037 // Intentionally exercising operator ==/!= rather than Assert.AreEqual/IsNotNull.
#pragma warning disable CS1718 // Comparison made to same variable is intentional, to exercise the reflexive equality operator.
[TestClass]
public class SessionIdTests
{
    [TestMethod]
    public void EqualityOperatorShouldReturnTrueForDistinctInstancesWithSameId()
    {
        var id = Guid.NewGuid();
        var left = new SessionId(id);
        var right = new SessionId(id);

        Assert.IsTrue(left == right);
        Assert.IsFalse(left != right);
    }

    [TestMethod]
    public void EqualityOperatorShouldReturnFalseForDifferentIds()
    {
        var left = new SessionId(Guid.NewGuid());
        var right = new SessionId(Guid.NewGuid());

        Assert.IsFalse(left == right);
        Assert.IsTrue(left != right);
    }

    [TestMethod]
    public void EqualityOperatorShouldHandleNullOperands()
    {
        SessionId? left = null;
        SessionId? right = null;
        var instance = new SessionId(Guid.NewGuid());

        Assert.IsTrue(left == right);
        Assert.IsFalse(left != right);

        Assert.IsFalse(instance == null);
        Assert.IsFalse(null == instance);
        Assert.IsTrue(instance != null);
        Assert.IsTrue(null != instance);
    }

    [TestMethod]
    public void EqualityOperatorShouldReturnTrueForSameReference()
    {
        var instance = new SessionId(Guid.NewGuid());

        Assert.IsTrue(instance == instance);
        Assert.IsFalse(instance != instance);
    }
}
#pragma warning restore CS1718
#pragma warning restore MSTEST0037
