// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

using Microsoft.VisualStudio.TestPlatform.Common.Filtering;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.Common.UnitTests.Filtering;

[TestClass]
public class ConditionTests
{
    [TestMethod]
    public void ParseShouldThrownFormatExceptionOnNullConditionString()
    {
        string? conditionString = null;
        Assert.ThrowsExactly<FormatException>(() => Condition.Parse(conditionString));
    }

    [TestMethod]
    public void ParseShouldThrownFormatExceptionOnEmptyConditionString()
    {
        var conditionString = "";
        Assert.ThrowsExactly<FormatException>(() => Condition.Parse(conditionString));
    }

    [TestMethod]
    public void ParseShouldCreateConditionWithEmptyValueOnTrailingOperator()
    {
        var conditionString = "PropertyName=";
        Condition condition = Condition.Parse(conditionString);
        Assert.AreEqual("PropertyName", condition.Name);
        Assert.AreEqual(Operation.Equal, condition.Operation);
        Assert.AreEqual(string.Empty, condition.Value);
    }

    [TestMethod]
    public void ParseShouldCreateDefaultConditionWhenOnlyPropertyValuePassed()
    {
        var conditionString = "ABC";
        Condition condition = Condition.Parse(conditionString);
        Assert.AreEqual(Condition.DefaultPropertyName, condition.Name);
        Assert.AreEqual(Operation.Contains, condition.Operation);
        Assert.AreEqual(conditionString, condition.Value);
    }

    [TestMethod]
    public void ParseShouldCreateProperConditionOnValidConditionString()
    {
        var conditionString = "PropertyName=PropertyValue";
        Condition condition = Condition.Parse(conditionString);
        Assert.AreEqual("PropertyName", condition.Name);
        Assert.AreEqual(Operation.Equal, condition.Operation);
        Assert.AreEqual("PropertyValue", condition.Value);
    }

    [TestMethod]
    public void ParseShouldHandleEscapedString()
    {
        var conditionString = @"FullyQualifiedName=TestClass1\(""hello""\).TestMethod\(1.5\)";

        Condition condition = Condition.Parse(conditionString);
        Assert.AreEqual("FullyQualifiedName", condition.Name);
        Assert.AreEqual(Operation.Equal, condition.Operation);
        Assert.AreEqual(@"TestClass1(""hello"").TestMethod(1.5)", condition.Value);
    }

    [TestMethod]
    public void ParseShouldHandleEscapedBang()
    {
        var conditionString = @"FullyQualifiedName!=TestClass1\(""\!""\).TestMethod\(1.5\)";

        Condition condition = Condition.Parse(conditionString);
        Assert.AreEqual("FullyQualifiedName", condition.Name);
        Assert.AreEqual(Operation.NotEqual, condition.Operation);
        Assert.AreEqual(@"TestClass1(""!"").TestMethod(1.5)", condition.Value);
    }

    [TestMethod]
    public void ParseShouldHandleEscapedNotEqual()
    {
        var conditionString = @"FullyQualifiedName!=TestClass1\(""\!\=""\).TestMethod\(1.5\)";

        Condition condition = Condition.Parse(conditionString);
        Assert.AreEqual("FullyQualifiedName", condition.Name);
        Assert.AreEqual(Operation.NotEqual, condition.Operation);
        Assert.AreEqual(@"TestClass1(""!="").TestMethod(1.5)", condition.Value);
    }

    [TestMethod]
    public void ParseShouldHandleEscapedTilde()
    {
        var conditionString = @"FullyQualifiedName~TestClass1\(""\~""\).TestMethod\(1.5\)";

        Condition condition = Condition.Parse(conditionString);
        Assert.AreEqual("FullyQualifiedName", condition.Name);
        Assert.AreEqual(Operation.Contains, condition.Operation);
        Assert.AreEqual(@"TestClass1(""~"").TestMethod(1.5)", condition.Value);
    }

    [TestMethod]
    public void ParseShouldHandleEscapedNotTilde()
    {
        var conditionString = @"FullyQualifiedName!~TestClass1\(""\!\~""\).TestMethod\(1.5\)";

        Condition condition = Condition.Parse(conditionString);
        Assert.AreEqual("FullyQualifiedName", condition.Name);
        Assert.AreEqual(Operation.NotContains, condition.Operation);
        Assert.AreEqual(@"TestClass1(""!~"").TestMethod(1.5)", condition.Value);
    }

    [TestMethod]
    public void ParseStringWithSingleUnescapedBangThrowsFormatException1()
    {
        var conditionString = @"FullyQualifiedName=Test1(""!"")";

        Assert.ThrowsExactly<FormatException>(() => Condition.Parse(conditionString));
    }

    [TestMethod]
    public void ParseStringWithSingleUnescapedBangThrowsFormatException2()
    {
        var conditionString = @"FullyQualifiedName!Test1()";

        Assert.ThrowsExactly<FormatException>(() => Condition.Parse(conditionString));
    }

    [TestMethod]
    public void TokenizeNullThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => Condition.TokenizeFilterConditionString(null!), "str");
    }

    [TestMethod]
    public void TokenizeConditionShouldHandleEscapedBang()
    {
        var conditionString = @"FullyQualifiedName=TestMethod\(""\!""\)";

        var tokens = Condition.TokenizeFilterConditionString(conditionString).ToArray();

        Assert.HasCount(3, tokens);
        Assert.AreEqual("FullyQualifiedName", tokens[0]);
        Assert.AreEqual("=", tokens[1]);
        Assert.AreEqual(@"TestMethod\(""\!""\)", tokens[2]);
    }

    [TestMethod]
    public void TokenizeConditionShouldHandleEscapedNotEqual1()
    {
        var conditionString = @"FullyQualifiedName=TestMethod\(""\!\=""\)";

        var tokens = Condition.TokenizeFilterConditionString(conditionString).ToArray();

        Assert.HasCount(3, tokens);
        Assert.AreEqual("FullyQualifiedName", tokens[0]);
        Assert.AreEqual("=", tokens[1]);
        Assert.AreEqual(@"TestMethod\(""\!\=""\)", tokens[2]);
    }

    [TestMethod]
    public void TokenizeConditionShouldHandleEscapedNotEqual2()
    {
        var conditionString = @"FullyQualifiedName!=TestMethod\(""\!\=""\)";

        var tokens = Condition.TokenizeFilterConditionString(conditionString).ToArray();

        Assert.HasCount(3, tokens);
        Assert.AreEqual("FullyQualifiedName", tokens[0]);
        Assert.AreEqual("!=", tokens[1]);
        Assert.AreEqual(@"TestMethod\(""\!\=""\)", tokens[2]);
    }

    [TestMethod]
    public void TokenizeConditionShouldHandleEscapedBackslash()
    {
        var conditionString = @"FullyQualifiedName=TestMethod\(""\\""\)";

        var tokens = Condition.TokenizeFilterConditionString(conditionString).ToArray();

        Assert.HasCount(3, tokens);
        Assert.AreEqual("FullyQualifiedName", tokens[0]);
        Assert.AreEqual("=", tokens[1]);
        Assert.AreEqual(@"TestMethod\(""\\""\)", tokens[2]);
    }

    [TestMethod]
    public void TokenizeConditionShouldHandleEscapedTilde()
    {
        var conditionString = @"FullyQualifiedName~TestMethod\(""\~""\)";

        var tokens = Condition.TokenizeFilterConditionString(conditionString).ToArray();

        Assert.HasCount(3, tokens);
        Assert.AreEqual("FullyQualifiedName", tokens[0]);
        Assert.AreEqual("~", tokens[1]);
        Assert.AreEqual(@"TestMethod\(""\~""\)", tokens[2]);
    }

    [TestMethod]
    public void TokenizeConditionShouldHandleEscapedNotTilde()
    {
        var conditionString = @"FullyQualifiedName!~TestMethod\(""\!\~""\)";

        var tokens = Condition.TokenizeFilterConditionString(conditionString).ToArray();

        Assert.HasCount(3, tokens);
        Assert.AreEqual("FullyQualifiedName", tokens[0]);
        Assert.AreEqual("!~", tokens[1]);
        Assert.AreEqual(@"TestMethod\(""\!\~""\)", tokens[2]);
    }

    [TestMethod]
    public void TokenizeConditionShouldHandleSingleUnescapedBang()
    {
        var conditionString = @"FullyQualifiedName!=TestMethod\(""!""\)";

        var tokens = Condition.TokenizeFilterConditionString(conditionString).ToArray();

        Assert.HasCount(5, tokens);
        Assert.AreEqual("FullyQualifiedName", tokens[0]);
        Assert.AreEqual("!=", tokens[1]);
        Assert.AreEqual(@"TestMethod\(""", tokens[2]);
        Assert.AreEqual("!", tokens[3]);
        Assert.AreEqual(@"""\)", tokens[4]);
    }

    [TestMethod]
    public void TokenizeConditionShouldHandleSingleBangAtEnd()
    {
        var conditionString = "FullyQualifiedName!";

        var tokens = Condition.TokenizeFilterConditionString(conditionString).ToArray();

        Assert.HasCount(2, tokens);
        Assert.AreEqual("FullyQualifiedName", tokens[0]);
        Assert.AreEqual("!", tokens[1]);
    }

    #region Empty value filter tests (uncategorized tests support)

    [TestMethod]
    public void ParseEmptyValueWithNotEqualsShouldCreateConditionWithEmptyValue()
    {
        Condition condition = Condition.Parse("TestCategory!=");

        Assert.AreEqual("TestCategory", condition.Name);
        Assert.AreEqual(Operation.NotEqual, condition.Operation);
        Assert.AreEqual(string.Empty, condition.Value);
    }

    [TestMethod]
    public void ParseEmptyValueWithContainsShouldCreateConditionWithEmptyValue()
    {
        Condition condition = Condition.Parse("TestCategory~");

        Assert.AreEqual("TestCategory", condition.Name);
        Assert.AreEqual(Operation.Contains, condition.Operation);
        Assert.AreEqual(string.Empty, condition.Value);
    }

    [TestMethod]
    public void ParseEmptyValueWithNotContainsShouldCreateConditionWithEmptyValue()
    {
        Condition condition = Condition.Parse("TestCategory!~");

        Assert.AreEqual("TestCategory", condition.Name);
        Assert.AreEqual(Operation.NotContains, condition.Operation);
        Assert.AreEqual(string.Empty, condition.Value);
    }

    [TestMethod]
    public void ParseWhitespaceValueAfterEqualsShouldCreateConditionWithEmptyValue()
    {
        Condition condition = Condition.Parse("TestCategory= ");

        Assert.AreEqual("TestCategory", condition.Name);
        Assert.AreEqual(Operation.Equal, condition.Operation);
        Assert.AreEqual(string.Empty, condition.Value);
    }

    [TestMethod]
    public void EvaluateEmptyStringEqualWithNullPropertyShouldReturnTrue()
    {
        var condition = new Condition("TestCategory", Operation.Equal, string.Empty);
        bool result = condition.Evaluate(propertyName => null);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void EvaluateEmptyStringEqualWithEmptyArrayShouldReturnTrue()
    {
        var condition = new Condition("TestCategory", Operation.Equal, string.Empty);
        bool result = condition.Evaluate(propertyName => Array.Empty<string>());

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void EvaluateEmptyStringEqualWithNonEmptyArrayShouldReturnFalse()
    {
        var condition = new Condition("TestCategory", Operation.Equal, string.Empty);
        bool result = condition.Evaluate(propertyName => new[] { "CategoryA" });

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void EvaluateEmptyStringNotEqualWithNullPropertyShouldReturnFalse()
    {
        var condition = new Condition("TestCategory", Operation.NotEqual, string.Empty);
        bool result = condition.Evaluate(propertyName => null);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void EvaluateEmptyStringNotEqualWithEmptyArrayShouldReturnFalse()
    {
        var condition = new Condition("TestCategory", Operation.NotEqual, string.Empty);
        bool result = condition.Evaluate(propertyName => Array.Empty<string>());

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void EvaluateEmptyStringNotEqualWithNonEmptyArrayShouldReturnTrue()
    {
        var condition = new Condition("TestCategory", Operation.NotEqual, string.Empty);
        bool result = condition.Evaluate(propertyName => new[] { "CategoryA" });

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void EvaluateEmptyStringContainsWithNullPropertyShouldReturnTrue()
    {
        var condition = new Condition("TestCategory", Operation.Contains, string.Empty);
        bool result = condition.Evaluate(propertyName => null);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void EvaluateEmptyStringContainsWithEmptyArrayShouldReturnTrue()
    {
        var condition = new Condition("TestCategory", Operation.Contains, string.Empty);
        bool result = condition.Evaluate(propertyName => Array.Empty<string>());

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void EvaluateEmptyStringContainsWithNonEmptyArrayShouldReturnFalse()
    {
        var condition = new Condition("TestCategory", Operation.Contains, string.Empty);
        bool result = condition.Evaluate(propertyName => new[] { "CategoryA" });

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void EvaluateEmptyStringNotContainsWithNullPropertyShouldReturnFalse()
    {
        var condition = new Condition("TestCategory", Operation.NotContains, string.Empty);
        bool result = condition.Evaluate(propertyName => null);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void EvaluateEmptyStringNotContainsWithEmptyArrayShouldReturnFalse()
    {
        var condition = new Condition("TestCategory", Operation.NotContains, string.Empty);
        bool result = condition.Evaluate(propertyName => Array.Empty<string>());

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void EvaluateEmptyStringNotContainsWithNonEmptyArrayShouldReturnTrue()
    {
        var condition = new Condition("TestCategory", Operation.NotContains, string.Empty);
        bool result = condition.Evaluate(propertyName => new[] { "CategoryA" });

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void EvaluateNonEmptyStringEqualShouldStillWorkNormally()
    {
        var condition = new Condition("TestCategory", Operation.Equal, "CategoryA");
        Assert.IsTrue(condition.Evaluate(propertyName => new[] { "CategoryA" }));
        Assert.IsFalse(condition.Evaluate(propertyName => new[] { "CategoryB" }));
        Assert.IsFalse(condition.Evaluate(propertyName => null));
    }

    [TestMethod]
    public void EvaluateEmptyStringEqualWithEmptyStringPropertyShouldReturnTrue()
    {
        // When property provider returns a single empty string (not an array),
        // GetPropertyValue wraps it into [""], which should match empty-value filter.
        // This must be consistent with FastFilter's behavior.
        var condition = new Condition("TestCategory", Operation.Equal, string.Empty);
        bool result = condition.Evaluate(propertyName => string.Empty);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void EvaluateEmptyStringEqualWithArrayContainingEmptyStringShouldReturnTrue()
    {
        // An array containing only an empty string (e.g. from [TestCategory("")])
        // should be treated as uncategorized, consistent with FastFilter.
        var condition = new Condition("TestCategory", Operation.Equal, string.Empty);
        bool result = condition.Evaluate(propertyName => new[] { string.Empty });

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void EvaluateEmptyStringNotEqualWithEmptyStringPropertyShouldReturnFalse()
    {
        var condition = new Condition("TestCategory", Operation.NotEqual, string.Empty);
        bool result = condition.Evaluate(propertyName => string.Empty);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void EvaluateEmptyStringContainsWithArrayContainingEmptyStringShouldReturnTrue()
    {
        var condition = new Condition("TestCategory", Operation.Contains, string.Empty);
        bool result = condition.Evaluate(propertyName => new[] { string.Empty });

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void EvaluateEmptyStringNotContainsWithArrayContainingEmptyStringShouldReturnFalse()
    {
        var condition = new Condition("TestCategory", Operation.NotContains, string.Empty);
        bool result = condition.Evaluate(propertyName => new[] { string.Empty });

        Assert.IsFalse(result);
    }

    #endregion
}
