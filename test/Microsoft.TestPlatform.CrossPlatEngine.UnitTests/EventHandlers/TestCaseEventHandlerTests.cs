
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
        private TestableInProcDataCollectionExtensionManager testableInProcDataCollectionExtensionManager;

        private Mock<ITestCaseEventsHandler> mockTestCaseEvents;

        private TestCaseEventsHandler testCasesEventsHandler;

        private string settingsXml = @"<RunSettings>
                                    <InProcDataCollectionRunSettings>
                                        <InProcDataCollectors>
                                            <InProcDataCollector friendlyName='Test Impact' uri='InProcDataCollector://Microsoft/TestImpact/1.0' assemblyQualifiedName='TestImpactListener.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=7ccb7239ffde675a'  codebase='E:\repos\MSTest\src\managed\TestPlatform\TestImpactListener.Tests\bin\Debug\TestImpactListener.Tests.dll'>
                                                <Configuration>
                                                    <Port>4312</Port>
                                                </Configuration>
                                            </InProcDataCollector>
                                        </InProcDataCollectors>
                                    </InProcDataCollectionRunSettings>
                                </RunSettings>";

        [TestInitialize]
        public void InitializeTests()
        {
            this.testableInProcDataCollectionExtensionManager = new TestableInProcDataCollectionExtensionManager(settingsXml, null);
            this.mockTestCaseEvents = new Mock<ITestCaseEventsHandler>();
            this.testCasesEventsHandler = new TestCaseEventsHandler(this.testableInProcDataCollectionExtensionManager, this.mockTestCaseEvents.Object);
        }

        [TestMethod]
        public void SendTestCaseStartShouldCallTriggerTestCaseStartOnInProcDataCollectionManager()
        {            
            this.testCasesEventsHandler.SendTestCaseStart(null);
            Assert.IsTrue(
                (this.testableInProcDataCollectionExtensionManager.TestCaseStartCalled == 1),
                "TestCaseStart must be called once");
        }

        [TestMethod]
        public void SendTestCaseEndShouldCallTriggerTestCaseEndOnInProcDataCollectionManager()
        {
            this.testCasesEventsHandler.SendTestCaseEnd(null, TestOutcome.Passed);
            Assert.IsTrue(
                (this.testableInProcDataCollectionExtensionManager.TestCaseEndCalled == 1),
                "TestCaseStart must be called once");
        }

        [TestMethod]
        public void SendTestResultShouldCallTriggerUpdateTestResultOnInProcDataCollectionManager()
        {
            this.testCasesEventsHandler.SendTestResult(null);
            Assert.IsTrue(
                (this.testableInProcDataCollectionExtensionManager.UpdateTestResult == 1),
                "TestCaseStart must be called once");
        }

        [TestMethod]
        public void TestCaseEventsFromClientsShouldBeCalledWhenTestCaseEventsAreCalled()
        {
            this.testCasesEventsHandler.SendTestCaseStart(null);
            this.testCasesEventsHandler.SendTestCaseEnd(null, TestOutcome.Passed);
            this.testCasesEventsHandler.SendTestResult(null);

            this.mockTestCaseEvents.Verify(x => x.SendTestCaseStart(null), Times.Once);
            this.mockTestCaseEvents.Verify(x => x.SendTestCaseEnd(null, TestOutcome.Passed), Times.Once);
            this.mockTestCaseEvents.Verify(x => x.SendTestResult(null), Times.Once);
        }

        internal class TestableInProcDataCollectionExtensionManager : InProcDataCollectionExtensionManager
        {
            public TestableInProcDataCollectionExtensionManager(string runSettings, ITestRunCache testRunCache) : base(runSettings, testRunCache)
            {
            }

            public IDictionary<string, IInProcDataCollector> DataCollectors => this.inProcDataCollectors;

            protected override IInProcDataCollector CreateDataCollector(DataCollectorSettings dataCollectorSettings, TypeInfo interfaceTypeInfo)
            {
                return new MockDataCollector(dataCollectorSettings);
            }

            public int TestSessionStartCalled { get; private set; }
            public int TestSessionEndCalled { get; private set; }
            public int TestCaseStartCalled { get; private set; }
            public int TestCaseEndCalled { get; private set; }
            public int UpdateTestResult { get; private set; }

            public override void TriggerTestSessionStart()
            {
                TestSessionStartCalled++;
            }

            public override void TriggerTestSessionEnd()
            {
                TestSessionEndCalled++;
            }

            public override void TriggerTestCaseStart(TestCase testCase)
            {
                TestCaseStartCalled++;
            }

            public override void TriggerTestCaseEnd(TestCase testCase, TestOutcome outcome)
            {
                TestCaseEndCalled++;
            }

            public override bool TriggerUpdateTestResult(TestResult testResult)
            {
                this.UpdateTestResult++;
                return true;
            }
        }

        public class MockDataCollector : IInProcDataCollector
        {
            public MockDataCollector(DataCollectorSettings dataCollectorSettings)
            {
                this.DataCollectorSettings = dataCollectorSettings;
            }

            public string AssemblyQualifiedName => this.DataCollectorSettings.AssemblyQualifiedName;

            public DataCollectorSettings DataCollectorSettings
            {
                get;
                private set;
            }

            public void LoadDataCollector(IDataCollectionSink inProcDataCollectionSink)
            {
                // Do Nothing
            }

            public void TriggerInProcDataCollectionMethod(string methodName, InProcDataCollectionArgs methodArg)
            {
            }
        }
    }
}
