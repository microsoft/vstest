// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Adapter
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.Common.Filtering;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Adapter;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using MSTest.TestFramework.AssertExtensions;

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

        /// <summary>
        /// If only property value passed, consider property key and operation defaults.
        /// </summary>
        [TestMethod]
        public void GetTestCaseFilterShouldNotThrowIfPropertyValueOnlyPassed()
        {
            this.runContext.FilterExpressionWrapper = new FilterExpressionWrapper("Infinity");

            var filter = this.runContext.GetTestCaseFilter(new List<string>{ "FullyQualifiedName" }, (s) => { return null; });

            Assert.IsNotNull(filter);
        }

        [TestMethod]
        public void GetTestCaseFilterShouldNotThrowOnInvalidProperties()
        {
            this.runContext.FilterExpressionWrapper = new FilterExpressionWrapper("highlyunlikelyproperty=unused");

            var filter = this.runContext.GetTestCaseFilter(new List<string> { "TestCategory" }, (s) => { return null; });

            Assert.IsNotNull(filter);
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
