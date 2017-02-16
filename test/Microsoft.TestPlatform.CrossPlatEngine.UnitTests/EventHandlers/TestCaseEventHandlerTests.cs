// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.EventHandlers
{
    using System;
    using System.Collections.ObjectModel;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.EventHandlers;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.InProcDataCollector;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollector.InProcDataCollector;
    using System.Collections.Generic;

    using Constants = Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Constants;
    using TestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;

    [TestClass]
    public class TestCaseEventHandlerTests
    {

        private Mock<ITestCaseEventsHandler> mockTestCaseEvents;

        private Mock<IDataCollectionTestCaseEventManager> mockDataCollectionTestCaseEventManager;

        private TestCaseEventsHandler testCasesEventsHandler;

        [TestInitialize]
        public void InitializeTests()
        {
            this.mockDataCollectionTestCaseEventManager = new Mock<IDataCollectionTestCaseEventManager>();

            this.mockTestCaseEvents = new Mock<ITestCaseEventsHandler>();
            this.testCasesEventsHandler = new TestCaseEventsHandler(this.mockDataCollectionTestCaseEventManager.Object, this.mockTestCaseEvents.Object);
        }

        [TestMethod]
        public void SendTestCaseStartShouldCallTriggerTestCaseStartOnInProcDataCollectionManager()
        {
            this.testCasesEventsHandler.SendTestCaseStart(new TestCase());
            this.mockDataCollectionTestCaseEventManager.Verify(x => x.RaiseTestCaseStart(It.IsAny<TestCaseStartEventArgs>()), Times.Once);
        }

        [TestMethod]
        public void SendTestCaseEndShouldCallTriggerTestCaseEndOnInProcDataCollectionManager()
        {
            this.testCasesEventsHandler.SendTestCaseEnd(new TestCase(), TestOutcome.Passed);
            this.mockDataCollectionTestCaseEventManager.Verify(x => x.RaiseTestCaseEnd(It.IsAny<TestCaseEndEventArgs>()), Times.Once);
        }

        [TestMethod]
        public void SendTestResultShouldCallTriggerUpdateTestResultOnInProcDataCollectionManager()
        {
            this.testCasesEventsHandler.SendTestResult(new TestResult(new TestCase()));
            this.mockDataCollectionTestCaseEventManager.Verify(x => x.RaiseTestResult(It.IsAny<TestResultEventArgs>()), Times.Once);

        }

        [TestMethod]
        public void TestCaseEventsFromClientsShouldBeCalledWhenTestCaseEventsAreCalled()
        {
            var testCase = new TestCase();
            this.testCasesEventsHandler.SendTestCaseStart(testCase);
            this.testCasesEventsHandler.SendTestCaseEnd(testCase, TestOutcome.Passed);
            var testResult = new TestResult(testCase);
            this.testCasesEventsHandler.SendTestResult(testResult);

            this.mockTestCaseEvents.Verify(x => x.SendTestCaseStart(testCase), Times.Once);
            this.mockTestCaseEvents.Verify(x => x.SendTestCaseEnd(testCase, TestOutcome.Passed), Times.Once);
            this.mockTestCaseEvents.Verify(x => x.SendTestResult(testResult), Times.Once);
        }
    }
}
