// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.HtmlLogger.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using VisualStudio.TestTools.UnitTesting;
    using Moq;
    using ObjectModel = VisualStudio.TestPlatform.ObjectModel;
    using VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger;
    using HtmlLoggerConstants = VisualStudio.TestPlatform.Extensions.HtmlLogger.Constants;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using HtmlLogger = VisualStudio.TestPlatform.Extensions.HtmlLogger;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using System.Linq;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using System.Runtime.Serialization;
    using Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger.ObjectModel;

    [TestClass]
    public class HtmlLoggerTests
    {
        private Mock<TestLoggerEvents> events;
        private HtmlLogger.HtmlLogger htmlLogger;
        private Dictionary<string, string> parameters;
        private static readonly string DefaultTestRunDirectory = Path.GetTempPath();
        private static readonly string DefaultLogFileNameParameterValue = "logfilevalue.html";
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
            this.htmlLogger = new HtmlLogger.HtmlLogger(this.mockFileHelper.Object, this.mockHtmlTransformer.Object, this.mockXmlSerializer.Object);
            this.parameters = new Dictionary<string, string>(2)
            {
                [DefaultLoggerParameterNames.TestRunDirectory] = HtmlLoggerTests.DefaultTestRunDirectory,
                [HtmlLoggerConstants.LogFileNameKey] = HtmlLoggerTests.DefaultLogFileNameParameterValue
            };
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
            const string testResultDir = @"C:\Code\abc";
            var events = new Mock<TestLoggerEvents>();

            this.htmlLogger.Initialize(events.Object, testResultDir);

            Assert.AreEqual(this.htmlLogger.TestResultsDirPath, testResultDir);
            Assert.IsNotNull(this.htmlLogger.TestRunDetails);
            Assert.IsNotNull(this.htmlLogger.Results);
        }

        [TestMethod]
        public void InitializeShouldThrowExceptionIfTestRunDirectoryIsEmptyOrNull()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () =>
                {
                    this.events = new Mock<TestLoggerEvents>();
                    this.parameters[DefaultLoggerParameterNames.TestRunDirectory] = null;
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
            const string message = "First message";
            var testRunMessageEventArgs = new TestRunMessageEventArgs(TestMessageLevel.Informational, message);
            
            this.htmlLogger.TestMessageHandler(new object(), testRunMessageEventArgs);
 
            var actualMessage = this.htmlLogger.TestRunDetails.RunLevelMessageInformational.First();
            Assert.AreEqual(message, actualMessage);   
        }

        [TestMethod]
        public void TestMessageHandlerShouldAddMessageInListIfItIsWarningAndError()
        {
            const string message = "error message";
            const string message2 = "warning message";

            var testRunMessageEventArgs = new TestRunMessageEventArgs(TestMessageLevel.Error, message);
            this.htmlLogger.TestMessageHandler(new object(), testRunMessageEventArgs);
            var testRunMessageEventArgs2 = new TestRunMessageEventArgs(TestMessageLevel.Warning, message2);
            this.htmlLogger.TestMessageHandler(new object(), testRunMessageEventArgs2);
            
            Assert.AreEqual(message, this.htmlLogger.TestRunDetails.RunLevelMessageErrorAndWarning.First());
            Assert.AreEqual(2, this.htmlLogger.TestRunDetails.RunLevelMessageErrorAndWarning.Count);
        }

        [TestMethod]
        public void TestResultHandlerShouldKeepTrackOfSummary()
        {
            var passTestCase1 = CreateTestCase("Pass1");
            var passTestCase2 = CreateTestCase("Pass2");
            var failTestCase1 = CreateTestCase("Fail1");
            var skipTestCase1 = CreateTestCase("Skip1");

            var passResult1 = new ObjectModel.TestResult(passTestCase1) {Outcome = TestOutcome.Passed};

            var passResult2 = new ObjectModel.TestResult(passTestCase2) {Outcome = TestOutcome.Passed};

            var failResult1 = new ObjectModel.TestResult(failTestCase1) {Outcome = TestOutcome.Failed};

            var skipResult1 = new ObjectModel.TestResult(skipTestCase1) {Outcome = TestOutcome.Skipped};

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

        [TestMethod]
        public void TestResultHandlerShouldSetDisplayNameIfNullProperly()
        {
            //this assert is for checking result display name equals to null
            var passTestCase1 = CreateTestCase("Pass1");
            var passTestResultExpected = new ObjectModel.TestResult(passTestCase1)
            {
                DisplayName = null, TestCase = {FullyQualifiedName = "abc"}
            };


            var pass1 = new Mock<TestResultEventArgs>(passTestResultExpected);
            this.htmlLogger.TestResultHandler(new object(), pass1.Object);

            var passTestResultActual = new HtmlLogger.ObjectModel.TestResult
            {
                ResultOutcome = TestOutcome.Passed, DisplayName = "abc"
            };


            Assert.AreEqual(passTestResultActual.DisplayName, this.htmlLogger.TestRunDetails.ResultCollectionList.First().ResultList.First().DisplayName);
       
            
            passTestResultExpected.DisplayName = "def";
            passTestResultExpected.TestCase.FullyQualifiedName = "abc";

            this.htmlLogger.TestResultHandler(new object(), pass1.Object);

            passTestResultActual.ResultOutcome = TestOutcome.Passed;
            passTestResultActual.DisplayName = "def";

            Assert.AreEqual(passTestResultActual.DisplayName, this.htmlLogger.TestRunDetails.ResultCollectionList.First().ResultList.Last().DisplayName);           
        }

        [TestMethod]
        public void TestResultHandlerShouldCreateTestResultProperly()
        {
                var passTestCase = CreateTestCase("Pass1");
                passTestCase.DisplayName = "abc";
                passTestCase.FullyQualifiedName = "fully";
                passTestCase.Source = "abc/def.dll";

            var passTestResultExpected = new ObjectModel.TestResult(passTestCase)
            {
                DisplayName = "def",
                ErrorMessage = "error message",
                ErrorStackTrace = "Error stack trace",
                Duration = TimeSpan.Zero
            };

            var eventArg = new Mock<TestResultEventArgs>(passTestResultExpected);

            this.htmlLogger.TestResultHandler(new object(), eventArg.Object);

            var result = this.htmlLogger.TestRunDetails.ResultCollectionList.First().ResultList.First();
           
            Assert.AreEqual(result.DisplayName, "def");
            Assert.AreEqual(result.ErrorMessage, "error message");
            Assert.AreEqual(result.ErrorStackTrace, "Error stack trace");
            Assert.AreEqual(result.FullyQualifiedName, "fully");
            Assert.AreEqual(this.htmlLogger.TestRunDetails.ResultCollectionList.First().Source, "abc/def.dll");
            Assert.AreEqual(result.Duration, null);
        }

        [TestMethod]
        public void TestResultHandlerShouldCreateOneTestEntryForEachTestCase()
        {
            TestCase testCase1 = CreateTestCase("TestCase1");
            TestCase testCase2 = CreateTestCase("TestCase2");

            ObjectModel.TestResult result1 = new ObjectModel.TestResult(testCase1) {Outcome = TestOutcome.Failed};

            ObjectModel.TestResult result2 = new ObjectModel.TestResult(testCase2) {Outcome = TestOutcome.Passed};

            Mock<TestResultEventArgs> resultEventArg1 = new Mock<TestResultEventArgs>(result1);
            Mock<TestResultEventArgs> resultEventArg2 = new Mock<TestResultEventArgs>(result2);

            this.htmlLogger.TestResultHandler(new object(), resultEventArg1.Object);
            this.htmlLogger.TestResultHandler(new object(), resultEventArg2.Object);

            Assert.AreEqual(this.htmlLogger.TestRunDetails.ResultCollectionList.First().ResultList.Count, 2, "TestResultHandler is not creating test result entry for each test case");
        }

        [TestMethod]
        public void TestResultHandlerShouldCreateOneTestResultCollectionForOneSource()
        {
            TestCase testCase1 = CreateTestCase("TestCase1");
            TestCase testCase2 = CreateTestCase("TestCase2");
            testCase1.Source = "abc.dll";
            testCase2.Source = "def.dll";

            ObjectModel.TestResult result1 = new ObjectModel.TestResult(testCase1) {Outcome = TestOutcome.Failed};

            ObjectModel.TestResult result2 = new ObjectModel.TestResult(testCase2) {Outcome = TestOutcome.Passed};

            Mock<TestResultEventArgs> resultEventArg1 = new Mock<TestResultEventArgs>(result1);
            Mock<TestResultEventArgs> resultEventArg2 = new Mock<TestResultEventArgs>(result2);

            this.htmlLogger.TestResultHandler(new object(), resultEventArg1.Object);
            this.htmlLogger.TestResultHandler(new object(), resultEventArg2.Object);

            Assert.AreEqual(this.htmlLogger.TestRunDetails.ResultCollectionList.Count, 2);
        }

        [TestMethod]
        public void TestResultHandlerShouldAddFailedResultToFailedResultListInTestResultCollection()
        {
            TestCase testCase1 = CreateTestCase("TestCase1");
            ObjectModel.TestResult result1 = new ObjectModel.TestResult(testCase1) {Outcome = TestOutcome.Failed};

            Mock<TestResultEventArgs> resultEventArg1 = new Mock<TestResultEventArgs>(result1);

            this.htmlLogger.TestResultHandler(new object(), resultEventArg1.Object);

            Assert.AreEqual(this.htmlLogger.TestRunDetails.ResultCollectionList.First().FailedResultList.Count,1);
        }

        [TestMethod]
        public void TestResultHandlerShouldAddHierarchicalResultsForOrderedTest()
        {
           TestCase testCase1 = CreateTestCase("TestCase1");
           TestCase testCase2 = CreateTestCase("TestCase2");
           TestCase testCase3 = CreateTestCase("TestCase3");

            Guid parentExecutionId = Guid.NewGuid();

            ObjectModel.TestResult result1 = new ObjectModel.TestResult(testCase1);
            result1.SetPropertyValue(HtmlLoggerConstants.ExecutionIdProperty, parentExecutionId);
            result1.SetPropertyValue(HtmlLoggerConstants.TestTypeProperty, HtmlLoggerConstants.OrderedTestTypeGuid);

            Mock<TestResultEventArgs> resultEventArg1 = new Mock<TestResultEventArgs>(result1);

            this.htmlLogger.TestResultHandler(new object(), resultEventArg1.Object);

            Assert.AreEqual(this.htmlLogger.TestRunDetails.ResultCollectionList.First().ResultList.Count, 1, "test handler is adding parent result correctly");
            Assert.IsNull(this.htmlLogger.TestRunDetails.ResultCollectionList.First().ResultList.First().InnerTestResults,  "test handler is adding child result correctly");

            var result2 = new ObjectModel.TestResult(testCase2);
            result2.SetPropertyValue(HtmlLoggerConstants.ExecutionIdProperty, Guid.NewGuid());
            result2.SetPropertyValue(HtmlLoggerConstants.ParentExecIdProperty, parentExecutionId);

            var result3 = new ObjectModel.TestResult(testCase3) {Outcome = TestOutcome.Failed};
            result3.SetPropertyValue(HtmlLoggerConstants.ExecutionIdProperty, Guid.NewGuid());
            result3.SetPropertyValue(HtmlLoggerConstants.ParentExecIdProperty, parentExecutionId);

            var resultEventArg2 = new Mock<TestResultEventArgs>(result2);
            var resultEventArg3 = new Mock<TestResultEventArgs>(result3);

            this.htmlLogger.TestResultHandler(new object(), resultEventArg2.Object);
            this.htmlLogger.TestResultHandler(new object(), resultEventArg3.Object);

            Assert.AreEqual(this.htmlLogger.TestRunDetails.ResultCollectionList.First().ResultList.Count, 1, "test handler is adding parent result correctly");
            Assert.AreEqual(this.htmlLogger.TestRunDetails.ResultCollectionList.First().ResultList.First().InnerTestResults.Count, 2, "test handler is adding child result correctly");
        }

        [TestMethod]
        public void TestCompleteHandlerShouldKeepTackOfSummary()
        {
            TestCase passTestCase1 = CreateTestCase("Pass1");
            TestCase passTestCase2 = CreateTestCase("Pass2");
            TestCase failTestCase1 = CreateTestCase("Fail1");
            TestCase skipTestCase1 = CreateTestCase("Skip1");

            var passResult1 = new ObjectModel.TestResult(passTestCase1) {Outcome = TestOutcome.Passed};

            var passResult2 = new ObjectModel.TestResult(passTestCase2) {Outcome = TestOutcome.Passed};

            var failResult1 = new ObjectModel.TestResult(failTestCase1) {Outcome = TestOutcome.Failed};

            var skipResult1 = new ObjectModel.TestResult(skipTestCase1) {Outcome = TestOutcome.Skipped};

            var pass1 = new Mock<TestResultEventArgs>(passResult1);
            var pass2 = new Mock<TestResultEventArgs>(passResult2);
            var fail1 = new Mock<TestResultEventArgs>(failResult1);
            var skip1 = new Mock<TestResultEventArgs>(skipResult1);


            this.htmlLogger.TestResultHandler(new object(), pass1.Object);
            this.htmlLogger.TestResultHandler(new object(), pass2.Object);
            this.htmlLogger.TestResultHandler(new object(), fail1.Object);
            this.htmlLogger.TestResultHandler(new object(), skip1.Object);

            this.mockFileHelper.Setup(x => x.GetStream(It.IsAny<string>(), FileMode.Create, FileAccess.ReadWrite)).Callback<string, FileMode, FileAccess>((x, y, z) =>
                {
                }).Returns(new Mock<Stream>().Object);

            this.htmlLogger.TestRunCompleteHandler(new object(), new TestRunCompleteEventArgs(null, false, true, null, null, TimeSpan.Zero));

            Assert.AreEqual(this.htmlLogger.TestRunDetails.Summary.TotalTests, 4, "summary should keep track of total tests");
            Assert.AreEqual(this.htmlLogger.TestRunDetails.Summary.FailedTests, 1, "summary should keep track of failed tests");
            Assert.AreEqual(this.htmlLogger.TestRunDetails.Summary.PassedTests, 2, "summary should keep track of passed tests");
            Assert.AreEqual(this.htmlLogger.TestRunDetails.Summary.SkippedTests, 1, "summary should keep track of passed tests");
            Assert.AreEqual(this.htmlLogger.TestRunDetails.Summary.PassPercentage, 50, "summary should keep track of passed tests");
            Assert.AreEqual(this.htmlLogger.TestRunDetails.Summary.TotalRunTime, null , "summary should keep track of passed tests");
        }

        [TestMethod]
        public void TestCompleteHandlerShouldCreateFileCorrectly()
        {
            var testCase1 = CreateTestCase("TestCase1");
            var result1 = new ObjectModel.TestResult(testCase1) {Outcome = TestOutcome.Failed};
            var resultEventArg1 = new Mock<TestResultEventArgs>(result1);
           

            this.mockFileHelper.Setup(x => x.GetStream(It.IsAny<string>(), FileMode.Create, FileAccess.ReadWrite)).Callback<string, FileMode, FileAccess>((x, y, z) =>
                {
                }).Returns(new Mock<Stream>().Object);

            this.htmlLogger.TestResultHandler(new object(), resultEventArg1.Object);
            this.htmlLogger.TestRunCompleteHandler(new object(), new TestRunCompleteEventArgs(null, false, true, null, null, TimeSpan.Zero));

            this.mockFileHelper.Verify(x => x.GetStream(It.IsAny<string>(), FileMode.Create, FileAccess.ReadWrite), Times.Once);
        }


        [TestMethod]
        public void TestCompleteHandlerShouldCallHtmlTransformerCorrectly()
        {
            var testCase1 = CreateTestCase("TestCase1");
            var result1 = new ObjectModel.TestResult(testCase1) {Outcome = TestOutcome.Failed};
            var resultEventArg1 = new Mock<TestResultEventArgs>(result1);

            this.mockFileHelper.Setup(x => x.GetStream(It.IsAny<string>(), FileMode.Create, FileAccess.ReadWrite)).Callback<string, FileMode, FileAccess>((x, y, z) =>
                {
                }).Returns(new Mock<Stream>().Object);

            this.htmlLogger.TestResultHandler(new object(), resultEventArg1.Object);
            this.htmlLogger.TestRunCompleteHandler(new object(), new TestRunCompleteEventArgs(null, false, true, null, null, TimeSpan.Zero));

            this.mockHtmlTransformer.Verify(x => x.Transform(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void TestCompleteHandlerShouldWriteToXmlSerializerCorrectly()
        {
            var testCase1 = CreateTestCase("TestCase1") ?? throw new ArgumentNullException($"CreateTestCase(\"TestCase1\")");
            var result1 = new ObjectModel.TestResult(testCase1) {Outcome = TestOutcome.Failed};
            var resultEventArg1 = new Mock<TestResultEventArgs>(result1);
            this.mockFileHelper.Setup(x => x.GetStream(It.IsAny<string>(), FileMode.Create, FileAccess.ReadWrite)).Callback<string, FileMode, FileAccess>((x, y, z) =>
                {
                }).Returns(new Mock<Stream>().Object);

            this.htmlLogger.TestResultHandler(new object(), resultEventArg1.Object);
            this.htmlLogger.TestRunCompleteHandler(new object(), new TestRunCompleteEventArgs(null, false, true, null, null, TimeSpan.Zero));

            this.mockXmlSerializer.Verify(x => x.WriteObject(It.IsAny<Stream>(), It.IsAny<TestRunDetails>()), Times.Once);
            Assert.IsTrue(htmlLogger.XmlFilePath.Contains(".xml"));
            Assert.IsTrue(htmlLogger.HtmlFilePath.Contains(".html"));
        }

        private static TestCase CreateTestCase(string testCaseName)
        {
            return new TestCase(testCaseName, new Uri("some://uri"), "DummySourceFileName");
        }
    }
}




  

