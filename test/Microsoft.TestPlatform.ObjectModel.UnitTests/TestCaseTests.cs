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

        [TestMethod]
        public void TestCaseLocalExtensionDataIsPubliclySettableGettableProperty()
        {
            var dummyData = new string[] { "foo"};
            this.testCase.LocalExtensionData = dummyData;
            Assert.AreEqual("foo", this.testCase.LocalExtensionData);
        }

        #region GetSetPropertyValue Tests

        [TestMethod]
        public void TestCaseGetPropertyValueForCodeFilePathShouldReturnCorrectValue()
        {
            var testCodeFilePath = "C:\\temp\foo.cs";
            this.testCase.CodeFilePath = testCodeFilePath;

            Assert.AreEqual(testCodeFilePath, this.testCase.GetPropertyValue(TestCaseProperties.CodeFilePath));
        }

        [TestMethod]
        public void TestCaseGetPropertyValueForDisplayNameShouldReturnCorrectValue()
        {
            var testDisplayName = "testCaseDisplayName";
            this.testCase.DisplayName = testDisplayName;

            Assert.AreEqual(testDisplayName, this.testCase.GetPropertyValue(TestCaseProperties.DisplayName));
        }
        
        [TestMethod]
        public void TestCaseGetPropertyValueForExecutorUriShouldReturnCorrectValue()
        {
            var testExecutorUri = new Uri("http://foo");
            this.testCase.ExecutorUri = testExecutorUri;

            Assert.AreEqual(testExecutorUri, this.testCase.GetPropertyValue(TestCaseProperties.ExecutorUri));
        }
        
        [TestMethod]
        public void TestCaseGetPropertyValueForFullyQualifiedNameShouldReturnCorrectValue()
        {
            var testFullyQualifiedName = "fullyQualifiedName.Test1";
            this.testCase.FullyQualifiedName = testFullyQualifiedName;

            Assert.AreEqual(testFullyQualifiedName, this.testCase.GetPropertyValue(TestCaseProperties.FullyQualifiedName));
        }
        
        [TestMethod]
        public void TestCaseGetPropertyValueForIdShouldReturnCorrectValue()
        {
            var testId = new Guid("{7845816C-9CDB-37DA-9ADF-1B1C1FE01B7D}");
            this.testCase.Id = testId;

            Assert.AreEqual(testId, this.testCase.GetPropertyValue(TestCaseProperties.Id));
        }

        [TestMethod]
        public void TestCaseGetPropertyValueForLineNumberShouldReturnCorrectValue()
        {
            var testLineNumber = 34;
            this.testCase.LineNumber = testLineNumber;

            Assert.AreEqual(testLineNumber, this.testCase.GetPropertyValue(TestCaseProperties.LineNumber));
        }

        [TestMethod]
        public void TestCaseGetPropertyValueForSourceShouldReturnCorrectValue()
        {
            var testSource = "C://temp/foobar.dll";
            this.testCase.Source = testSource;

            Assert.AreEqual(testSource, this.testCase.GetPropertyValue(TestCaseProperties.Source));
        }

        [TestMethod]
        public void TestCaseSetPropertyValueForCodeFilePathShouldSetValue()
        {
            var testCodeFilePath = "C:\\temp\foo.cs";
            this.testCase.SetPropertyValue(TestCaseProperties.CodeFilePath, testCodeFilePath);

            Assert.AreEqual(testCodeFilePath, this.testCase.CodeFilePath);
        }

        [TestMethod]
        public void TestCaseSetPropertyValueForDisplayNameShouldSetValue()
        {
            var testDisplayName = "testCaseDisplayName";
            this.testCase.SetPropertyValue(TestCaseProperties.DisplayName, testDisplayName);

            Assert.AreEqual(testDisplayName, this.testCase.DisplayName);
        }

        [TestMethod]
        public void TestCaseSetPropertyValueForExecutorUriShouldSetValue()
        {
            var testExecutorUri = new Uri("http://foo");
            this.testCase.SetPropertyValue(TestCaseProperties.ExecutorUri, testExecutorUri);

            Assert.AreEqual(testExecutorUri, this.testCase.ExecutorUri);
        }

        [TestMethod]
        public void TestCaseSetPropertyValueForFullyQualifiedNameShouldSetValue()
        {
            var testFullyQualifiedName = "fullyQualifiedName.Test1";
            this.testCase.SetPropertyValue(TestCaseProperties.FullyQualifiedName, testFullyQualifiedName);

            Assert.AreEqual(testFullyQualifiedName, this.testCase.FullyQualifiedName);
        }

        [TestMethod]
        public void TestCaseSetPropertyValueForIdShouldSetValue()
        {
            var testId = new Guid("{7845816C-9CDB-37DA-9ADF-1B1C1FE01B7D}");
            this.testCase.SetPropertyValue(TestCaseProperties.Id, testId);

            Assert.AreEqual(testId, this.testCase.Id);
        }

        [TestMethod]
        public void TestCaseSetPropertyValueForLineNumberShouldSetValue()
        {
            var testLineNumber = 34;
            this.testCase.SetPropertyValue(TestCaseProperties.LineNumber, testLineNumber);

            Assert.AreEqual(testLineNumber, this.testCase.LineNumber);
        }

        [TestMethod]
        public void TestCaseSetPropertyValueForSourceShouldSetValue()
        {
            var testSource = "C://temp/foobar.dll";
            this.testCase.SetPropertyValue(TestCaseProperties.Source, testSource);

            Assert.AreEqual(testSource, this.testCase.Source);
        }

        #endregion
    }
}
