// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestPlatform.Common.Filtering;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.Library.IntegrationTests;

/// <summary>
/// Integration tests for <see cref="FilterExpressionWrapper"/> exercising the full vstest API
/// (IS_VSTEST_REPO), which includes <see cref="FilterOptions"/> and the <c>propertyProvider</c>
/// overload of <c>ValidForProperties</c>. The companion unit-test project covers the
/// source-only-package (non-IS_VSTEST_REPO) surface area.
/// </summary>
[TestClass]
public class FilterExpressionWrapperIntegrationTests
{
    [TestMethod]
    public void ConstructorWithFilterOptionsShouldSetFilterOptionsProperty()
    {
        var options = new FilterOptions { FilterRegEx = @"^[^\s\(]+" };
        var wrapper = new FilterExpressionWrapper("FullyQualifiedName=Test1", options);

        Assert.AreSame(options, wrapper.FilterOptions);
    }

    [TestMethod]
    public void ConstructorWithNullFilterOptionsShouldLeaveFilterOptionsNull()
    {
        var wrapper = new FilterExpressionWrapper("Name=Test1", null);

        Assert.IsNull(wrapper.FilterOptions);
    }

    [TestMethod]
    public void FilterRegExShouldBeAppliedToPropertyValueBeforeMatching()
    {
        // The regex matches the full value before comparison, so values matching the regex pattern
        // are compared against the filter values.
        var options = new FilterOptions { FilterRegEx = @"^[^\s\(]+" };
        var wrapper = new FilterExpressionWrapper(
            "FullyQualifiedName=MyNamespace.TestClass.Test1|FullyQualifiedName=MyNamespace.TestClass.Test2",
            options);

        Assert.IsNotNull(wrapper.FastFilter);
        Assert.IsNull(wrapper.ParseError);

        Assert.IsTrue(wrapper.Evaluate(p => p == "FullyQualifiedName" ? "MyNamespace.TestClass.Test1" : null));
        Assert.IsTrue(wrapper.Evaluate(p => p == "FullyQualifiedName" ? "MyNamespace.TestClass.Test2" : null));
        Assert.IsFalse(wrapper.Evaluate(p => p == "FullyQualifiedName" ? "MyNamespace.TestClass.Test3" : null));
    }

    [TestMethod]
    public void FilterRegExReplacementShouldStripParameterSuffixBeforeMatching()
    {
        // FilterRegExReplacement removes the parameter list "(int, string)" from the fully qualified name.
        var options = new FilterOptions
        {
            FilterRegEx = @"\s*\([^\)]*\)",
            FilterRegExReplacement = string.Empty,
        };
        var wrapper = new FilterExpressionWrapper(
            "FullyQualifiedName=TestClass.Test1|FullyQualifiedName=TestClass.Test2",
            options);

        Assert.IsNotNull(wrapper.FastFilter);
        Assert.IsNull(wrapper.ParseError);

        // "TestClass.Test1(int, string)" after replacement becomes "TestClass.Test1" — matches.
        Assert.IsTrue(wrapper.Evaluate(p => p == "FullyQualifiedName" ? "TestClass.Test1(int, string)" : null));
        Assert.IsTrue(wrapper.Evaluate(p => p == "FullyQualifiedName" ? "TestClass.Test2(bool)" : null));
        Assert.IsFalse(wrapper.Evaluate(p => p == "FullyQualifiedName" ? "TestClass.Test3()" : null));
    }

    [TestMethod]
    public void InvalidFilterRegExShouldSetParseError()
    {
        var options = new FilterOptions { FilterRegEx = @"^[^\s\(]+\1" }; // \1 is an invalid back-reference
        var wrapper = new FilterExpressionWrapper("FullyQualifiedName=Test", options);

        Assert.IsNull(wrapper.FastFilter);
        Assert.IsFalse(string.IsNullOrEmpty(wrapper.ParseError));
    }

    [TestMethod]
    public void ValidForPropertiesWithPropertyProviderShouldReturnNullForKnownProperty()
    {
        var wrapper = new FilterExpressionWrapper("FullyQualifiedName=Test1");
        var supported = new List<string> { "FullyQualifiedName" };

        string[]? invalid = wrapper.ValidForProperties(
            supported,
            p => p == "FullyQualifiedName" ? TestCaseProperties.FullyQualifiedName : null);

        Assert.IsNull(invalid);
    }

    [TestMethod]
    public void ValidForPropertiesWithPropertyProviderShouldReturnUnsupportedPropertyNames()
    {
        var wrapper = new FilterExpressionWrapper("UnknownProp=Value");
        var supported = new List<string> { "FullyQualifiedName" };

        string[]? invalid = wrapper.ValidForProperties(supported, _ => null);

        Assert.IsNotNull(invalid);
        Assert.AreEqual("UnknownProp", invalid.Single());
    }

    [TestMethod]
    public void ValidForPropertiesWithNullProviderShouldStillValidateAgainstSupportedList()
    {
        var wrapper = new FilterExpressionWrapper("FullyQualifiedName=Test1");
        var supported = new List<string> { "FullyQualifiedName" };

        string[]? invalid = wrapper.ValidForProperties(supported, null);

        Assert.IsNull(invalid);
    }
}

/// <summary>
/// Integration tests for <see cref="TestCaseFilterExpression"/> exercising the full vstest API
/// (IS_VSTEST_REPO), in particular:
/// <list type="bullet">
///   <item><c>MatchTestCase(TestCase, Func&lt;string,object?&gt;)</c> with real <see cref="TestCase"/> objects</item>
///   <item><c>ValidForProperties</c> with a <c>Func&lt;string, TestProperty?&gt;</c> provider</item>
///   <item>The class implements <see cref="ITestCaseFilterExpression"/></item>
/// </list>
/// </summary>
[TestClass]
public class TestCaseFilterExpressionIntegrationTests
{
    private static TestCase CreateTestCase(string fullyQualifiedName)
        => new(fullyQualifiedName, new Uri("executor://mstest/v2"), "assembly.dll");

    [TestMethod]
    public void TestCaseFilterExpressionShouldImplementITestCaseFilterExpression()
    {
        var wrapper = new FilterExpressionWrapper("FullyQualifiedName=Test1");
        ITestCaseFilterExpression expression = new TestCaseFilterExpression(wrapper);

        Assert.IsNotNull(expression);
    }

    [TestMethod]
    public void MatchTestCaseShouldReturnTrueWhenFullyQualifiedNameMatchesFilter()
    {
        var wrapper = new FilterExpressionWrapper("FullyQualifiedName=MyNamespace.MyClass.Test1");
        var expression = new TestCaseFilterExpression(wrapper);
        var testCase = CreateTestCase("MyNamespace.MyClass.Test1");

        bool result = expression.MatchTestCase(testCase, p => p == "FullyQualifiedName" ? testCase.FullyQualifiedName : null);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void MatchTestCaseShouldReturnFalseWhenFullyQualifiedNameDoesNotMatchFilter()
    {
        var wrapper = new FilterExpressionWrapper("FullyQualifiedName=MyNamespace.MyClass.Test1");
        var expression = new TestCaseFilterExpression(wrapper);
        var testCase = CreateTestCase("MyNamespace.MyClass.Test2");

        bool result = expression.MatchTestCase(testCase, p => p == "FullyQualifiedName" ? testCase.FullyQualifiedName : null);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void MatchTestCaseShouldReturnFalseWhenFilterHasParseError()
    {
        var wrapper = new FilterExpressionWrapper("(FullyQualifiedName=Test1");
        var expression = new TestCaseFilterExpression(wrapper);
        var testCase = CreateTestCase("MyNamespace.MyClass.Test1");

        bool result = expression.MatchTestCase(testCase, p => "Test1");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void MatchTestCaseShouldSupportOrOperatorWithMultipleTestCases()
    {
        var wrapper = new FilterExpressionWrapper(
            "FullyQualifiedName=MyNamespace.MyClass.Test1|FullyQualifiedName=MyNamespace.MyClass.Test2");
        var expression = new TestCaseFilterExpression(wrapper);

        var test1 = CreateTestCase("MyNamespace.MyClass.Test1");
        var test2 = CreateTestCase("MyNamespace.MyClass.Test2");
        var test3 = CreateTestCase("MyNamespace.MyClass.Test3");

        Assert.IsTrue(expression.MatchTestCase(test1, p => p == "FullyQualifiedName" ? test1.FullyQualifiedName : null));
        Assert.IsTrue(expression.MatchTestCase(test2, p => p == "FullyQualifiedName" ? test2.FullyQualifiedName : null));
        Assert.IsFalse(expression.MatchTestCase(test3, p => p == "FullyQualifiedName" ? test3.FullyQualifiedName : null));
    }

    [TestMethod]
    public void MatchTestCaseShouldSupportCustomPropertyOnTestCase()
    {
        var categoryProperty = TestProperty.Register(
            "FilterSourceIntegrationTests.Category", "Category", typeof(string), typeof(TestCase));

        var wrapper = new FilterExpressionWrapper("Category=Integration");
        var expression = new TestCaseFilterExpression(wrapper);

        var matchingTestCase = CreateTestCase("MyNamespace.MyClass.IntegrationTest");
        matchingTestCase.SetPropertyValue(categoryProperty, "Integration");

        var nonMatchingTestCase = CreateTestCase("MyNamespace.MyClass.UnitTest");
        nonMatchingTestCase.SetPropertyValue(categoryProperty, "Unit");

        Assert.IsTrue(expression.MatchTestCase(
            matchingTestCase,
            p => p == "Category" ? matchingTestCase.GetPropertyValue<string>(categoryProperty, null) : null));
        Assert.IsFalse(expression.MatchTestCase(
            nonMatchingTestCase,
            p => p == "Category" ? nonMatchingTestCase.GetPropertyValue<string>(categoryProperty, null) : null));
    }

    [TestMethod]
    public void ValidForPropertiesWithPropertyProviderShouldReturnNullWhenAllPropertiesAreSupported()
    {
        var wrapper = new FilterExpressionWrapper("FullyQualifiedName=Test1");
        var expression = new TestCaseFilterExpression(wrapper);
        var supported = new List<string> { "FullyQualifiedName" };

        string[]? invalid = expression.ValidForProperties(
            supported,
            p => p == "FullyQualifiedName" ? TestCaseProperties.FullyQualifiedName : null);

        Assert.IsNull(invalid);
    }

    [TestMethod]
    public void ValidForPropertiesWithPropertyProviderShouldReturnUnsupportedPropertyNames()
    {
        var wrapper = new FilterExpressionWrapper("UnknownProp=Value");
        var expression = new TestCaseFilterExpression(wrapper);
        var supported = new List<string> { "FullyQualifiedName" };

        string[]? invalid = expression.ValidForProperties(supported, _ => null);

        Assert.IsNotNull(invalid);
        Assert.AreEqual("UnknownProp", invalid.Single());
    }

    [TestMethod]
    public void ValidForPropertiesWithPropertyProviderShouldReturnNullWhenFilterHasParseError()
    {
        var wrapper = new FilterExpressionWrapper("(FullyQualifiedName=Test1");
        var expression = new TestCaseFilterExpression(wrapper);
        var supported = new List<string> { "FullyQualifiedName" };

        string[]? invalid = expression.ValidForProperties(
            supported,
            p => TestCaseProperties.FullyQualifiedName);

        Assert.IsNull(invalid);
    }
}
