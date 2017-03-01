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
        public void ParseShouldThrownFormatException_OnNullConditionString()
        {
            string conditionString = null;
            Assert.ThrowsException<FormatException>(() => Condition.Parse(conditionString));
        }

        [TestMethod]
        public void ParseShouldThrownFormatException_OnEmptyConditionString()
        {
            var conditionString = "";
            Assert.ThrowsException<FormatException>(() => Condition.Parse(conditionString));
        }

        [TestMethod]
        public void ParseShouldThrownFormatException_OnIncompleteConditionString()
        {
            var conditionString = "PropertyName=";
            Assert.ThrowsException<FormatException>( ()=> Condition.Parse(conditionString));
        }

        [TestMethod]
        public void ParseShouldCreateDefaultCondition_WhenOnlyPropertyValuePassed()
        {
            var conditionString = "ABC";
            Condition condition = Condition.Parse(conditionString);
            Assert.AreEqual(Condition.DefaultPropertyName, condition.Name);
            Assert.AreEqual(Operation.Contains, condition.Operation);
            Assert.AreEqual(conditionString, condition.Value);
        }

        [TestMethod]
        public void ParseShouldCreateProperCondition_OnValidConditionString()
        {
            var conditionString = "PropertyName=PropertyValue";
            Condition condition = Condition.Parse(conditionString);
            Assert.AreEqual("PropertyName", condition.Name);
            Assert.AreEqual(Operation.Equal, condition.Operation);
            Assert.AreEqual("PropertyValue", condition.Value);
        }
    }
}
