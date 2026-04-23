// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestPlatform.Common.Filtering;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.Common.UnitTests.Filtering;

[TestClass]
public class TestCaseFilterExpressionTests
{
    [TestMethod]
    public void ValidForPropertiesShouldNotSetvalidForMatchVariableTofalseIfFilterIsInvalid()
    {
        var filterExpressionWrapper = new FilterExpressionWrapper("highlyunlikelyproperty=unused");
        var testCaseFilterExpression = new TestCaseFilterExpression(filterExpressionWrapper);

        Assert.AreEqual("highlyunlikelyproperty",
            testCaseFilterExpression.ValidForProperties(new List<string>() { "TestCategory" }, s => null)!.Single());

        TestCase dummyTestCase = new();
        bool result = testCaseFilterExpression.MatchTestCase(dummyTestCase, s => "unused");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ValidForPropertiesShouldReturnNullWhenAllPropertiesAreKnown()
    {
        var filterExpressionWrapper = new FilterExpressionWrapper("FullyQualifiedName=Ns.Class.Method");
        var testCaseFilterExpression = new TestCaseFilterExpression(filterExpressionWrapper);

        var invalidProperties = testCaseFilterExpression.ValidForProperties(
            new List<string> { "FullyQualifiedName", "TestCategory" }, s => null);

        Assert.IsNull(invalidProperties, "No invalid properties expected when all filter properties are known.");
    }

    [TestMethod]
    public void MatchTestCaseShouldReturnFalseWhenPropertyValueDoesNotMatch()
    {
        var filterExpressionWrapper = new FilterExpressionWrapper("FullyQualifiedName=Ns.Class.Method1");
        var testCaseFilterExpression = new TestCaseFilterExpression(filterExpressionWrapper);

        TestCase testCase = new("Ns.Class.Method2", new System.Uri("executor://test"), "test.dll");
        bool result = testCaseFilterExpression.MatchTestCase(testCase,
            s => s == "FullyQualifiedName" ? testCase.FullyQualifiedName : null);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void MatchTestCaseShouldReturnTrueWhenPropertyValueMatches()
    {
        var filterExpressionWrapper = new FilterExpressionWrapper("FullyQualifiedName=Ns.Class.Method1");
        var testCaseFilterExpression = new TestCaseFilterExpression(filterExpressionWrapper);

        TestCase testCase = new("Ns.Class.Method1", new System.Uri("executor://test"), "test.dll");
        bool result = testCaseFilterExpression.MatchTestCase(testCase,
            s => s == "FullyQualifiedName" ? testCase.FullyQualifiedName : null);

        Assert.IsTrue(result);
    }
}
