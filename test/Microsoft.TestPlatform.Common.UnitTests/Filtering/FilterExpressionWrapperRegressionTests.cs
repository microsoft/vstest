// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestPlatform.Common.Filtering;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.Common.UnitTests.Filtering;

/// <summary>
/// Regression tests for FilterExpressionWrapper behavior and edge cases.
/// </summary>
[TestClass]
public class FilterExpressionWrapperRegressionTests
{
    [TestMethod]
    public void FilterExpressionWrapper_NullFilter_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new FilterExpressionWrapper(null!, null));
    }

    [TestMethod]
    public void FilterExpressionWrapper_EmptyFilter_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new FilterExpressionWrapper(string.Empty, null));
    }

    [TestMethod]
    public void FilterExpressionWrapper_ValidFilter_ParseErrorShouldBeNull()
    {
        var wrapper = new FilterExpressionWrapper("FullyQualifiedName=Test1");

        Assert.IsTrue(string.IsNullOrEmpty(wrapper.ParseError),
            "A valid filter should not produce a parse error.");
    }

    [TestMethod]
    public void FilterExpressionWrapper_InvalidFilter_ParseErrorShouldNotBeNull()
    {
        var wrapper = new FilterExpressionWrapper("((Invalid)");

        Assert.IsFalse(string.IsNullOrEmpty(wrapper.ParseError),
            "An invalid filter should produce a parse error.");
    }

    [TestMethod]
    public void FilterExpressionWrapper_EvaluateShouldMatchCaseInsensitively()
    {
        var wrapper = new FilterExpressionWrapper("FullyQualifiedName=testmethod");

        // "TESTMETHOD" should match "testmethod" due to case-insensitive comparison.
        bool result = wrapper.Evaluate(s => s == "FullyQualifiedName" ? "TESTMETHOD" : null);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ValidForPropertiesShouldReturnInvalidPropertyNames()
    {
        var wrapper = new FilterExpressionWrapper("UnknownProp=Value1|AnotherBadProp=Value2");

        var invalidProperties = wrapper.ValidForProperties(
            new List<string> { "FullyQualifiedName", "TestCategory" }, s => null);

        Assert.IsNotNull(invalidProperties);
        Assert.HasCount(2, invalidProperties);
    }
}
