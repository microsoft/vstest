// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.HtmlLogger.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using ObjectModel = VisualStudio.TestPlatform.ObjectModel;
    using VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger;
    using HtmlLoggerConstants = Utility.Constants;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using HtmlLogger = VisualStudio.TestPlatform.Extensions.HtmlLogger;
    using System.Collections.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using System.Linq;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using System.Runtime.Serialization;

    [TestClass]
    public class HtmlLoggerTests
    {
        private Mock<TestLoggerEvents> events;
        private Htmllogger htmlLogger;
        private Dictionary<string, string> parameters;
        private static string DefaultTestRunDirectory = Path.GetTempPath();
        private static string DefaultLogFileNameParameterValue = "logfilevalue.html";

        private Mock<IFileHelper> mockFileHelper;
        private Mock<XmlObjectSerializer> mockXmlSerializer ;
        private Mock<IHtmlTransformer> mockHtmlTransformer;

        [TestInitialize]
        public void Initialize()
        {
            this.events = new Mock<TestLoggerEvents>();
            this.mockFileHelper = new Mock<IFileHelper>();
            this.mockHtmlTransformer = new Mock<IHtmlTransformer>();
            this.mockXmlSerializer = new Mock<XmlObjectSerializer>();
            this.htmlLogger = new Htmllogger(this.mockFileHelper.Object, this.mockHtmlTransformer.Object, this.mockXmlSerializer.Object);
            this.parameters = new Dictionary<string, string>(2);
            this.parameters[DefaultLoggerParameterNames.TestRunDirectory] = HtmlLoggerTests.DefaultTestRunDirectory;
            this.parameters[HtmlLoggerConstants.LogFileNameKey] = HtmlLoggerTests.DefaultLogFileNameParameterValue;
            this.htmlLogger.Initialize(this.events.Object, this.parameters);
        }

        /// <summary>
        /// if events is null initialize should throw exception
        /// </summary>
        [TestMethod]
        public void InitializeShouldThrowExceptionIfEventsIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () =>
                {
                    this.htmlLogger.Initialize(null, this.parameters);
                });
        }

        /// <summary>
        /// initilaize should initialize all Properties
        /// </summary>
        [TestMethod]
        public void InitializeShouldInitializeAllProperties()
        {
            var testResultDir = @"C:\Code\abc";
            var events = new Mock<TestLoggerEvents>();

            this.htmlLogger.Initialize(events.Object, testResultDir);

            Assert.AreEqual(this.htmlLogger.TestResultsDirPath, testResultDir);
            Assert.IsNotNull(this.htmlLogger.TestResults);
            Assert.IsNotNull(this.htmlLogger.Results);
        }

        /// <summary>
        /// if test run directory is null the initialize should throw exception
        /// </summary>
        [TestMethod]
        public void InitializeShouldThrowExceptionIfTestRunDirectoryIsEmptyOrNull()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () =>
                {
                    this.events = new Mock<TestLoggerEvents>();
                    this.parameters[ObjectModel.DefaultLoggerParameterNames.TestRunDirectory] = null;
                    this.htmlLogger.Initialize(events.Object, parameters);
                });
        }

        /// <summary>
        /// initialize should throw exception if parameters are empty
        /// </summary>
        [TestMethod]
        public void InitializeShouldThrowExceptionIfParametersAreEmpty()
        {
            var events = new Mock<TestLoggerEvents>();
            Assert.ThrowsException<ArgumentException>(() => this.htmlLogger.Initialize(events.Object, new Dictionary<string, string>()));
        }

        /// <summary>
        /// if event args is null test message handler should throw exception
        /// </summary>
        [TestMethod]
        public void TestMessageHandlerShouldThrowExceptionIfEventArgsIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.htmlLogger.TestMessageHandler(new object(), default(TestRunMessageEventArgs));
            });
        }

        /// <summary>
        /// Test message handler should add informational messages to list of informational strings in test results
        /// </summary>
        [TestMethod]
        public void TestMessageHandlerShouldAddMessageWhenItIsInformation()
        {
            string message = "First message";
            TestRunMessageEventArgs trme = new TestRunMessageEventArgs(TestMessageLevel.Informational, message);
            
            this.htmlLogger.TestMessageHandler(new object(), trme);
 
            string actualMessage = this.htmlLogger.TestResults.RunLevelMessageInformational.First();
            Assert.AreEqual(message, actualMessage.ToString());   
        }

        /// <summary>
        /// Test message handler should add ierror and warning messages to list of error and warning strings in test results
        /// </summary>
        [TestMethod]
        public void TestMessageHandlerShouldAddMessageInListIfItIsWarningAndError()
        {
            string message = "error message";
            string message2 = "warning message";

            TestRunMessageEventArgs trme = new TestRunMessageEventArgs(TestMessageLevel.Error, message);
            this.htmlLogger.TestMessageHandler(new object(), trme);
            TestRunMessageEventArgs trme2 = new TestRunMessageEventArgs(TestMessageLevel.Warning, message2);
            this.htmlLogger.TestMessageHandler(new object(), trme2);
            
            Assert.AreEqual(message, this.htmlLogger.TestResults.RunLevelMessageErrorAndWarning.First());
            Assert.AreEqual(2, this.htmlLogger.TestResults.RunLevelMessageErrorAndWarning.Count());
        }

        /// <summary>
        /// Test result handler should keep track of passed failed total skipped tests summary
        /// </summary>
        [TestMethod]
        public void TestResultHandlerShouldKeepTrackofSummary()
        {
            TestCase passTestCase1 = CreateTestCase("Pass1");
            TestCase passTestCase2 = CreateTestCase("Pass2");
            TestCase failTestCase1 = CreateTestCase("Fail1");
            TestCase skipTestCase1 = CreateTestCase("Skip1");

            ObjectModel.TestResult passResult1 = new ObjectModel.TestResult(passTestCase1);
            passResult1.Outcome = TestOutcome.Passed;

            ObjectModel.TestResult passResult2 = new ObjectModel.TestResult(passTestCase2);
            passResult2.Outcome = TestOutcome.Passed;

            ObjectModel.TestResult failResult1 = new ObjectModel.TestResult(failTestCase1);
            failResult1.Outcome = TestOutcome.Failed;

            ObjectModel.TestResult skipResult1 = new ObjectModel.TestResult(skipTestCase1);
            skipResult1.Outcome = TestOutcome.Skipped;

            Mock<TestResultEventArgs> pass1 = new Mock<TestResultEventArgs>(passResult1);
            Mock<TestResultEventArgs> pass2 = new Mock<TestResultEventArgs>(passResult2);
            Mock<TestResultEventArgs> fail1 = new Mock<TestResultEventArgs>(failResult1);
            Mock<TestResultEventArgs> skip1 = new Mock<TestResultEventArgs>(skipResult1);


            this.htmlLogger.TestResultHandler(new object(), pass1.Object);
            this.htmlLogger.TestResultHandler(new object(), pass2.Object);
            this.htmlLogger.TestResultHandler(new object(), fail1.Object);
            this.htmlLogger.TestResultHandler(new object(), skip1.Object);

            Assert.AreEqual(this.htmlLogger.PassedTests, 2, "Passed Tests");
            Assert.AreEqual(this.htmlLogger.FailedTests, 1, "Failed Tests");
            Assert.AreEqual(this.htmlLogger.TotalTests, 4, "Total Tests");
        }

        /// <summary>
        /// Test Result handler should set dispaly name in test result Properly
        /// </summary>
        [TestMethod]
        public void TestResultHandlerShouldSetDisplayNameIfNullProperly()
        {
            //this assert is for checking result dispalyname equals to null
            TestCase passTestCase1 = CreateTestCase("Pass1");
            ObjectModel.TestResult PassTestResultExpected = new ObjectModel.TestResult(passTestCase1);
            PassTestResultExpected.DisplayName = null;
            PassTestResultExpected.TestCase.DisplayName = "abc";
         

            Mock<TestResultEventArgs> pass1 = new Mock<TestResultEventArgs>(PassTestResultExpected);
            this.htmlLogger.TestResultHandler(new object(), pass1.Object);

            HtmlLogger.TestResult passTestResultActual = new HtmlLogger.TestResult();
            
            passTestResultActual.resultOutcome = TestOutcome.Passed;
            passTestResultActual.DisplayName = "abc";

            Assert.AreEqual(passTestResultActual.DisplayName, this.htmlLogger.TestResults.Results.First().DisplayName);
       
            TestCase passTestCase2 = CreateTestCase("Pass1");
            ObjectModel.TestResult PassTestResultExpected1 = new ObjectModel.TestResult(passTestCase1);
            PassTestResultExpected.DisplayName = "def";
            PassTestResultExpected.TestCase.DisplayName = "abc";

            Mock<TestResultEventArgs> pass2= new Mock<TestResultEventArgs>(PassTestResultExpected);
            this.htmlLogger.TestResultHandler(new object(), pass1.Object);

            HtmlLogger.TestResult passTestResultActual1 = new HtmlLogger.TestResult();
            passTestResultActual.resultOutcome = TestOutcome.Passed;
            passTestResultActual.DisplayName = "def";

            Assert.AreEqual(passTestResultActual.DisplayName, this.htmlLogger.TestResults.Results.Last().DisplayName);           
        }

        /// <summary>
        /// Test Result Handler should create test result  properly
        /// </summary>
        [TestMethod]
        public void TestResultHandlerShouldCreateTestResultProperly()
        {
                TestCase passTestCase2 = CreateTestCase("Pass1");
                passTestCase2.DisplayName = "abc";
                passTestCase2.FullyQualifiedName = "fully";

            ObjectModel.TestResult PassTestResultExpected1 = new ObjectModel.TestResult(passTestCase2)
            {
                DisplayName = "def",
                ErrorMessage = "error message",
                ErrorStackTrace = "Error strack trace",
                Duration = TimeSpan.Zero
            };

            Mock<TestResultEventArgs> eventArg = new Mock<TestResultEventArgs>(PassTestResultExpected1);

            this.htmlLogger.TestResultHandler(new object(), eventArg.Object);

            var result = this.htmlLogger.TestResults.Results.First();
           
            Assert.AreEqual(result.DisplayName, "def");
            Assert.AreEqual(result.ErrorMessage, "error message");
            Assert.AreEqual(result.ErrorStackTrace, "Error strack trace");
            Assert.AreEqual(result.FullyQualifiedName, "fully");
            Assert.AreEqual(result.Duration, null);
        }

        /// <summary>
        /// test result should create one test result for one resutlt event args 
        /// </summary>
        [TestMethod]
        public void TestResultHandlerShouldCreateOneTestEntryForEachTestCase()
        {
            TestCase testCase1 = CreateTestCase("TestCase1");
            TestCase testCase2 = CreateTestCase("TestCase2");
            
            ObjectModel.TestResult result1 = new ObjectModel.TestResult(testCase1);
            result1.Outcome = TestOutcome.Failed;

            ObjectModel.TestResult result2 = new ObjectModel.TestResult(testCase2);
            result2.Outcome = TestOutcome.Passed;

            Mock<TestResultEventArgs> resultEventArg1 = new Mock<TestResultEventArgs>(result1);
            Mock<TestResultEventArgs> resultEventArg2 = new Mock<TestResultEventArgs>(result2);

            this.htmlLogger.TestResultHandler(new object(), resultEventArg1.Object);
            this.htmlLogger.TestResultHandler(new object(), resultEventArg2.Object);

            Assert.AreEqual(this.htmlLogger.TestResults.GetTestResultscount(), 2, "TestResultHandler is not creating test result entry for each test case");
        }

        [TestMethod]
        public void TestResultHandlerShouldAddHierarchicalResultsForOrderedTest()
        {
           TestCase testCase1 = CreateTestCase("TestCase1");
           TestCase testCase2 = CreateTestCase("TestCase2");
           TestCase testCase3 = CreateTestCase("TestCase3");

            Guid parentExecutionId = Guid.NewGuid();

            ObjectModel.TestResult result1 = new ObjectModel.TestResult(testCase1);
            result1.SetPropertyValue<Guid>(HtmlLoggerConstants.ExecutionIdProperty, parentExecutionId);
            result1.SetPropertyValue<Guid>(HtmlLoggerConstants.TestTypeProperty, HtmlLoggerConstants.OrderedTestTypeGuid);

            Mock<TestResultEventArgs> resultEventArg1 = new Mock<TestResultEventArgs>(result1);

            this.htmlLogger.TestResultHandler(new object(), resultEventArg1.Object);

            Assert.AreEqual(this.htmlLogger.TestResults.GetTestResultscount(), 1, "testhandler is adding parent result correctly");
            Assert.IsNull(this.htmlLogger.TestResults.Results[0].innerTestResults,  "testhandler is adding child result correctly");

            ObjectModel.TestResult result2 = new ObjectModel.TestResult(testCase2);
            result2.SetPropertyValue<Guid>(HtmlLoggerConstants.ExecutionIdProperty, Guid.NewGuid());
            result2.SetPropertyValue<Guid>(HtmlLoggerConstants.ParentExecIdProperty, parentExecutionId);

            ObjectModel.TestResult result3 = new ObjectModel.TestResult(testCase3);
            result3.Outcome = TestOutcome.Failed;
            result3.SetPropertyValue<Guid>(HtmlLoggerConstants.ExecutionIdProperty, Guid.NewGuid());
            result3.SetPropertyValue<Guid>(HtmlLoggerConstants.ParentExecIdProperty, parentExecutionId);

            Mock<TestResultEventArgs> resultEventArg2 = new Mock<TestResultEventArgs>(result2);
            Mock<TestResultEventArgs> resultEventArg3 = new Mock<TestResultEventArgs>(result3);

            this.htmlLogger.TestResultHandler(new object(), resultEventArg2.Object);
            this.htmlLogger.TestResultHandler(new object(), resultEventArg3.Object);

            Assert.AreEqual(this.htmlLogger.TestResults.GetTestResultscount(), 1, "testhandler is adding parent result correctly");
            Assert.AreEqual(this.htmlLogger.TestResults.Results[0].GetInnerTestResultscount(), 2, "testhandler is adding child result correctly");
        }

        /// <summary>
        /// Test comple handler should set summary property in test results properly
        /// </summary>
        [TestMethod]
        public void TestCompleteHandlerShouldKeepTackOfSummary()
        {
            TestCase passTestCase1 = CreateTestCase("Pass1");
            TestCase passTestCase2 = CreateTestCase("Pass2");
            TestCase failTestCase1 = CreateTestCase("Fail1");
            TestCase skipTestCase1 = CreateTestCase("Skip1");

            ObjectModel.TestResult passResult1 = new ObjectModel.TestResult(passTestCase1);
            passResult1.Outcome = TestOutcome.Passed;

            ObjectModel.TestResult passResult2 = new ObjectModel.TestResult(passTestCase2);
            passResult2.Outcome = TestOutcome.Passed;

            ObjectModel.TestResult failResult1 = new ObjectModel.TestResult(failTestCase1);
            failResult1.Outcome = TestOutcome.Failed;

            ObjectModel.TestResult skipResult1 = new ObjectModel.TestResult(skipTestCase1);
            skipResult1.Outcome = TestOutcome.Skipped;

            Mock<TestResultEventArgs> pass1 = new Mock<TestResultEventArgs>(passResult1);
            Mock<TestResultEventArgs> pass2 = new Mock<TestResultEventArgs>(passResult2);
            Mock<TestResultEventArgs> fail1 = new Mock<TestResultEventArgs>(failResult1);
            Mock<TestResultEventArgs> skip1 = new Mock<TestResultEventArgs>(skipResult1);


            this.htmlLogger.TestResultHandler(new object(), pass1.Object);
            this.htmlLogger.TestResultHandler(new object(), pass2.Object);
            this.htmlLogger.TestResultHandler(new object(), fail1.Object);
            this.htmlLogger.TestResultHandler(new object(), skip1.Object);

            string fileName;
            this.mockFileHelper.Setup(x => x.GetStream(It.IsAny<string>(), FileMode.Create, FileAccess.ReadWrite)).Callback<string, FileMode, FileAccess>((x, y, z) => fileName = x).Returns(new Mock<Stream>().Object);

            this.htmlLogger.TestRunCompleteHandler(new object(), new TestRunCompleteEventArgs(null, false, true, null, null, TimeSpan.Zero));

            Assert.AreEqual(this.htmlLogger.TestResults.Summary.TotalTests, 4, "summary should keep track of totaltests");
            Assert.AreEqual(this.htmlLogger.TestResults.Summary.FailedTests, 1, "summary should keep track of failedtests");
            Assert.AreEqual(this.htmlLogger.TestResults.Summary.PassedTests, 2, "summary should keep track of passedtests");
        }

        /// <summary>
        /// Test complete handle should create file correctly
        /// </summary>
        [TestMethod]
        public void TestCompleteHandlerShouldCreateFileCorrectly()
        {
            string fileName;
            this.mockFileHelper.Setup(x => x.GetStream(It.IsAny<string>(), FileMode.Create, FileAccess.ReadWrite)).Callback<string, FileMode, FileAccess>((x, y, z) => fileName = x).Returns(new Mock<Stream>().Object);

            this.htmlLogger.TestRunCompleteHandler(new object(), new TestRunCompleteEventArgs(null, false, true, null, null, TimeSpan.Zero));

            this.mockFileHelper.Verify(x => x.GetStream(It.IsAny<string>(), FileMode.Create, FileAccess.ReadWrite), Times.Once);
        }

        [TestMethod]
        public void TestCompleteHandlerShouldCallHtmlTransformerCorrectly()
        {
            string fileName;
            this.mockFileHelper.Setup(x => x.GetStream(It.IsAny<string>(), FileMode.Create, FileAccess.ReadWrite)).Callback<string, FileMode, FileAccess>((x, y, z) => fileName = x).Returns(new Mock<Stream>().Object);
            this.htmlLogger.TestRunCompleteHandler(new object(), new TestRunCompleteEventArgs(null, false, true, null, null, TimeSpan.Zero));
            this.mockHtmlTransformer.Verify(x => x.Transform(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void TestCompleteHandlerShouldWriteToXmlSerializerCorrectly()
        {
            string fileName;
            this.mockFileHelper.Setup(x => x.GetStream(It.IsAny<string>(), FileMode.Create, FileAccess.ReadWrite)).Callback<string, FileMode, FileAccess>((x, y, z) => fileName = x).Returns(new Mock<Stream>().Object);          

            this.htmlLogger.TestRunCompleteHandler(new object(), new TestRunCompleteEventArgs(null, false, true, null, null, TimeSpan.Zero));

            this.mockXmlSerializer.Verify(x => x.WriteObject(It.IsAny<Stream>(), It.IsAny<TestRunDetails>()), Times.Once);
            Assert.IsTrue(htmlLogger.XmlFilePath.Contains(".xml"));
            Assert.IsTrue(htmlLogger.HtmlFilePath.Contains(".html"));
        }

        private HtmlLogger.TestResult CreateTestResult(TestOutcome testoutcome, string displayName)
        {

            HtmlLogger.TestResult testResult = new HtmlLogger.TestResult();
            testResult.resultOutcome = testoutcome;
            testResult.DisplayName = displayName;
            return testResult;
        }

        private TestRunDetails CreateTestResults()
        {
            TestRunDetails testresults = new TestRunDetails();
            return testresults;
        }

        private static TestCase CreateTestCase(string testCaseName)
        {
            return new TestCase(testCaseName, new Uri("some://uri"), "DummySourceFileName");
        }

        private static TestRunCompleteEventArgs CreateTestRunCompleteEventArgs()
        {
            var testRunCompleteEventArgs = new TestRunCompleteEventArgs(null, false, false, null,
                new Collection<AttachmentSet>(), new TimeSpan(1, 0, 0, 0));
            return testRunCompleteEventArgs;
        }

        private static Mock<TestResultEventArgs> CreatePassTestResultEventArgsMock(string testCaseName = "Pass1", List<TestResultMessage> testResultMessages = null)
        {
            TestCase passTestCase = CreateTestCase(testCaseName);
            var passResult = new ObjectModel.TestResult(passTestCase);

            if (testResultMessages != null && testResultMessages.Any())
            {
                foreach (var message in testResultMessages)
                {
                    passResult.Messages.Add(message);
                }
            }
            return new Mock<TestResultEventArgs>(passResult);
        }

        private void MakeTestRunComplete()
        {
            var pass = HtmlLoggerTests.CreatePassTestResultEventArgsMock();
            this.htmlLogger.TestResultHandler(new object(), pass.Object);
            var testRunCompleteEventArgs = HtmlLoggerTests.CreateTestRunCompleteEventArgs();
            this.htmlLogger.TestRunCompleteHandler(new object(), testRunCompleteEventArgs);
        }

    }
}




  

