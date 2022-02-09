// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.DataCollection;

using System;

using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class InProcDataCollectionSinkTests
{
    private IDataCollectionSink _dataCollectionSink;

    private DataCollectionContext _dataCollectionContext;

    private TestCase _testCase;

    [TestInitialize]
    public void InitializeTest()
    {
        _dataCollectionSink = new InProcDataCollectionSink();
        _testCase = new TestCase("DummyNS.DummyC.DummyM", new Uri("executor://mstest/v1"), "Dummy.dll");
        _dataCollectionContext = new DataCollectionContext(_testCase);
    }

    [TestMethod]
    public void SendDataShouldAddKeyValueToDictionaryInSink()
    {
        _testCase.SetPropertyValue(TestCaseProperties.Id, Guid.NewGuid());
        _dataCollectionSink.SendData(_dataCollectionContext, "DummyKey", "DummyValue");

        var dict = ((InProcDataCollectionSink)_dataCollectionSink).GetDataCollectionDataSetForTestCase(_testCase.Id);

        Assert.AreEqual("DummyValue", dict["DummyKey"]);
    }

    [TestMethod]

    public void SendDataShouldThrowArgumentExceptionIfKeyIsNull()
    {
        _testCase.SetPropertyValue(TestCaseProperties.Id, Guid.NewGuid());

        Assert.ThrowsException<ArgumentNullException>(
            () => _dataCollectionSink.SendData(_dataCollectionContext, null, "DummyValue"));
    }

    [TestMethod]
    public void SendDataShouldThrowArgumentExceptionIfValueIsNull()
    {
        _testCase.SetPropertyValue(TestCaseProperties.Id, Guid.NewGuid());

        Assert.ThrowsException<ArgumentNullException>(
            () => _dataCollectionSink.SendData(_dataCollectionContext, "DummyKey", null));
    }

    //[TestMethod]
    // TODO : Currently this code hits when test case id is null for core projects. For that we don't have algorithm to generate the guid. It's not implemented exception now (Source Code : EqtHash.cs).
    public void SendDataShouldThrowArgumentExceptionIfTestCaseIdIsNull()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => _dataCollectionSink.SendData(_dataCollectionContext, "DummyKey", "DummyValue"));
    }
}
