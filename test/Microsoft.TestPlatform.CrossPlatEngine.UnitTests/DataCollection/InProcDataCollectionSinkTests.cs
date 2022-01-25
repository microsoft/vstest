// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.DataCollection
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class InProcDataCollectionSinkTests
    {
        private IDataCollectionSink dataCollectionSink;

        private DataCollectionContext dataCollectionContext;

        private TestCase testCase;

        [TestInitialize]
        public void InitializeTest()
        {
            dataCollectionSink = new InProcDataCollectionSink();
            testCase = new TestCase("DummyNS.DummyC.DummyM", new Uri("executor://mstest/v1"), "Dummy.dll");
            dataCollectionContext = new DataCollectionContext(testCase);
        }

        [TestMethod]
        public void SendDataShouldAddKeyValueToDictionaryInSink()
        {
            testCase.SetPropertyValue(TestCaseProperties.Id, Guid.NewGuid());
            dataCollectionSink.SendData(dataCollectionContext, "DummyKey", "DummyValue");

            var dict = ((InProcDataCollectionSink)dataCollectionSink).GetDataCollectionDataSetForTestCase(testCase.Id);

            Assert.AreEqual("DummyValue", dict["DummyKey"]);
        }

        [TestMethod]

        public void SendDataShouldThrowArgumentExceptionIfKeyIsNull()
        {
            testCase.SetPropertyValue(TestCaseProperties.Id, Guid.NewGuid());

            Assert.ThrowsException<ArgumentNullException>(
                () => dataCollectionSink.SendData(dataCollectionContext, null, "DummyValue"));
        }

        [TestMethod]
        public void SendDataShouldThrowArgumentExceptionIfValueIsNull()
        {
            testCase.SetPropertyValue(TestCaseProperties.Id, Guid.NewGuid());

            Assert.ThrowsException<ArgumentNullException>(
                () => dataCollectionSink.SendData(dataCollectionContext, "DummyKey", null));
        }

        //[TestMethod]
        // TODO : Currently this code hits when test case id is null for core projects. For that we don't have algorithm to generate the guid. It's not implemented exception now (Source Code : EqtHash.cs).
        public void SendDataShouldThrowArgumentExceptionIfTestCaseIdIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () => dataCollectionSink.SendData(dataCollectionContext, "DummyKey", "DummyValue"));
        }
    }
}
