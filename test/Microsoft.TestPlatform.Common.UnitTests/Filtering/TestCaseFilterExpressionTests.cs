// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;

namespace Microsoft.TestPlatform.Common.UnitTests.Filtering
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.VisualStudio.TestPlatform.Common.Filtering;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    [TestClass]
    public class TestCaseFilterExpressionTests
    {
        [TestMethod]
        public void ValidForPropertiesShouldNotSetvalidForMatchVariableTofalseIfFilterIsInvalid()
        {
            var filterExpressionWrapper = new FilterExpressionWrapper("highlyunlikelyproperty=unused");
            var testCaseFilterExpression = new TestCaseFilterExpression(filterExpressionWrapper);

            Assert.AreEqual("highlyunlikelyproperty", testCaseFilterExpression.ValidForProperties(new List<string>() { "TestCategory" }, (s) => { return null; }).Single());

            TestCase dummyTestCase = new TestCase();
            bool result = testCaseFilterExpression.MatchTestCase(dummyTestCase, (s) => { return "unused"; });

            Assert.IsTrue(result);
        }

    }
}
