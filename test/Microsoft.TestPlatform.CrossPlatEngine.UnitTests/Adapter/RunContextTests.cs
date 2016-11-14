// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Adapter
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.Common.Filtering;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Adapter;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class RunContextTests
    {
        private RunContext runContext;

        [TestInitialize]
        public void TestInit()
        {
            this.runContext = new RunContext();
        }

        [TestMethod]
        public void GetTestCaseFilterShouldReturnNullIfFilterExpressionIsNull()
        {
            this.runContext.FilterExpressionWrapper = null;

            Assert.IsNull(this.runContext.GetTestCaseFilter(null, (s) => { return null; }));
        }

        [TestMethod]
        public void GetTestCaseFilterShouldThrowOnfilterExpressionParsingError()
        {
            this.runContext.FilterExpressionWrapper = new FilterExpressionWrapper("Infinity");

            var isExceptionThrown = false;

            try
            {
                this.runContext.GetTestCaseFilter(null, (s) => { return null; });
            }
            catch (TestPlatformFormatException ex)
            {
                isExceptionThrown = true;
                StringAssert.Contains(ex.Message, "Incorrect format for TestCaseFilter Error: Invalid Condition 'Infinity'. Specify the correct format and try again. Note that the incorrect format can lead to no test getting executed.");
            }

            Assert.IsTrue(isExceptionThrown);
        }

        [TestMethod]
        public void GetTestCaseFilterShouldThrowOnInvalidProperties()
        {
            this.runContext.FilterExpressionWrapper = new FilterExpressionWrapper("highlyunlikelyproperty=unused");
            
            var isExceptionThrown = false;

            try
            {
                this.runContext.GetTestCaseFilter(new List<string> { "TestCategory" }, (s) => { return null; });
            }
            catch (TestPlatformFormatException ex)
            {
                isExceptionThrown = true;
                StringAssert.Contains(ex.Message, "No tests matched the filter because it contains one or more properties that are not valid (highlyunlikelyproperty). Specify filter expression containing valid properties (TestCategory) and try again.");
            }

            Assert.IsTrue(isExceptionThrown);
        }

        [TestMethod]
        public void GetTestCaseFilterShouldReturnTestCaseFilter()
        {
            this.runContext.FilterExpressionWrapper = new FilterExpressionWrapper("TestCategory=Important");
            var filter = this.runContext.GetTestCaseFilter(new List<string> { "TestCategory" }, (s) => { return null; });

            Assert.IsNotNull(filter);
            Assert.AreEqual("TestCategory=Important", filter.TestCaseFilterValue);
        }
    }
}
