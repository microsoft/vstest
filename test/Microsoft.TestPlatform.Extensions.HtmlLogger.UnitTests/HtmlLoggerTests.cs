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
    using HtmlLoggerConstants = Microsoft.TestPlatform.Extensions.HtmlLogger.Utility.Constants;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using HtmlLogger = VisualStudio.TestPlatform.Extensions.HtmlLogger;
    using System.Text;
    using System.Collections.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using System.Linq;
    using System.Xml;
    using System.Xml.Linq;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    [TestClass]
    public class HtmlLoggerTests
    {
        private Mock<TestLoggerEvents> events;
        private Htmllogger htmlLogger;
        private Dictionary<string, string> parameters;
        private static string DefaultTestRunDirectory = Path.GetTempPath();
        private static string DefaultLogFileNameParameterValue = "logfilevalue.trx";
        private Mock<IFileHelper> mockFileHelper;

        [TestInitialize]
        public void Initialize()
        {
            this.events = new Mock<TestLoggerEvents>();
            this.htmlLogger = new Htmllogger();
            this.parameters = new Dictionary<string, string>(2);
            this.parameters[ObjectModel.DefaultLoggerParameterNames.TestRunDirectory] = HtmlLoggerTests.DefaultTestRunDirectory;
            this.parameters[HtmlLoggerConstants.LogFileNameKey] = HtmlLoggerTests.DefaultLogFileNameParameterValue;
            this.htmlLogger.Initialize(this.events.Object, this.parameters);


        }
        [TestMethod]
        public void InitializeShouldThrowExceptionIfEventsIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () =>
                {
                    this.htmlLogger.Initialize(null, this.parameters);
                });
        }

        [TestMethod]
        public void InitializeShouldInitializeAllProperties()
        {
            var testResultDir = @"C:\Code\abc";
            var events = new Mock<TestLoggerEvents>();

            this.htmlLogger.Initialize(events.Object, testResultDir);

            Assert.AreEqual(this.htmlLogger.TestResultsDirPath, testResultDir);
            Assert.IsNotNull(this.htmlLogger.GetTestResults());
           
            Assert.IsNotNull(this.htmlLogger.GetResults());
        }

        

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

      
        [TestMethod]
        public void InitializeShouldThrowExceptionIfParametersAreEmpty()
        {
            var events = new Mock<TestLoggerEvents>();
            Assert.ThrowsException<ArgumentException>(() => this.htmlLogger.Initialize(events.Object, new Dictionary<string, string>()));
        }

        [TestMethod]
        public void TestMessageHandlerShouldThrowExceptionIfEventArgsIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.htmlLogger.TestMessageHandler(new object(), default(TestRunMessageEventArgs));
            });
        }

        [TestMethod]
        public void TestMessageHandlerShouldAddMessageWhenItIsInformation()
        {
            string message = "First message";
            
            TestRunMessageEventArgs trme = new TestRunMessageEventArgs(TestMessageLevel.Informational, message);
            HtmlLogger.TestResults testResults = this.CreateTestResults();
            this.htmlLogger.SetTestResults(testResults);
            this.htmlLogger.TestMessageHandler(new object(), trme);
 
            string actualMessage = this.htmlLogger.GetTestResults().RunLevelMessageInformational.First();
            Assert.AreEqual(message, actualMessage.ToString());
           
        }

        [TestMethod]
        public void TestMessageHandlerShouldAddMessageInListIfItIsWarningAndError()
        {
            string message = "error message";
            string message2 = "warning message";
  
            HtmlLogger.TestResults testResults = this.CreateTestResults();
            this.htmlLogger.SetTestResults(testResults); 

            TestRunMessageEventArgs trme = new TestRunMessageEventArgs(TestMessageLevel.Error, message);
            this.htmlLogger.TestMessageHandler(new object(), trme);
            TestRunMessageEventArgs trme2 = new TestRunMessageEventArgs(TestMessageLevel.Warning, message2);
            this.htmlLogger.TestMessageHandler(new object(), trme2);
            
           

            Assert.AreEqual(message, this.htmlLogger.GetTestResults().RunLevelMessageErrorAndWarning.First());
            Assert.AreEqual(2, this.htmlLogger.GetTestResults().RunLevelMessageErrorAndWarning.Count());
        }

        
        
        [TestMethod]
        public void TestResultHandlerShouldKeepTrackofSummary()
        {
            ObjectModel.TestCase passTestCase1 = CreateTestCase("Pass1");
            ObjectModel.TestCase passTestCase2 = CreateTestCase("Pass2");
            ObjectModel.TestCase failTestCase1 = CreateTestCase("Fail1");
            ObjectModel.TestCase skipTestCase1 = CreateTestCase("Skip1");

            ObjectModel.TestResult passResult1 = new ObjectModel.TestResult(passTestCase1);
            passResult1.Outcome = ObjectModel.TestOutcome.Passed;

            ObjectModel.TestResult passResult2 = new ObjectModel.TestResult(passTestCase2);
            passResult2.Outcome = ObjectModel.TestOutcome.Passed;

            ObjectModel.TestResult failResult1 = new ObjectModel.TestResult(failTestCase1);
            failResult1.Outcome = ObjectModel.TestOutcome.Failed;

            ObjectModel.TestResult skipResult1 = new ObjectModel.TestResult(skipTestCase1);
            skipResult1.Outcome = ObjectModel.TestOutcome.Skipped;

            Mock<TestResultEventArgs> pass1 = new Mock<TestResultEventArgs>(passResult1);
            Mock<TestResultEventArgs> pass2 = new Mock<TestResultEventArgs>(passResult2);
            Mock<TestResultEventArgs> fail1 = new Mock<TestResultEventArgs>(failResult1);
            Mock<TestResultEventArgs> skip1 = new Mock<TestResultEventArgs>(skipResult1);


            this.htmlLogger.TestResultHandler(new object(), pass1.Object);
            this.htmlLogger.TestResultHandler(new object(), pass2.Object);
            this.htmlLogger.TestResultHandler(new object(), fail1.Object);
            this.htmlLogger.TestResultHandler(new object(), skip1.Object);

            Assert.AreEqual(this.htmlLogger.passTests, 2, "Passed Tests");
            Assert.AreEqual(this.htmlLogger.failTests, 1, "Failed Tests");
            Assert.AreEqual(this.htmlLogger.totalTests, 4, "Total Tests");

        }

        [TestMethod]
        public void TestResultHandlerShouldCreateDisplayNameIfNullProperly()
        {
            //this assert is for checking result dispalyname equals to null
            ObjectModel.TestCase passTestCase1 = CreateTestCase("Pass1");
            ObjectModel.TestResult PassTestResultExpected = new ObjectModel.TestResult(passTestCase1);
            PassTestResultExpected.DisplayName = null;
            PassTestResultExpected.TestCase.DisplayName = "abc";
         

            Mock<TestResultEventArgs> pass1 = new Mock<TestResultEventArgs>(PassTestResultExpected);
            this.htmlLogger.TestResultHandler(new object(), pass1.Object);

            HtmlLogger.TestResult passTestResultActual = new HtmlLogger.TestResult();
            
            passTestResultActual.resultOutcome = TestOutcome.Passed;
            passTestResultActual.DisplayName = "abc";

            Assert.AreEqual(passTestResultActual.DisplayName, this.htmlLogger.GetTestResults().Results.First().DisplayName);
          //  Assert.AreEqual( passTestResultActual,this.htmlLogger.GetTestResults().Results.First());

            //this assert is for checking result dispalyname equals to notnull

            ObjectModel.TestCase passTestCase2 = CreateTestCase("Pass1");
            ObjectModel.TestResult PassTestResultExpected1 = new ObjectModel.TestResult(passTestCase1);
            PassTestResultExpected.DisplayName = "def";
            PassTestResultExpected.TestCase.DisplayName = "abc";

            Mock<TestResultEventArgs> pass2= new Mock<TestResultEventArgs>(PassTestResultExpected);
            this.htmlLogger.TestResultHandler(new object(), pass1.Object);

            HtmlLogger.TestResult passTestResultActual1 = new HtmlLogger.TestResult();
            passTestResultActual.resultOutcome = TestOutcome.Passed;
            passTestResultActual.DisplayName = "def";

            Assert.AreEqual(passTestResultActual.DisplayName, this.htmlLogger.GetTestResults().Results.Last().DisplayName);
           // Assert.AreEqual(passTestResultActual1, this.htmlLogger.GetTestResults().Results.First());


        }

        [TestMethod]
        public void TestResultHandlerShouldCreateDisplayNameIfNotNullProperly()
        {
           
            //this assert is for checking result dispalyname equals to notnull

            ObjectModel.TestCase passTestCase2 = CreateTestCase("Pass1");
            ObjectModel.TestResult PassTestResultExpected1 = new ObjectModel.TestResult(passTestCase2);
            PassTestResultExpected1.DisplayName = "def";
            PassTestResultExpected1.TestCase.DisplayName = "abc";

            Mock<TestResultEventArgs> pass2 = new Mock<TestResultEventArgs>(PassTestResultExpected1);
            this.htmlLogger.TestResultHandler(new object(), pass2.Object);

            HtmlLogger.TestResult passTestResultActual1 = new HtmlLogger.TestResult();
            passTestResultActual1.resultOutcome = TestOutcome.Passed;
            passTestResultActual1.DisplayName = "def";


            Assert.AreEqual(passTestResultActual1, this.htmlLogger.GetTestResults().Results.First());

        }

        [TestMethod]
        public void TestResultHandlerShouldCreateTestResultProperly()
        {

            //this assert is for checking result dispalyname equals to notnull

            ObjectModel.TestCase passTestCase2 = CreateTestCase("Pass1");
                passTestCase2.DisplayName = "abc";
                passTestCase2.FullyQualifiedName = "fully";

            ObjectModel.TestResult PassTestResultExpected1 = new ObjectModel.TestResult(passTestCase2)
            {
                DisplayName = "def",
                ErrorMessage = "error message",
                ErrorStackTrace = "Error strack trace"
            };


            Mock<TestResultEventArgs> eventArg = new Mock<TestResultEventArgs>(PassTestResultExpected1);

            this.htmlLogger.TestResultHandler(new object(), eventArg.Object);

            var result = this.htmlLogger.GetTestResults().Results.First();

            Assert.AreEqual(result.Duration, "");
            Assert.AreEqual(result.Duration, "");
            Assert.AreEqual(result.Duration, "");
            Assert.AreEqual(result.Duration, "");
            Assert.AreEqual(result.Duration, "");

        }



        private HtmlLogger.TestResult CreateTestResult(ObjectModel.TestOutcome testoutcome,string displayName)
        {

            HtmlLogger.TestResult testResult = new HtmlLogger.TestResult();
            testResult.resultOutcome = testoutcome;
            testResult.DisplayName = displayName;
            return testResult;
        }

        [TestMethod]
        public void TestResultHandlerShouldCreateOneTestEntryForEachTestCase()
        {
            ObjectModel.TestCase testCase1 = CreateTestCase("TestCase1");
            ObjectModel.TestCase testCase2 = CreateTestCase("TestCase2");

            HtmlLogger.TestResults testResults = this.CreateTestResults();
            this.htmlLogger.SetTestResults(testResults);

            ObjectModel.TestResult result1 = new ObjectModel.TestResult(testCase1);
            result1.Outcome = ObjectModel.TestOutcome.Failed;

            ObjectModel.TestResult result2 = new ObjectModel.TestResult(testCase2);
            result2.Outcome = ObjectModel.TestOutcome.Passed;

            Mock<TestResultEventArgs> resultEventArg1 = new Mock<TestResultEventArgs>(result1);
            Mock<TestResultEventArgs> resultEventArg2 = new Mock<TestResultEventArgs>(result2);

            this.htmlLogger.TestResultHandler(new object(), resultEventArg1.Object);
            this.htmlLogger.TestResultHandler(new object(), resultEventArg2.Object);

            Assert.AreEqual(this.htmlLogger.GetTestResults().GetTestResultscount(), 2, "TestResultHandler is not creating test result entry for each test case");
        }

        [TestMethod]
        public void TestResultHandlerShouldAddHierarchicalResultsForOrderedTest()
        {
            ObjectModel.TestCase testCase1 = CreateTestCase("TestCase1");
            ObjectModel.TestCase testCase2 = CreateTestCase("TestCase2");
            ObjectModel.TestCase testCase3 = CreateTestCase("TestCase3");

            Guid parentExecutionId = Guid.NewGuid();

            ObjectModel.TestResult result1 = new ObjectModel.TestResult(testCase1);
            result1.SetPropertyValue<Guid>(HtmlLoggerConstants.ExecutionIdProperty, parentExecutionId);
            result1.SetPropertyValue<Guid>(HtmlLoggerConstants.TestTypeProperty, HtmlLoggerConstants.OrderedTestTypeGuid);

            ObjectModel.TestResult result2 = new ObjectModel.TestResult(testCase2);
            result2.SetPropertyValue<Guid>(HtmlLoggerConstants.ExecutionIdProperty, Guid.NewGuid());
            result2.SetPropertyValue<Guid>(HtmlLoggerConstants.ParentExecIdProperty, parentExecutionId);

            ObjectModel.TestResult result3 = new ObjectModel.TestResult(testCase3);
            result3.Outcome = ObjectModel.TestOutcome.Failed;
            result3.SetPropertyValue<Guid>(HtmlLoggerConstants.ExecutionIdProperty, Guid.NewGuid());
            result3.SetPropertyValue<Guid>(HtmlLoggerConstants.ParentExecIdProperty, parentExecutionId);

            Mock<TestResultEventArgs> resultEventArg1 = new Mock<TestResultEventArgs>(result1);
            Mock<TestResultEventArgs> resultEventArg2 = new Mock<TestResultEventArgs>(result2);
            Mock<TestResultEventArgs> resultEventArg3 = new Mock<TestResultEventArgs>(result3);

            this.htmlLogger.TestResultHandler(new object(), resultEventArg1.Object);
            this.htmlLogger.TestResultHandler(new object(), resultEventArg2.Object);
            this.htmlLogger.TestResultHandler(new object(), resultEventArg3.Object);

            Assert.AreEqual(this.htmlLogger.GetTestResults().GetTestResultscount(), 1, "testhandler is adding parent result correctly");
            Assert.AreEqual(this.htmlLogger.GetTestResults().Results[0].GetInnerTestResultscount(), 2, "testhandler is adding child result correctly");

           
        }

        [TestMethod]
        public void TestCompleteHandlerShouldKeepTackOfSummary()
        {
            ObjectModel.TestCase passTestCase1 = CreateTestCase("Pass1");
            ObjectModel.TestCase passTestCase2 = CreateTestCase("Pass2");
            ObjectModel.TestCase failTestCase1 = CreateTestCase("Fail1");
            ObjectModel.TestCase skipTestCase1 = CreateTestCase("Skip1");

            HtmlLogger.TestResults testResults = this.CreateTestResults();
            this.htmlLogger.SetTestResults(testResults);

            ObjectModel.TestResult passResult1 = new ObjectModel.TestResult(passTestCase1);
            passResult1.Outcome = ObjectModel.TestOutcome.Passed;

            ObjectModel.TestResult passResult2 = new ObjectModel.TestResult(passTestCase2);
            passResult2.Outcome = ObjectModel.TestOutcome.Passed;

            ObjectModel.TestResult failResult1 = new ObjectModel.TestResult(failTestCase1);
            failResult1.Outcome = ObjectModel.TestOutcome.Failed;

            ObjectModel.TestResult skipResult1 = new ObjectModel.TestResult(skipTestCase1);
            skipResult1.Outcome = ObjectModel.TestOutcome.Skipped;

            Mock<TestResultEventArgs> pass1 = new Mock<TestResultEventArgs>(passResult1);
            Mock<TestResultEventArgs> pass2 = new Mock<TestResultEventArgs>(passResult2);
            Mock<TestResultEventArgs> fail1 = new Mock<TestResultEventArgs>(failResult1);
            Mock<TestResultEventArgs> skip1 = new Mock<TestResultEventArgs>(skipResult1);


            this.htmlLogger.TestResultHandler(new object(), pass1.Object);
            this.htmlLogger.TestResultHandler(new object(), pass2.Object);
            this.htmlLogger.TestResultHandler(new object(), fail1.Object);
            this.htmlLogger.TestResultHandler(new object(), skip1.Object);

            this.htmlLogger.TestRunCompleteHandler(new object(), new TestRunCompleteEventArgs(null, false, true, null, null, TimeSpan.Zero));

            Assert.AreEqual(this.htmlLogger.GetTestResults().Summary.TotalTests, 4, "summary should keep track of totaltests");
            Assert.AreEqual(this.htmlLogger.GetTestResults().Summary.FailedTests, 1, "summary should keep track of failedtests");
            Assert.AreEqual(this.htmlLogger.GetTestResults().Summary.PassedTests, 2, "summary should keep track of passedtests");
        }

        [TestMethod]
        public void TestRunInformationShouldContainUtcDateTime()
        {
            HtmlLogger.TestResults testResults = this.CreateTestResults();
            this.htmlLogger.SetTestResults(testResults);
            this.MakeTestRunComplete();


            this.ValidateDateTimeInTrx(this.htmlLogger.htmlFileName,"should handle htmlfilename time to meet bounds of utc");
            this.ValidateDateTimeInTrx(this.htmlLogger.xmlFileName, "should handle xmlfilename time to meet bounds of utc");
        }

        private void ValidateDateTimeInTrx(string htmlFileName,string error)
        {
            this.mockFileHelper = new Mock<IFileHelper>();
            this.mockFileHelper.Verify(x => x.WriteAllTextToFile(htmlFileName, It.IsAny<string>()), Times.Once);

            //using (FileStream file = File.OpenRead(htmlFileName))
            //{
            //    using (XmlReader reader = XmlReader.Create(htmlFileName))
            //    {
            //        XDocument document = XDocument.Load(htmlFileName);
            //        var timesNode = document.Descendants(document.Root.GetDefaultNamespace() + "Times").FirstOrDefault();
            //        ValidateTimeWithinUtcLimits(DateTimeOffset.Parse(timesNode.Attributes("creation").FirstOrDefault().Value),error);
            //        ValidateTimeWithinUtcLimits(DateTimeOffset.Parse(timesNode.Attributes("start").FirstOrDefault().Value),error);
            //        var resultNode = document.Descendants(document.Root.GetDefaultNamespace() + "UnitTestResult").FirstOrDefault();
            //        ValidateTimeWithinUtcLimits(DateTimeOffset.Parse(resultNode.Attributes("endTime").FirstOrDefault().Value),error);
            //        ValidateTimeWithinUtcLimits(DateTimeOffset.Parse(resultNode.Attributes("startTime").FirstOrDefault().Value),error);
            //    }
            //}
        }

        private void ValidateTimeWithinUtcLimits(DateTimeOffset dateTime,string error)
        {
            Assert.IsTrue(dateTime.UtcDateTime.Subtract(DateTime.UtcNow) < new TimeSpan(0, 0, 0, 60),error);
        }

        private HtmlLogger.TestResults CreateTestResults()
        {
            HtmlLogger.TestResults testresults = new HtmlLogger.TestResults();
            //testresults.Summary.PassedTests = 1;
            return testresults;
        }

        private static TestCase CreateTestCase(string testCaseName)
        {
            return new ObjectModel.TestCase(testCaseName, new Uri("some://uri"), "DummySourceFileName");
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




  

