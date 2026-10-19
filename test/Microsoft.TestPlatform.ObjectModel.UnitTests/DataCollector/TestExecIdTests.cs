// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.ObjectModel.UnitTests;

[TestClass]
public class TestExecIdTests
{
    [TestMethod]
    public void EqualityOperatorShouldReturnTrueForDistinctInstancesWithSameId()
    {
        var id = Guid.NewGuid();
        var left = new TestExecId(id);
        var right = new TestExecId(id);

        bool equalityResult = left == right;
        bool inequalityResult = left != right;
        Assert.IsTrue(equalityResult);
        Assert.IsFalse(inequalityResult);
    }

    [TestMethod]
    public void EqualityOperatorShouldReturnFalseForDifferentIds()
    {
        var left = new TestExecId(Guid.NewGuid());
        var right = new TestExecId(Guid.NewGuid());

        bool equalityResult = left == right;
        bool inequalityResult = left != right;
        Assert.IsFalse(equalityResult);
        Assert.IsTrue(inequalityResult);
    }

    [TestMethod]
    public void EqualityOperatorShouldHandleNullOperands()
    {
        TestExecId? left = null;
        TestExecId? right = null;

        bool bothNullEquality = left == right;
        bool bothNullInequality = left != right;
        Assert.IsTrue(bothNullEquality);
        Assert.IsFalse(bothNullInequality);

        right = new TestExecId(Guid.NewGuid());
        bool leftNullEquality = left == right;
        bool leftNullInequality = left != right;
        Assert.IsFalse(leftNullEquality);
        Assert.IsTrue(leftNullInequality);

        left = right;
        right = null;
        bool rightNullEquality = left == right;
        bool rightNullInequality = left != right;
        Assert.IsFalse(rightNullEquality);
        Assert.IsTrue(rightNullInequality);
    }

    [TestMethod]
    public void EqualityOperatorShouldReturnTrueForSameReference()
    {
        var instance = new TestExecId(Guid.NewGuid());
        var alias = instance;

        bool equalityResult = instance == alias;
        bool inequalityResult = instance != alias;
        Assert.IsTrue(equalityResult);
        Assert.IsFalse(inequalityResult);
    }
}
