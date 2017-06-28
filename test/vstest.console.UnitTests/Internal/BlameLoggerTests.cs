using Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors;
using Microsoft.VisualStudio.TestPlatform.Common.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using vstest.console.UnitTests.TestDoubles;

namespace vstest.console.UnitTests.Internal
{
    class BlameLoggerTests
    {
        //private Mock<ITestRunRequest> testRunRequest;
        //private Mock<TestLoggerEvents> events;
        //private Mock<IOutput> mockOutput;
        //private TestLoggerManager testLoggerManager;
        //private BlameLogger blameLogger;

        //[TestInitialize]
        //public void Initialize()
        //{
        //    RunTestsArgumentProcessorTests.SetupMockExtensions();

        //    // Setup Mocks and other dependencies
        //    this.Setup();
        //}

        //[TestCleanup]
        //public void Cleanup()
        //{
        //    DummyTestLoggerManager.Cleanup();
        //}

        //[TestMethod]
        //public void InitializeShouldThrowExceptionIfEventsIsNull()
        //{
        //    Assert.ThrowsException<ArgumentNullException>(() =>
        //    {
        //        this.blameLogger.Initialize(null, string.Empty);
        //    });
        //}

        //[TestMethod]
        //public void InitializeWithParametersShouldThrowExceptionIfParametersIsNull()
        //{
        //    Assert.ThrowsException<ArgumentNullException>(() =>
        //    {
        //        this.blameLogger.Initialize(new Mock<TestLoggerEvents>().Object, null);
        //    });
        //}

        //[TestMethod]
        //public void TestMessageHandlerShouldThrowExceptionIfEventArgsIsNull()
        //{
        //    // Raise an event on mock object
        //    Assert.ThrowsException<ArgumentNullException>(() =>
        //    {
        //        this.testRunRequest.Raise(m => m.TestRunMessage += null, default(TestRunMessageEventArgs));
        //    });
        //}
        //[TestMethod]
        //public void TestResultHandlerShouldThowExceptionIfEventArgsIsNull()
        //{
        //    // Raise an event on mock object
        //    Assert.ThrowsException<NullReferenceException>(() =>
        //    {
        //        testRunRequest.Raise(m => m.OnRunStatsChange += null, default(TestRunChangedEventArgs));
        //    });
        //}


        //[TestMethod]
        //public void TestResulCompleteHandlerShouldThowExceptionIfEventArgsIsNull()
        //{
        //    // Raise an event on mock object
        //    Assert.ThrowsException<NullReferenceException>(() =>
        //    {
        //        this.testRunRequest.Raise(m => m.OnRunCompletion += null, default(TestRunCompleteEventArgs));
        //    });
        //}
        //private void Setup()
        //{
        //    // mock for ITestRunRequest
        //    this.testRunRequest = new Mock<ITestRunRequest>();
        //    this.events = new Mock<TestLoggerEvents>();
        //    this.mockOutput = new Mock<IOutput>();

        //    this.blameLogger = new BlameLogger(this.mockOutput.Object);

        //    DummyTestLoggerManager.Cleanup();

        //    // Create Instance of TestLoggerManager
        //    this.testLoggerManager = TestLoggerManager.Instance;
        //    this.testLoggerManager.AddLogger(this.blameLogger, BlameLogger.ExtensionUri, new Dictionary<string, string>());
        //    this.testLoggerManager.EnableLogging();

        //    // Register TestRunRequest object
        //    this.testLoggerManager.RegisterTestRunEvents(this.testRunRequest.Object);
        //}
        //private List<Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult> GetTestResultObject(TestOutcome outcome)
        //{
        //    var testcase = new TestCase("TestName", new Uri("some://uri"), "TestSource");
        //    var testresult = new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult(testcase);
        //    testresult.Outcome = outcome;
        //    var testresultList = new List<Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult> { testresult };
        //    return testresultList;
        //}
    }
}
