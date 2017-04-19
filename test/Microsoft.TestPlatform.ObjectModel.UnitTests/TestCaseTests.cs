// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.ObjectModel.UnitTests
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TestCaseTests
    {
        private TestCase testCase;

        [TestInitialize]
        public void TestInit()
        {
            testCase = new TestCase("sampleTestClass.sampleTestCase", new Uri("executor://sampleTestExecutor"), "sampleTest.dll");
        }

        [TestMethod]
        public void TestCaseIdIfNotSetExplicitlyShouldReturnGuidBasedOnSourceAndName()
        {
            Assert.AreEqual("28e7a7ed-8fb9-05b7-5e90-4a8c52f32b5b", testCase.Id.ToString());
        }

        [TestMethod]
        public void TestCaseIdIfNotSetExplicitlyShouldReturnGuidBasedOnSourceAndNameIfNameIsChanged()
        {
            testCase.FullyQualifiedName = "sampleTestClass1.sampleTestCase1";

            Assert.AreEqual("6f86dd1c-7130-a1ae-8e7f-02e7de898a43", testCase.Id.ToString());
        }

        [TestMethod]
        public void TestCaseIdIfNotSetExplicitlyShouldReturnGuidBasedOnSourceAndNameIfSourceIsChanged()
        {
            testCase.Source = "sampleTest1.dll";

            Assert.AreEqual("22843fee-70ea-4cf4-37cd-5061b4c47a8a", testCase.Id.ToString());
        }

        [TestMethod]
        public void TestCaseIdShouldReturnIdSetExplicitlyEvenIfNameOrSourceInfoChanges()
        {
            var testGuid = new Guid("{8167845C-9CDB-476F-9F2B-1B1C1FE01B7D}");
            testCase.Id = testGuid;

            testCase.FullyQualifiedName = "sampleTestClass1.sampleTestCase1";
            testCase.Source = "sampleTest1.dll";

            Assert.AreEqual(testGuid, testCase.Id);
        }
    }
}
