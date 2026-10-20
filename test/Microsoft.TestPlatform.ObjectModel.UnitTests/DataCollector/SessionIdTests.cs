// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.ObjectModel.UnitTests;

[TestClass]
public class SessionIdTests
{
    [TestMethod]
    public void EqualityOperatorShouldReturnTrueForDistinctInstancesWithSameId()
    {
        var id = Guid.NewGuid();
        var left = new SessionId(id);
        var right = new SessionId(id);

        bool equalityResult = left == right;
        bool inequalityResult = left != right;
        Assert.IsTrue(equalityResult);
        Assert.IsFalse(inequalityResult);
    }

    [TestMethod]
    public void EqualityOperatorShouldReturnFalseForDifferentIds()
    {
        var left = new SessionId(Guid.NewGuid());
        var right = new SessionId(Guid.NewGuid());

        bool equalityResult = left == right;
        bool inequalityResult = left != right;
        Assert.IsFalse(equalityResult);
        Assert.IsTrue(inequalityResult);
    }

    [TestMethod]
    public void EqualityOperatorShouldHandleNullOperands()
    {
        SessionId? left = null;
        SessionId? right = null;

        bool bothNullEquality = left == right;
        bool bothNullInequality = left != right;
        Assert.IsTrue(bothNullEquality);
        Assert.IsFalse(bothNullInequality);

        right = new SessionId(Guid.NewGuid());
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
        var instance = new SessionId(Guid.NewGuid());
        var alias = instance;

        bool equalityResult = instance == alias;
        bool inequalityResult = instance != alias;
        Assert.IsTrue(equalityResult);
        Assert.IsFalse(inequalityResult);
    }
}
