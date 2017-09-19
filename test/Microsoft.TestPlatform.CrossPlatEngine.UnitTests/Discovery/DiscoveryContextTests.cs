// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Discovery
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.Common.Filtering;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Discovery;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using MSTest.TestFramework.AssertExtensions;

    [TestClass]
    public class DiscoveryContextTests
    {
        private DiscoveryContext discoveryContext;

        [TestInitialize]
        public void TestInit()
        {
            this.discoveryContext = new DiscoveryContext();
        }

        /// <summary>
        /// GetTestCaseFilter should return null in case filter expression is null
        /// </summary>
        [TestMethod]
        public void GetTestCaseFilterShouldReturnNullIfFilterExpressionIsNull()
        {
            this.discoveryContext.FilterExpressionWrapper = null;

            Assert.IsNull(this.discoveryContext.GetTestCaseFilter(null, (s) => { return null; }));
        }

        /// <summary>
        /// If only property value passed, consider property key and operation defaults.
        /// </summary>
        [TestMethod]
        public void GetTestCaseFilterShouldNotThrowIfPropertyValueOnlyPassed()
        {
            this.discoveryContext.FilterExpressionWrapper = new FilterExpressionWrapper("Infinity");

            var filter = this.discoveryContext.GetTestCaseFilter(new List<string>{ "FullyQualifiedName" }, (s) => { return null; });

            Assert.IsNotNull(filter);
        }

        /// <summary>
        /// Exception should not be thrown in case invalid properties passed in filter expression.
        /// </summary>
        [TestMethod]
        public void GetTestCaseFilterShouldNotThrowOnInvalidProperties()
        {
            this.discoveryContext.FilterExpressionWrapper = new FilterExpressionWrapper("highlyunlikelyproperty=unused");

            var filter = this.discoveryContext.GetTestCaseFilter(new List<string> { "TestCategory" }, (s) => { return null; });

            Assert.IsNotNull(filter);
        }

        /// <summary>
        /// GetTestCaseFilter should return correct filter as present in filter expression.
        /// </summary>
        [TestMethod]
        public void GetTestCaseFilterShouldReturnTestCaseFilter()
        {
            this.discoveryContext.FilterExpressionWrapper = new FilterExpressionWrapper("TestCategory=Important");

            var filter = this.discoveryContext.GetTestCaseFilter(new List<string> { "TestCategory" }, (s) => { return null; });

            Assert.IsNotNull(filter);
            Assert.AreEqual("TestCategory=Important", filter.TestCaseFilterValue);
        }
    }
}
