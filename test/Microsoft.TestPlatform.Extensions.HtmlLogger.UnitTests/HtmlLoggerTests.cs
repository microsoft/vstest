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
        private Mock<XmlObjectSerializer> mockXmlSerializer;
        private Mock<IHtmlTransformer> mockHtmlTransformer;

        [TestInitialize]
        public void TestInitialize()
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

        #region Initialize Method

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
                this.htmlLogger.TestMessageHandler(new object(), default);
            });
        }

        #endregion

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
        public void TestMessageHandlerShouldNotInitializelistForInformationErrorAndWarningMessages()
        {
            Assert.IsNull(this.htmlLogger.TestRunDetails.RunLevelMessageInformational);
            Assert.IsNull(this.htmlLogger.TestRunDetails.RunLevelMessageErrorAndWarning);
        }

        [TestMethod]
        public void TestCompleteHandlerShouldThrowExceptionIfParametersAreNull()
        {
            Dictionary<string, string> parameters = null;
            var events = new Mock<TestLoggerEvents>();
            Assert.ThrowsException<ArgumentNullException>(() => this.htmlLogger.Initialize(events.Object, parameters));
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
        public void TestResultHandlerShouldKeepTrackOfFailedResult()
        {
            var failTestCase1 = CreateTestCase("Fail1");

            var failResult1 = new ObjectModel.TestResult(failTestCase1) { Outcome = TestOutcome.Failed };

            this.htmlLogger.TestResultHandler(new object(), new Mock<TestResultEventArgs>(failResult1).Object);

            Assert.AreEqual(1, this.htmlLogger.FailedTests, "Failed Tests");
        }

        [TestMethod]
        public void TestResultHandlerShouldKeepTrackOfTotalResult()
        {
            var passTestCase1 = CreateTestCase("Pass1");
            var passResult1 = new ObjectModel.TestResult(passTestCase1) { Outcome = TestOutcome.Passed };

            this.htmlLogger.TestResultHandler(new object(), new Mock<TestResultEventArgs>(passResult1).Object);

            Assert.AreEqual(1, this.htmlLogger.TotalTests, "Total Tests");
        }

        [TestMethod]
        public void TestResultHandlerShouldKeepTrackOfPassedResult()
        {
            var passTestCase2 = CreateTestCase("Pass2");
            var passResult2 = new ObjectModel.TestResult(passTestCase2) { Outcome = TestOutcome.Passed };

            this.htmlLogger.TestResultHandler(new object(), new Mock<TestResultEventArgs>(passResult2).Object);

            Assert.AreEqual(1, this.htmlLogger.PassedTests, "Passed Tests");
        }

        [TestMethod]
        public void TestResultHandlerShouldKeepTrackOfSkippedResult()
        {
            var skipTestCase1 = CreateTestCase("Skip1");
            var skipResult1 = new ObjectModel.TestResult(skipTestCase1) { Outcome = TestOutcome.Skipped };

            this.htmlLogger.TestResultHandler(new object(), new Mock<TestResultEventArgs>(skipResult1).Object);

            Assert.AreEqual(1, this.htmlLogger.SkippedTests, "Skipped Tests");
        }

        [TestMethod]
        public void TestResultHandlerShouldSetDisplayNameIfDisplayNameIsNull()
        {
            //this assert is for checking result display name equals to null
            var passTestCase1 = CreateTestCase("Pass1");
            var passTestResultExpected = new ObjectModel.TestResult(passTestCase1)
            {
                DisplayName = null,
                TestCase = { FullyQualifiedName = "abc" }
            };

            this.htmlLogger.TestResultHandler(new object(), new Mock<TestResultEventArgs>(passTestResultExpected).Object);

            Assert.AreEqual("abc", this.htmlLogger.TestRunDetails.ResultCollectionList.First().ResultList.First().DisplayName);
        }

        [TestMethod]
        public void TestResultHandlerShouldSetDisplayNameIfDisplayNameIsNotNull()
        {
            //this assert is for checking result display name not equals to null
            var passTestCase1 = CreateTestCase("Pass1");
            var passTestResultExpected = new ObjectModel.TestResult(passTestCase1)
            {
                DisplayName = "def",
                TestCase = { FullyQualifiedName = "abc" }
            };

            this.htmlLogger.TestResultHandler(new object(), new Mock<TestResultEventArgs>(passTestResultExpected).Object);

            Assert.AreEqual("def", this.htmlLogger.TestRunDetails.ResultCollectionList.First().ResultList.Last().DisplayName);
        }

        [TestMethod]
        public void TestResultHandlerShouldCreateTestResultProperly()
        {
            var passTestCase = CreateTestCase("Pass1");
            passTestCase.DisplayName = "abc";
            passTestCase.FullyQualifiedName = "fully";
            passTestCase.Source = "abc/def.dll";
            TimeSpan ts1 = new TimeSpan(0, 0, 0, 1, 0);

            var passTestResultExpected = new ObjectModel.TestResult(passTestCase)
            {
                DisplayName = "def",
                ErrorMessage = "error message",
                ErrorStackTrace = "Error stack trace",
                Duration = ts1
            };

            var eventArg = new Mock<TestResultEventArgs>(passTestResultExpected);
            // Act
            this.htmlLogger.TestResultHandler(new object(), eventArg.Object);

            var result = this.htmlLogger.TestRunDetails.ResultCollectionList.First().ResultList.First();

            Assert.AreEqual("def", result.DisplayName);
            Assert.AreEqual("error message", result.ErrorMessage);
            Assert.AreEqual("Error stack trace", result.ErrorStackTrace);
            Assert.AreEqual("fully", result.FullyQualifiedName);
            Assert.AreEqual("abc/def.dll", this.htmlLogger.TestRunDetails.ResultCollectionList.First().Source);
            Assert.AreEqual("1s", result.Duration);
        }

        [TestMethod]
        public void GetFormattedDurationStringShouldGiveCorrectFormat()
        {
            TimeSpan ts1 = new TimeSpan(0, 0, 0, 0, 1);
            Assert.AreEqual("1ms", htmlLogger.GetFormattedDurationString(ts1));

            TimeSpan ts2 = new TimeSpan(0, 0, 0, 1, 0);
            Assert.AreEqual("1s", htmlLogger.GetFormattedDurationString(ts2));

            TimeSpan ts3 = new TimeSpan(0, 0, 1, 0, 1);
            Assert.AreEqual("1m", htmlLogger.GetFormattedDurationString(ts3));

            TimeSpan ts4 = new TimeSpan(0, 1, 0, 2, 3);
            Assert.AreEqual("1h", htmlLogger.GetFormattedDurationString(ts4));

            TimeSpan ts5 = new TimeSpan(0, 1, 2, 3, 4);
            Assert.AreEqual("1h 2m", htmlLogger.GetFormattedDurationString(ts5));

            TimeSpan ts6 = new TimeSpan(0, 0, 1, 2, 3);
            Assert.AreEqual("1m 2s", htmlLogger.GetFormattedDurationString(ts6));

            TimeSpan ts7 = new TimeSpan(0, 0, 0, 1, 3);
            Assert.AreEqual("1s 3ms", htmlLogger.GetFormattedDurationString(ts7));

            TimeSpan ts8 = new TimeSpan(2);
            Assert.AreEqual("< 1ms", htmlLogger.GetFormattedDurationString(ts8));

            TimeSpan ts10 = new TimeSpan(1, 0, 0, 1, 3);
            Assert.AreEqual("> 1d", htmlLogger.GetFormattedDurationString(ts10));

            TimeSpan ts9 = new TimeSpan(0, 0, 0, 0, 0);
            Assert.IsNull(htmlLogger.GetFormattedDurationString(ts9));
        }

        [TestMethod]
        public void TestResultHandlerShouldCreateOneTestEntryForEachTestCase()
        {
            TestCase testCase1 = CreateTestCase("TestCase1");
            TestCase testCase2 = CreateTestCase("TestCase2");
            ObjectModel.TestResult result1 = new ObjectModel.TestResult(testCase1) { Outcome = TestOutcome.Failed };
            ObjectModel.TestResult result2 = new ObjectModel.TestResult(testCase2) { Outcome = TestOutcome.Passed };
            Mock<TestResultEventArgs> resultEventArg1 = new Mock<TestResultEventArgs>(result1);
            Mock<TestResultEventArgs> resultEventArg2 = new Mock<TestResultEventArgs>(result2);

            // Act
            this.htmlLogger.TestResultHandler(new object(), resultEventArg1.Object);
            this.htmlLogger.TestResultHandler(new object(), resultEventArg2.Object);

            Assert.AreEqual(2, this.htmlLogger.TestRunDetails.ResultCollectionList.First().ResultList.Count, "TestResultHandler is not creating test result entry for each test case");
        }

        [TestMethod]
        public void TestResultHandlerShouldCreateOneTestResultCollectionForOneSource()
        {
            TestCase testCase1 = CreateTestCase("TestCase1");
            testCase1.Source = "abc.dll";

            TestCase testCase2 = CreateTestCase("TestCase2");
            testCase2.Source = "def.dll";

            ObjectModel.TestResult result1 = new ObjectModel.TestResult(testCase1) { Outcome = TestOutcome.Failed };
            ObjectModel.TestResult result2 = new ObjectModel.TestResult(testCase2) { Outcome = TestOutcome.Passed };

            this.htmlLogger.TestResultHandler(new object(), new Mock<TestResultEventArgs>(result1).Object);
            this.htmlLogger.TestResultHandler(new object(), new Mock<TestResultEventArgs>(result2).Object);

            Assert.AreEqual(2, this.htmlLogger.TestRunDetails.ResultCollectionList.Count);
            Assert.AreEqual("abc.dll", this.htmlLogger.TestRunDetails.ResultCollectionList.First().Source);
            Assert.AreEqual("def.dll", this.htmlLogger.TestRunDetails.ResultCollectionList.Last().Source);
        }

        [TestMethod]
        public void TestResultHandlerShouldAddFailedResultToFailedResultListInTestResultCollection()
        {
            TestCase testCase1 = CreateTestCase("TestCase1");
            ObjectModel.TestResult result1 = new ObjectModel.TestResult(testCase1) { Outcome = TestOutcome.Failed };

            this.htmlLogger.TestResultHandler(new object(), new Mock<TestResultEventArgs>(result1).Object);

            Assert.AreEqual(1, this.htmlLogger.TestRunDetails.ResultCollectionList.First().FailedResultList.Count);
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

            this.htmlLogger.TestResultHandler(new object(), new Mock<TestResultEventArgs>(result1).Object);

            Assert.AreEqual(1, this.htmlLogger.TestRunDetails.ResultCollectionList.First().ResultList.Count, "test handler is adding parent result correctly");
            Assert.IsNull(this.htmlLogger.TestRunDetails.ResultCollectionList.First().ResultList.First().InnerTestResults, "test handler is adding child result correctly");

            var result2 = new ObjectModel.TestResult(testCase2);
            result2.SetPropertyValue(HtmlLoggerConstants.ExecutionIdProperty, Guid.NewGuid());
            result2.SetPropertyValue(HtmlLoggerConstants.ParentExecIdProperty, parentExecutionId);

            var result3 = new ObjectModel.TestResult(testCase3) { Outcome = TestOutcome.Failed };
            result3.SetPropertyValue(HtmlLoggerConstants.ExecutionIdProperty, Guid.NewGuid());
            result3.SetPropertyValue(HtmlLoggerConstants.ParentExecIdProperty, parentExecutionId);

            this.htmlLogger.TestResultHandler(new object(), new Mock<TestResultEventArgs>(result2).Object);
            this.htmlLogger.TestResultHandler(new object(), new Mock<TestResultEventArgs>(result3).Object);

            Assert.AreEqual(1, this.htmlLogger.TestRunDetails.ResultCollectionList.First().ResultList.Count, "test handler is adding parent result correctly");
            Assert.AreEqual(2, this.htmlLogger.TestRunDetails.ResultCollectionList.First().ResultList.First().InnerTestResults.Count, "test handler is adding child result correctly");
        }

        [TestMethod]
        public void TestCompleteHandlerShouldKeepTackOfSummary()
        {
            TestCase passTestCase1 = CreateTestCase("Pass1");
            TestCase passTestCase2 = CreateTestCase("Pass2");
            TestCase failTestCase1 = CreateTestCase("Fail1");
            TestCase skipTestCase1 = CreateTestCase("Skip1");
            var passResult1 = new ObjectModel.TestResult(passTestCase1) { Outcome = TestOutcome.Passed };
            var passResult2 = new ObjectModel.TestResult(passTestCase2) { Outcome = TestOutcome.Passed };
            var failResult1 = new ObjectModel.TestResult(failTestCase1) { Outcome = TestOutcome.Failed };
            var skipResult1 = new ObjectModel.TestResult(skipTestCase1) { Outcome = TestOutcome.Skipped };

            this.htmlLogger.TestResultHandler(new object(), new Mock<TestResultEventArgs>(passResult1).Object);
            this.htmlLogger.TestResultHandler(new object(), new Mock<TestResultEventArgs>(passResult2).Object);
            this.htmlLogger.TestResultHandler(new object(), new Mock<TestResultEventArgs>(failResult1).Object);
            this.htmlLogger.TestResultHandler(new object(), new Mock<TestResultEventArgs>(skipResult1).Object);

            this.mockFileHelper.Setup(x => x.GetStream(It.IsAny<string>(), FileMode.Create, FileAccess.ReadWrite)).Callback<string, FileMode, FileAccess>((x, y, z) =>
                {
                }).Returns(new Mock<Stream>().Object);

            this.htmlLogger.TestRunCompleteHandler(new object(), new TestRunCompleteEventArgs(null, false, true, null, null, TimeSpan.Zero));

            Assert.AreEqual(4, this.htmlLogger.TestRunDetails.Summary.TotalTests, "summary should keep track of total tests");
            Assert.AreEqual(1, this.htmlLogger.TestRunDetails.Summary.FailedTests, "summary should keep track of failed tests");
            Assert.AreEqual(2, this.htmlLogger.TestRunDetails.Summary.PassedTests, "summary should keep track of passed tests");
            Assert.AreEqual(1, this.htmlLogger.TestRunDetails.Summary.SkippedTests, "summary should keep track of passed tests");
            Assert.AreEqual(50, this.htmlLogger.TestRunDetails.Summary.PassPercentage, "summary should keep track of passed tests");
            Assert.IsNull(this.htmlLogger.TestRunDetails.Summary.TotalRunTime, "summary should keep track of passed tests");
        }

        [TestMethod]
        public void TestCompleteHandlerShouldCreateCustumHtmlFileNamewithLogFileNameKey()
        {
            var parameters = new Dictionary<string, string>();
            parameters[HtmlLoggerConstants.LogFileNameKey] = null;
            parameters[DefaultLoggerParameterNames.TestRunDirectory] = "dsa";

            var testCase1 = CreateTestCase("TestCase1");
            var result1 = new ObjectModel.TestResult(testCase1) { Outcome = TestOutcome.Failed };
            var resultEventArg1 = new Mock<TestResultEventArgs>(result1);
            this.htmlLogger.TestResultHandler(new object(), resultEventArg1.Object);

            this.htmlLogger.Initialize(new Mock<TestLoggerEvents>().Object, parameters);
            this.htmlLogger.TestRunCompleteHandler(new object(), new TestRunCompleteEventArgs(null, false, true, null, null, TimeSpan.Zero));
            Assert.IsTrue(this.htmlLogger.HtmlFilePath.Contains("TestResult"));
        }

        [TestMethod]
        public void TestCompleteHandlerShouldCreateCustumHtmlFileNameWithLogPrefix()
        {
            var parameters = new Dictionary<string, string>();
            parameters[HtmlLoggerConstants.LogFilePrefixKey] = "sample";
            parameters[DefaultLoggerParameterNames.TestRunDirectory] = "dsa";
            parameters[DefaultLoggerParameterNames.TargetFramework] = ".NETFramework,Version=4.5.1";

            var testCase1 = CreateTestCase("TestCase1");
            var result1 = new ObjectModel.TestResult(testCase1) { Outcome = TestOutcome.Failed };
            var resultEventArg1 = new Mock<TestResultEventArgs>(result1);
            this.htmlLogger.TestResultHandler(new object(), resultEventArg1.Object);

            this.htmlLogger.Initialize(new Mock<TestLoggerEvents>().Object, parameters);
            this.htmlLogger.TestRunCompleteHandler(new object(), new TestRunCompleteEventArgs(null, false, true, null, null, TimeSpan.Zero));
            Assert.IsFalse(this.htmlLogger.HtmlFilePath.Contains("__"));
        }

        [TestMethod]
        public void TestCompleteHandlerShouldCreateCustumHtmlFileNameWithLogPrefixIfTargetFrameworkIsNull()
        {
            var parameters = new Dictionary<string, string>();
            parameters[HtmlLoggerConstants.LogFilePrefixKey] = "sample";
            parameters[DefaultLoggerParameterNames.TestRunDirectory] = "dsa";
            parameters[DefaultLoggerParameterNames.TargetFramework] = ".NETFramework,Version=4.5.1";

            var testCase1 = CreateTestCase("TestCase1");
            var result1 = new ObjectModel.TestResult(testCase1) { Outcome = TestOutcome.Failed };
            var resultEventArg1 = new Mock<TestResultEventArgs>(result1);
            this.htmlLogger.TestResultHandler(new object(), resultEventArg1.Object);

            this.htmlLogger.Initialize(new Mock<TestLoggerEvents>().Object, parameters);
            this.htmlLogger.TestRunCompleteHandler(new object(), new TestRunCompleteEventArgs(null, false, true, null, null, TimeSpan.Zero));
            Assert.IsTrue(this.htmlLogger.HtmlFilePath.Contains("sample_net451"));
        }

        [TestMethod]
        public void TestCompleteHandlerShouldCreateCustumHtmlFileNameWithLogPrefixNull()
        {
            var parameters = new Dictionary<string, string>();
            parameters[HtmlLoggerConstants.LogFilePrefixKey] = null;
            parameters[DefaultLoggerParameterNames.TestRunDirectory] = "dsa";
            parameters[DefaultLoggerParameterNames.TargetFramework] = ".NETFramework,Version=4.5.1";

            var testCase1 = CreateTestCase("TestCase1");
            var result1 = new ObjectModel.TestResult(testCase1) { Outcome = TestOutcome.Failed };
            var resultEventArg1 = new Mock<TestResultEventArgs>(result1);

            this.mockFileHelper.Setup(x => x.GetStream(It.IsAny<string>(), FileMode.Create, FileAccess.ReadWrite)).Callback<string, FileMode, FileAccess>((x, y, z) =>
            {
            }).Returns(new Mock<Stream>().Object);

            this.htmlLogger.TestResultHandler(new object(), resultEventArg1.Object);
            this.htmlLogger.TestRunCompleteHandler(new object(), new TestRunCompleteEventArgs(null, false, true, null, null, TimeSpan.Zero));

            this.mockFileHelper.Verify(x => x.GetStream(It.IsAny<string>(), FileMode.Create, FileAccess.ReadWrite), Times.Once);
        }

        [TestMethod]
        public void TestCompleteHandlerShouldThrowExceptionWithLogPrefixIfTargetFrameworkKeyIsNotPresent()
        {
            var parameters = new Dictionary<string, string>();
            parameters[HtmlLoggerConstants.LogFilePrefixKey] = "sample.html";
            parameters[DefaultLoggerParameterNames.TestRunDirectory] = "dsa";
            var testCase1 = CreateTestCase("TestCase1");
            var result1 = new ObjectModel.TestResult(testCase1) { Outcome = TestOutcome.Failed };
            var resultEventArg1 = new Mock<TestResultEventArgs>(result1);
            this.htmlLogger.TestResultHandler(new object(), resultEventArg1.Object);

            this.htmlLogger.Initialize(new Mock<TestLoggerEvents>().Object, parameters);

            Assert.ThrowsException<KeyNotFoundException>(() => this.htmlLogger.TestRunCompleteHandler(new object(), new TestRunCompleteEventArgs(null, false, true, null, null, TimeSpan.Zero)));
        }

        [TestMethod]
        public void IntializeShouldThrowExceptionIfBothPrefixAndNameProvided()
        {
            this.parameters[HtmlLoggerConstants.LogFileNameKey] = "results.html";
            var trxPrefix = Path.Combine(Path.GetTempPath(), "results");
            this.parameters[HtmlLoggerConstants.LogFilePrefixKey] = "HtmlPrefix";
            this.parameters[DefaultLoggerParameterNames.TargetFramework] = ".NETFramework,Version=4.5.1";

            Assert.ThrowsException<ArgumentException>(() => this.htmlLogger.Initialize(events.Object, this.parameters));
        }

        [TestMethod]
        public void TestCompleteHandlerShouldCreateFileCorrectly()
        {
            var testCase1 = CreateTestCase("TestCase1");
            var result1 = new ObjectModel.TestResult(testCase1) { Outcome = TestOutcome.Failed };
            var resultEventArg1 = new Mock<TestResultEventArgs>(result1);

            this.mockFileHelper.Setup(x => x.GetStream(It.IsAny<string>(), FileMode.Create, FileAccess.ReadWrite)).Callback<string, FileMode, FileAccess>((x, y, z) =>
                {
                }).Returns(new Mock<Stream>().Object);

            this.htmlLogger.TestResultHandler(new object(), resultEventArg1.Object);
            this.htmlLogger.TestRunCompleteHandler(new object(), new TestRunCompleteEventArgs(null, false, true, null, null, TimeSpan.Zero));

            this.mockFileHelper.Verify(x => x.GetStream(It.IsAny<string>(), FileMode.Create, FileAccess.ReadWrite), Times.Once);
        }

        [TestMethod]
        public void TestCompleteHandlerShouldDeleteFileCorrectly()
        {
            var testCase1 = CreateTestCase("TestCase1");
            var result1 = new ObjectModel.TestResult(testCase1) { Outcome = TestOutcome.Failed };
            var resultEventArg1 = new Mock<TestResultEventArgs>(result1);

            this.mockFileHelper.Setup(x => x.Delete(It.IsAny<string>())).Callback<string>((x) =>
            {
            });

            this.htmlLogger.TestResultHandler(new object(), resultEventArg1.Object);
            this.htmlLogger.TestRunCompleteHandler(new object(), new TestRunCompleteEventArgs(null, false, true, null, null, TimeSpan.Zero));

            this.mockFileHelper.Verify(x => x.Delete(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void TestCompleteHandlerShouldCallHtmlTransformerCorrectly()
        {
            var testCase1 = CreateTestCase("TestCase1");
            var result1 = new ObjectModel.TestResult(testCase1) { Outcome = TestOutcome.Failed };
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
            var result1 = new ObjectModel.TestResult(testCase1) { Outcome = TestOutcome.Failed };
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

        [TestMethod]
        public void TestCompleteHandlerShouldNotDivideByZeroWhenThereAre0TestResults()
        {
            this.mockFileHelper.Setup(x => x.GetStream(It.IsAny<string>(), FileMode.Create, FileAccess.ReadWrite)).Callback<string, FileMode, FileAccess>((x, y, z) =>
            {
            }).Returns(new Mock<Stream>().Object);

            this.htmlLogger.TestRunCompleteHandler(new object(), new TestRunCompleteEventArgs(null, false, true, null, null, TimeSpan.Zero));

            Assert.AreEqual(0, this.htmlLogger.TestRunDetails.Summary.TotalTests);
            Assert.AreEqual(0, this.htmlLogger.TestRunDetails.Summary.PassPercentage);
        }

        private static TestCase CreateTestCase(string testCaseName)
        {
            return new TestCase(testCaseName, new Uri("some://uri"), "DummySourceFileName");
        }
    }
}






