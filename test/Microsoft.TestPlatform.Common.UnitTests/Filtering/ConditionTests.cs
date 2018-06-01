// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Common.UnitTests.Filtering
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.Common.Filtering;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ConditionTests
    {
        [TestMethod]
        public void ParseShouldThrownFormatExceptionOnNullConditionString()
        {
            string conditionString = null;
            Assert.ThrowsException<FormatException>(() => Condition.Parse(conditionString));
        }

        [TestMethod]
        public void ParseShouldThrownFormatExceptionOnEmptyConditionString()
        {
            var conditionString = "";
            Assert.ThrowsException<FormatException>(() => Condition.Parse(conditionString));
        }

        [TestMethod]
        public void ParseShouldThrownFormatExceptionOnIncompleteConditionString()
        {
            var conditionString = "PropertyName=";
            Assert.ThrowsException<FormatException>( () => Condition.Parse(conditionString));
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
        public void ParseStringWithSingleUnescapedBangShouldFail1()
        {

            var conditionString = @"FullyQualifiedName=Test1(""!"")";

            Assert.ThrowsException<FormatException>(() => Condition.Parse(conditionString));
        }

        [TestMethod]
        public void ParseStringWithSingleUnescapedBangShouldFail2()
        {

            var conditionString = @"FullyQualifiedName!Test1()";

            Assert.ThrowsException<FormatException>(() => Condition.Parse(conditionString));
        }
    }
}
