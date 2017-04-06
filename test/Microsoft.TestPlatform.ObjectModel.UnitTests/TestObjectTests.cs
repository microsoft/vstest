// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.ObjectModel.UnitTests
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Collections.Generic;
    using System.Linq;

    [TestClass]
    public class TestObjectTests
    {
        private static TestCase testCase = new TestCase(
                                               "sampleTestClass.sampleTestCase",
                                               new Uri("executor://sampleTestExecutor"),
                                               "sampleTest.dll")
        {
            CodeFilePath = "/user/src/testFile.cs",
            DisplayName = "sampleTestCase",
            Id = new Guid("be78d6fc-61b0-4882-9d07-40d796fd96ce"),
            Traits = { new Trait("Priority", "0"), new Trait("Category", "unit") }
        };

        [TestMethod]
        public void TestCaseIdShouldReturnGuidWhenTestPropertiesIdIsSet()
        {
            Guid expected = new Guid("{8167845C-9CDB-476F-9F2B-1B1C1FE01B7D}");
            testCase.Id = expected;
            var actual = testCase.Id;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GetPropertiesShouldReturnListOfPropertiesInStore()
        {
            TestProperty tp = TestProperty.Register("dummyId", "dummyLabel", typeof(int), typeof(TestObjectTests));
            var kvp = new KeyValuePair<TestProperty, object>(tp, 123);
            testCase.SetPropertyValue(kvp.Key, kvp.Value);

            var properties = testCase.GetProperties().ToList();
            Assert.IsTrue(properties.Contains(kvp));
        }
    }
}
