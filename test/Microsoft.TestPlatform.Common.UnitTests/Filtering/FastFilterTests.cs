// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Common.UnitTests.Filtering
{
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.VisualStudio.TestPlatform.Common.Filtering;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

    [TestClass]
    public class FastFilterTests
    {
        [TestMethod]
        public void MultiplePropertyNamesShouldNotCreateFastFilter()
        {
            var filterExpressionWrapper = new FilterExpressionWrapper("FullyQualifiedName=Test1|Category=Core");
            var fastFilter = filterExpressionWrapper.fastFilter;

            Assert.IsTrue(fastFilter == null);
        }

        [TestMethod]
        public void MultipleOperatorKindsShouldNotCreateFastFilter()
        {
            var filterExpressionWrapper = new FilterExpressionWrapper("Name=Test1&(Name=Test2|NameTest3)");
            var fastFilter = filterExpressionWrapper.fastFilter;

            Assert.IsTrue(fastFilter == null);
        }

        [TestMethod]
        public void MultipleOperationKindsShouldNotCreateFastFilter()
        {
            var filterExpressionWrapper = new FilterExpressionWrapper("Name!=TestClass1&Category=Nightly");
            var fastFilter = filterExpressionWrapper.fastFilter;

            Assert.IsTrue(fastFilter == null);
        }

        [TestMethod]
        public void ContainsOperationShouldNotCreateFastFilter()
        {
            var filterExpressionWrapper = new FilterExpressionWrapper("Name~TestClass1");
            var fastFilter = filterExpressionWrapper.fastFilter;

            Assert.IsTrue(fastFilter == null);
        }

        [TestMethod]
        public void AndOperatorAndEqualsOperationShouldNotCreateFastFilter()
        {
            var filterExpressionWrapper = new FilterExpressionWrapper("Name=Test1&Name=Test2");
            var fastFilter = filterExpressionWrapper.fastFilter;

            Assert.IsTrue(fastFilter == null);
        }

        [TestMethod]
        public void OrOperatorAndNotEqualsOperationShouldNotCreateFastFilter()
        {
            var filterExpressionWrapper = new FilterExpressionWrapper("Name!=Test1|Name!=Test2");
            var fastFilter = filterExpressionWrapper.fastFilter;

            Assert.IsTrue(fastFilter == null);
        }

        [TestMethod]
        public void FastFilterWithSingleEqualsClause()
        {
            var filterExpressionWrapper = new FilterExpressionWrapper("FullyQualifiedName=Test1");
            var fastFilter = filterExpressionWrapper.fastFilter;

            var expectedFilterValues = new HashSet<string>() { "test1" };

            Assert.IsTrue(fastFilter != null);
            Assert.AreEqual("FullyQualifiedName", fastFilter.FilterPropertyName);
            Assert.IsFalse(fastFilter.IsFilteredOutWhenMatched);
            Assert.IsTrue(expectedFilterValues.SetEquals(fastFilter.FilterPropertyValues));

            filterExpressionWrapper.ValidForProperties(new List<string>() { "FullyQualifiedName" }, null);            

            Assert.IsTrue(fastFilter.Evaluate((s) => "Test1"));
            Assert.IsFalse(fastFilter.Evaluate((s) => "Test2"));
        }

        [TestMethod]
        public void FastFilterWithMultipleEqualsClause()
        {
            var filterExpressionWrapper = new FilterExpressionWrapper("FullyQualifiedName=Test1|FullyQualifiedName=Test2|FullyQualifiedName=Test3");
            var fastFilter = filterExpressionWrapper.fastFilter;

            var expectedFilterValues = new HashSet<string>() { "test1", "test2", "test3" };

            Assert.IsTrue(fastFilter != null);
            Assert.AreEqual("FullyQualifiedName", fastFilter.FilterPropertyName);
            Assert.IsFalse(fastFilter.IsFilteredOutWhenMatched);
            Assert.IsTrue(expectedFilterValues.SetEquals(fastFilter.FilterPropertyValues));

            filterExpressionWrapper.ValidForProperties(new List<string>() { "FullyQualifiedName" }, null);            

            Assert.IsTrue(fastFilter.Evaluate((s) => "Test1"));
            Assert.IsTrue(fastFilter.Evaluate((s) => "Test2"));
            Assert.IsTrue(fastFilter.Evaluate((s) => "Test3"));
            Assert.IsFalse(fastFilter.Evaluate((s) => "Test4"));
        }

        [TestMethod]
        public void FastFilterWithMultipleEqualsClauseAndParentheses()
        {
            var filterExpressionWrapper = new FilterExpressionWrapper("FullyQualifiedName=Test1|(FullyQualifiedName=Test2|FullyQualifiedName=Test3)");
            var fastFilter = filterExpressionWrapper.fastFilter;

            var expectedFilterValues = new HashSet<string>() { "test1", "test2", "test3" };

            Assert.IsTrue(fastFilter != null);
            Assert.AreEqual("FullyQualifiedName", fastFilter.FilterPropertyName);
            Assert.IsFalse(fastFilter.IsFilteredOutWhenMatched);
            Assert.IsTrue(expectedFilterValues.SetEquals(fastFilter.FilterPropertyValues));

            filterExpressionWrapper.ValidForProperties(new List<string>() { "FullyQualifiedName" }, null);

            Assert.IsTrue(fastFilter.Evaluate((s) => "Test1"));
            Assert.IsTrue(fastFilter.Evaluate((s) => "Test2"));
            Assert.IsTrue(fastFilter.Evaluate((s) => "Test3"));
            Assert.IsFalse(fastFilter.Evaluate((s) => "Test4"));
        }

        [TestMethod]
        public void FastFilterWithMultipleEqualsClauseAndRegex()
        {
            var filterExpressionWrapper = new FilterExpressionWrapper("FullyQualifiedName=Test1|FullyQualifiedName=Test2|FullyQualifiedName=Test3", new FilterOptions() { FilterRegEx = @"^[^\s\(]+" });
            var fastFilter = filterExpressionWrapper.fastFilter;

            var expectedFilterValues = new HashSet<string>() { "test1", "test2", "test3" };

            Assert.IsTrue(fastFilter != null);
            Assert.AreEqual("FullyQualifiedName", fastFilter.FilterPropertyName);
            Assert.IsFalse(fastFilter.IsFilteredOutWhenMatched);
            Assert.IsTrue(expectedFilterValues.SetEquals(fastFilter.FilterPropertyValues));

            filterExpressionWrapper.ValidForProperties(new List<string>() { "FullyQualifiedName" }, null);

            Assert.IsTrue(fastFilter.Evaluate((s) => "Test1"));
            Assert.IsTrue(fastFilter.Evaluate((s) => "Test2"));
            Assert.IsTrue(fastFilter.Evaluate((s) => "Test3"));
            Assert.IsTrue(fastFilter.Evaluate((s) => "Test1 (123)"));
            Assert.IsTrue(fastFilter.Evaluate((s) => "Test2(123)"));
            Assert.IsTrue(fastFilter.Evaluate((s) => "Test3  (123)"));
            Assert.IsFalse(fastFilter.Evaluate((s) => "Test4"));
            Assert.IsFalse(fastFilter.Evaluate((s) => "Test4 ()"));
        }

        [TestMethod]
        public void FastFilterWithMultipleEqualsClauseForMultiplePropertyValues()
        {
            var filterExpressionWrapper = new FilterExpressionWrapper("Category=UnitTest|Category=PerfTest", null);
            var fastFilter = filterExpressionWrapper.fastFilter;

            var expectedFilterValues = new HashSet<string>() { "unittest", "perftest"};

            Assert.IsTrue(fastFilter != null);
            Assert.AreEqual("Category", fastFilter.FilterPropertyName);
            Assert.IsFalse(fastFilter.IsFilteredOutWhenMatched);
            Assert.IsTrue(expectedFilterValues.SetEquals(fastFilter.FilterPropertyValues));

            filterExpressionWrapper.ValidForProperties(new List<string>() { "Category" }, null);

            Assert.IsTrue(fastFilter.Evaluate((s) => new[] { "UnitTest" }));
            Assert.IsTrue(fastFilter.Evaluate((s) => new[] { "PerfTest" }));
            Assert.IsTrue(fastFilter.Evaluate((s) => new[] { "UnitTest", "PerfTest" }));
            Assert.IsTrue(fastFilter.Evaluate((s) => new[] { "UnitTest", "IntegrationTest" }));
            Assert.IsFalse(fastFilter.Evaluate((s) => new[] { "IntegrationTest" }));
            Assert.IsFalse(fastFilter.Evaluate((s) => null));
        }

        [TestMethod]
        public void FastFilterWithMultipleEqualsClauseAndRegexReplacement()
        {
            var filterExpressionWrapper = new FilterExpressionWrapper("FullyQualifiedName=TestClass.Test1|FullyQualifiedName=TestClass.Test2|FullyQualifiedName=TestClass.Test3", new FilterOptions() { FilterRegEx = @"\s*\([^\)]*\)", FilterRegExReplacement = "" });
            var fastFilter = filterExpressionWrapper.fastFilter;

            var expectedFilterValues = new HashSet<string>() { "testclass.test1", "testclass.test2", "testclass.test3" };

            Assert.IsTrue(fastFilter != null);
            Assert.AreEqual("FullyQualifiedName", fastFilter.FilterPropertyName);
            Assert.IsFalse(fastFilter.IsFilteredOutWhenMatched);
            Assert.IsTrue(expectedFilterValues.SetEquals(fastFilter.FilterPropertyValues));

            filterExpressionWrapper.ValidForProperties(new List<string>() { "FullyQualifiedName" }, null);

            Assert.IsTrue(fastFilter.Evaluate((s) => "TestClass(1).Test1"));
            Assert.IsTrue(fastFilter.Evaluate((s) => "TestClass().Test1()"));
            Assert.IsTrue(fastFilter.Evaluate((s) => "TestClass(1, 2).Test2"));
            Assert.IsTrue(fastFilter.Evaluate((s) => "TestClass.Test3 (abcd1234)"));
            Assert.IsTrue(fastFilter.Evaluate((s) => "TestClass(1).Test1(123)"));
            Assert.IsTrue(fastFilter.Evaluate((s) => "TestClass(1, 2).Test2(x:1, y:2, z:3)"));
            Assert.IsTrue(fastFilter.Evaluate((s) => "TestClass(1, 2,3).Test3(1)  (123)"));
            Assert.IsFalse(fastFilter.Evaluate((s) => "TestClass1.Test1"));
            Assert.IsFalse(fastFilter.Evaluate((s) => "TestClass1(1).Test1"));
            Assert.IsFalse(fastFilter.Evaluate((s) => "TestClass((1, 2, 3)).Test1"));
        }

        [TestMethod]
        public void FastFilterWithSingleNotEqualsClause()
        {
            var filterExpressionWrapper = new FilterExpressionWrapper("FullyQualifiedName!=Test1");
            var fastFilter = filterExpressionWrapper.fastFilter;

            var expectedFilterValues = new HashSet<string>() { "test1" };

            Assert.IsTrue(fastFilter != null);
            Assert.AreEqual("FullyQualifiedName", fastFilter.FilterPropertyName);
            Assert.IsTrue(fastFilter.IsFilteredOutWhenMatched);
            Assert.IsTrue(expectedFilterValues.SetEquals(fastFilter.FilterPropertyValues));

            filterExpressionWrapper.ValidForProperties(new List<string>() { "FullyQualifiedName" }, null);

            Assert.IsFalse(fastFilter.Evaluate((s) => "Test1"));
            Assert.IsTrue(fastFilter.Evaluate((s) => "Test2"));
        }

        [TestMethod]
        public void FastFilterWithMultipleNotEqualsClause()
        {
            var filterExpressionWrapper = new FilterExpressionWrapper("FullyQualifiedName!=Test1&FullyQualifiedName!=Test2&FullyQualifiedName!=Test3");
            var fastFilter = filterExpressionWrapper.fastFilter;

            var expectedFilterValues = new HashSet<string>() { "test1", "test2", "test3" };

            Assert.IsTrue(fastFilter != null);
            Assert.AreEqual("FullyQualifiedName", fastFilter.FilterPropertyName);
            Assert.IsTrue(fastFilter.IsFilteredOutWhenMatched);
            Assert.IsTrue(expectedFilterValues.SetEquals(fastFilter.FilterPropertyValues));

            filterExpressionWrapper.ValidForProperties(new List<string>() { "FullyQualifiedName" }, null);

            Assert.IsFalse(fastFilter.Evaluate((s) => "Test1"));
            Assert.IsFalse(fastFilter.Evaluate((s) => "Test2"));
            Assert.IsFalse(fastFilter.Evaluate((s) => "Test3"));
            Assert.IsTrue(fastFilter.Evaluate((s) => "Test4"));
        }

        [TestMethod]
        public void FastFilterWithMultipleNotEqualsClauseAndRegex()
        {
            var filterExpressionWrapper = new FilterExpressionWrapper("FullyQualifiedName!=Test1&FullyQualifiedName!=Test2&FullyQualifiedName!=Test3", new FilterOptions() { FilterRegEx = @"^[^\s\(]+" });
            var fastFilter = filterExpressionWrapper.fastFilter;

            var expectedFilterValues = new HashSet<string>() { "test1", "test2", "test3" };

            Assert.IsTrue(fastFilter != null);
            Assert.AreEqual("FullyQualifiedName", fastFilter.FilterPropertyName);
            Assert.IsTrue(fastFilter.IsFilteredOutWhenMatched);
            Assert.IsTrue(expectedFilterValues.SetEquals(fastFilter.FilterPropertyValues));

            filterExpressionWrapper.ValidForProperties(new List<string>() { "FullyQualifiedName" }, null);

            Assert.IsFalse(fastFilter.Evaluate((s) => "Test1"));
            Assert.IsFalse(fastFilter.Evaluate((s) => "Test2"));
            Assert.IsFalse(fastFilter.Evaluate((s) => "Test3"));
            Assert.IsFalse(fastFilter.Evaluate((s) => "Test1 (123)"));
            Assert.IsFalse(fastFilter.Evaluate((s) => "Test2(123)"));
            Assert.IsFalse(fastFilter.Evaluate((s) => "Test3  (123)"));
            Assert.IsTrue(fastFilter.Evaluate((s) => "Test4"));
            Assert.IsTrue(fastFilter.Evaluate((s) => "Test4 (123)"));
        }

        [TestMethod]
        public void FastFilterWithMultipleNotEqualsClauseForMultiplePropertyValues()
        {
            var filterExpressionWrapper = new FilterExpressionWrapper("Category!=UnitTest&Category!=PerfTest", null);
            var fastFilter = filterExpressionWrapper.fastFilter;

            var expectedFilterValues = new HashSet<string>() { "unittest", "perftest" };

            Assert.IsTrue(fastFilter != null);
            Assert.AreEqual("Category", fastFilter.FilterPropertyName);
            Assert.IsTrue(fastFilter.IsFilteredOutWhenMatched);
            Assert.IsTrue(expectedFilterValues.SetEquals(fastFilter.FilterPropertyValues));

            filterExpressionWrapper.ValidForProperties(new List<string>() { "Category" }, null);

            Assert.IsFalse(fastFilter.Evaluate((s) => new[] { "UnitTest" }));
            Assert.IsFalse(fastFilter.Evaluate((s) => new[] { "PerfTest" }));
            Assert.IsFalse(fastFilter.Evaluate((s) => new[] { "UnitTest", "PerfTest" }));
            Assert.IsFalse(fastFilter.Evaluate((s) => new[] { "UnitTest", "IntegrationTest" }));
            Assert.IsTrue(fastFilter.Evaluate((s) => new[] { "IntegrationTest" }));
            Assert.IsTrue(fastFilter.Evaluate((s) => null));
        }

        [TestMethod]
        public void FastFilterWithWithRegexParseErrorShouldNotCreateFastFilter()
        {
            var filterExpressionWrapper = new FilterExpressionWrapper("FullyQualifiedName=Test", new FilterOptions() { FilterRegEx = @"^[^\s\(]+\1" });

            Assert.AreEqual(null, filterExpressionWrapper.fastFilter);
            Assert.IsFalse(string.IsNullOrEmpty(filterExpressionWrapper.ParseError));
        }
    }
}
