// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Adapter
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using CrossPlatEngineAdapter = Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Adapter;
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
            var testCaseFilterExpression = new CrossPlatEngineAdapter.TestCaseFilterExpression(filterExpressionWrapper);

            testCaseFilterExpression.ValidForProperties(new List<string>() { "TestCategory" }, (s) => { return null; });

            TestCase dummyTestCase = new TestCase();
            bool result = testCaseFilterExpression.MatchTestCase(dummyTestCase, (s) => { return "unused"; });

            Assert.IsTrue(result);
        }

    }
}
