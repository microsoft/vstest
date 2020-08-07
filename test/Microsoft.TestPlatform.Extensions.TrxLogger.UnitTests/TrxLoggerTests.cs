// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.TestPlatform.Extensions.TrxLogger.UnitTests
{
    using Microsoft.TestPlatform.Extensions.TrxLogger.Utility;
    using Microsoft.VisualStudio.TestPlatform.Extensions.TrxLogger;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Xml;
    using System.Xml.Linq;
    using VisualStudio.TestPlatform.ObjectModel;
    using VisualStudio.TestPlatform.ObjectModel.Client;
    using VisualStudio.TestPlatform.ObjectModel.Logging;
    using ObjectModel = Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using TrxLoggerConstants = Microsoft.TestPlatform.Extensions.TrxLogger.Utility.Constants;
    using TrxLoggerObjectModel = Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel;
    using TrxLoggerResources = Microsoft.VisualStudio.TestPlatform.Extensions.TrxLogger.Resources.TrxResource;

    [TestClass]
    public class TrxLoggerTests
    {
        private Mock<TestLoggerEvents> events;
        private TestableTrxLogger testableTrxLogger;
        private Dictionary<string, string> parameters;
        private static string DefaultTestRunDirectory = Path.GetTempPath();
        private static string DefaultLogFileNameParameterValue = "logfilevalue.trx";
        private const string DefaultLogFilePrefixParameterValue = "log_prefix";

        private const int MultipleLoggerInstanceCount = 2;

        [TestInitialize]
        public void Initialize()
        {
            this.events = new Mock<TestLoggerEvents>();

            this.testableTrxLogger = new TestableTrxLogger();
            this.parameters = new Dictionary<string, string>(2);
            this.parameters[DefaultLoggerParameterNames.TestRunDirectory] = TrxLoggerTests.DefaultTestRunDirectory;
            this.parameters[TrxLoggerConstants.LogFileNameKey] = TrxLoggerTests.DefaultLogFileNameParameterValue;
            this.testableTrxLogger.Initialize(this.events.Object, this.parameters);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (!string.IsNullOrEmpty(this.testableTrxLogger?.trxFile) && File.Exists(this.testableTrxLogger.trxFile))
            {
                File.Delete(this.testableTrxLogger.trxFile);
            }
        }

        [TestMethod]
        public void InitializeShouldThrowExceptionIfEventsIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () =>
                    {
                        this.testableTrxLogger.Initialize(null, this.parameters);
                    });
        }

        [TestMethod]
        public void InitializeShouldNotThrowExceptionIfEventsIsNotNull()
        {
            var events = new Mock<TestLoggerEvents>();
            this.testableTrxLogger.Initialize(events.Object, this.parameters);
        }

        [TestMethod]
        public void InitializeShouldThrowExceptionIfTestRunDirectoryIsEmptyOrNull()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () =>
                {
                    var events = new Mock<TestLoggerEvents>();
                    this.parameters[DefaultLoggerParameterNames.TestRunDirectory] = null;
                    this.testableTrxLogger.Initialize(events.Object, parameters);
                });
        }

        [TestMethod]
        public void InitializeShouldNotThrowExceptionIfTestRunDirectoryIsNeitherEmptyNorNull()
        {
            var events = new Mock<TestLoggerEvents>();
            this.testableTrxLogger.Initialize(events.Object, this.parameters);
        }

        [TestMethod]
        public void InitializeShouldThrowExceptionIfParametersAreEmpty()
        {
            var events = new Mock<TestLoggerEvents>();
            Assert.ThrowsException<ArgumentException>(() => this.testableTrxLogger.Initialize(events.Object, new Dictionary<string, string>()));
        }

        [TestMethod]
        public void TestMessageHandlerShouldThrowExceptionIfEventArgsIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.testableTrxLogger.TestMessageHandler(new object(), default(TestRunMessageEventArgs));
            });
        }

        [TestMethod]
        public void TestMessageHandlerShouldAddMessageWhenItIsInformation()
        {
            string message = "First message";
            string message2 = "Second message";
            TestRunMessageEventArgs trme = new TestRunMessageEventArgs(TestMessageLevel.Informational, message);
            this.testableTrxLogger.TestMessageHandler(new object(), trme);

            TestRunMessageEventArgs trme2 = new TestRunMessageEventArgs(TestMessageLevel.Informational, message2);
            this.testableTrxLogger.TestMessageHandler(new object(), trme2);

            string expectedMessage = message + Environment.NewLine + message2 + Environment.NewLine;
            Assert.AreEqual(expectedMessage, this.testableTrxLogger.GetRunLevelInformationalMessage());
        }

        [TestMethod]
        public void TestMessageHandlerShouldAddMessageInListIfItIsWarning()
        {
            string message = "The information to test";
            TestRunMessageEventArgs trme = new TestRunMessageEventArgs(TestMessageLevel.Warning, message);
            this.testableTrxLogger.TestMessageHandler(new object(), trme);
            this.testableTrxLogger.TestMessageHandler(new object(), trme);

            Assert.AreEqual(2, this.testableTrxLogger.GetRunLevelErrorsAndWarnings().Count);
        }

        [TestMethod]
        public void TestMessageHandlerShouldAddMessageInListIfItIsError()
        {
            string message = "The information to test";
            TestRunMessageEventArgs trme = new TestRunMessageEventArgs(TestMessageLevel.Error, message);
            this.testableTrxLogger.TestMessageHandler(new object(), trme);

            Assert.AreEqual(1, this.testableTrxLogger.GetRunLevelErrorsAndWarnings().Count);
        }

        [TestMethod]
        public void TestResultHandlerShouldCaptureStartTimeInSummaryWithTimeStampDuringIntialize()
        {
            ObjectModel.TestCase testCase = CreateTestCase("dummy string");
            ObjectModel.TestResult testResult = new ObjectModel.TestResult(testCase);
            Mock<TestResultEventArgs> e = new Mock<TestResultEventArgs>(testResult);

            this.testableTrxLogger.TestResultHandler(new object(), e.Object);

            Assert.AreEqual(this.testableTrxLogger.TestRunStartTime, this.testableTrxLogger.LoggerTestRun.Started);
        }

        [TestMethod]
        public void TestResultHandlerKeepingTheTrackOfPassedAndFailedTests()
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

            this.testableTrxLogger.TestResultHandler(new object(), pass1.Object);
            this.testableTrxLogger.TestResultHandler(new object(), pass2.Object);
            this.testableTrxLogger.TestResultHandler(new object(), fail1.Object);
            this.testableTrxLogger.TestResultHandler(new object(), skip1.Object);

            Assert.AreEqual(2, this.testableTrxLogger.PassedTestCount, "Passed Tests");
            Assert.AreEqual(1, this.testableTrxLogger.FailedTestCount, "Failed Tests");
        }

        [TestMethod]
        public void TestResultHandlerKeepingTheTrackOfTotalTests()
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

            this.testableTrxLogger.TestResultHandler(new object(), pass1.Object);
            this.testableTrxLogger.TestResultHandler(new object(), pass2.Object);
            this.testableTrxLogger.TestResultHandler(new object(), fail1.Object);
            this.testableTrxLogger.TestResultHandler(new object(), skip1.Object);

            Assert.AreEqual(4, this.testableTrxLogger.TotalTestCount, "Passed Tests");
        }

        [TestMethod]
        public void TestResultHandlerLockingAMessageForSkipTest()
        {
            ObjectModel.TestCase skipTestCase1 = CreateTestCase("Skip1");

            ObjectModel.TestResult skipResult1 = new ObjectModel.TestResult(skipTestCase1);
            skipResult1.Outcome = ObjectModel.TestOutcome.Skipped;

            Mock<TestResultEventArgs> skip1 = new Mock<TestResultEventArgs>(skipResult1);

            this.testableTrxLogger.TestResultHandler(new object(), skip1.Object);

            string expectedMessage = String.Format(CultureInfo.CurrentCulture, TrxLoggerResources.MessageForSkippedTests, "Skip1");

            Assert.AreEqual(expectedMessage + Environment.NewLine, this.testableTrxLogger.GetRunLevelInformationalMessage());
        }

        [TestMethod]
        public void TestResultHandlerShouldCreateOneTestResultForEachTestCase()
        {
            var testCase1 = CreateTestCase("testCase1");
            ObjectModel.TestCase testCase2 = CreateTestCase("testCase2");

            ObjectModel.TestResult result1 = new ObjectModel.TestResult(testCase1);
            result1.Outcome = ObjectModel.TestOutcome.Skipped;

            ObjectModel.TestResult result2 = new ObjectModel.TestResult(testCase2);
            result2.Outcome = ObjectModel.TestOutcome.Failed;

            Mock<TestResultEventArgs> resultEventArg1 = new Mock<TestResultEventArgs>(result1);
            Mock<TestResultEventArgs> resultEventArg2 = new Mock<TestResultEventArgs>(result2);

            this.testableTrxLogger.TestResultHandler(new object(), resultEventArg1.Object);
            this.testableTrxLogger.TestResultHandler(new object(), resultEventArg2.Object);

            Assert.AreEqual(2, this.testableTrxLogger.TestResultCount, "TestResultHandler is not creating test result entry for each test case");
        }

        [TestMethod]
        public void TestResultHandlerShouldCreateOneTestEntryForEachTestCase()
        {
            ObjectModel.TestCase testCase1 = CreateTestCase("TestCase1");
            ObjectModel.TestCase testCase2 = CreateTestCase("TestCase2");

            ObjectModel.TestResult result1 = new ObjectModel.TestResult(testCase1);
            result1.Outcome = ObjectModel.TestOutcome.Skipped;

            ObjectModel.TestResult result2 = new ObjectModel.TestResult(testCase2);
            result2.Outcome = ObjectModel.TestOutcome.Passed;

            Mock<TestResultEventArgs> resultEventArg1 = new Mock<TestResultEventArgs>(result1);
            Mock<TestResultEventArgs> resultEventArg2 = new Mock<TestResultEventArgs>(result2);

            this.testableTrxLogger.TestResultHandler(new object(), resultEventArg1.Object);
            this.testableTrxLogger.TestResultHandler(new object(), resultEventArg2.Object);

            Assert.AreEqual(2, this.testableTrxLogger.TestEntryCount, "TestResultHandler is not creating test result entry for each test case");
        }

        [TestMethod]
        public void TestResultHandlerShouldCreateOneUnitTestElementForEachTestCase()
        {
            ObjectModel.TestCase testCase1 = CreateTestCase("TestCase1");
            ObjectModel.TestCase testCase2 = CreateTestCase("TestCase2");

            ObjectModel.TestResult result1 = new ObjectModel.TestResult(testCase1);

            ObjectModel.TestResult result2 = new ObjectModel.TestResult(testCase2);
            result2.Outcome = ObjectModel.TestOutcome.Failed;

            Mock<TestResultEventArgs> resultEventArg1 = new Mock<TestResultEventArgs>(result1);
            Mock<TestResultEventArgs> resultEventArg2 = new Mock<TestResultEventArgs>(result2);

            this.testableTrxLogger.TestResultHandler(new object(), resultEventArg1.Object);
            this.testableTrxLogger.TestResultHandler(new object(), resultEventArg2.Object);

            Assert.AreEqual(2, this.testableTrxLogger.UnitTestElementCount, "TestResultHandler is not creating test result entry for each test case");
        }

        [TestMethod]
        public void TestResultHandlerShouldAddFlatResultsIfParentTestResultIsNotPresent()
        {
            ObjectModel.TestCase testCase1 = CreateTestCase("TestCase1");

            Guid parentExecutionId = Guid.NewGuid();

            ObjectModel.TestResult result1 = new ObjectModel.TestResult(testCase1);
            result1.SetPropertyValue<Guid>(TrxLoggerConstants.ExecutionIdProperty, Guid.NewGuid());
            result1.SetPropertyValue<Guid>(TrxLoggerConstants.ParentExecIdProperty, parentExecutionId);

            ObjectModel.TestResult result2 = new ObjectModel.TestResult(testCase1);
            result2.Outcome = ObjectModel.TestOutcome.Failed;
            result2.SetPropertyValue<Guid>(TrxLoggerConstants.ExecutionIdProperty, Guid.NewGuid());
            result2.SetPropertyValue<Guid>(TrxLoggerConstants.ParentExecIdProperty, parentExecutionId);

            Mock<TestResultEventArgs> resultEventArg1 = new Mock<TestResultEventArgs>(result1);
            Mock<TestResultEventArgs> resultEventArg2 = new Mock<TestResultEventArgs>(result2);

            this.testableTrxLogger.TestResultHandler(new object(), resultEventArg1.Object);
            this.testableTrxLogger.TestResultHandler(new object(), resultEventArg2.Object);

            Assert.AreEqual(2, this.testableTrxLogger.TestResultCount, "TestResultHandler is not creating flat results when parent result is not present.");
        }

        [TestMethod]
        public void TestResultHandlerShouldChangeGuidAndDisplayNameForMsTestResultIfParentNotPresentButTestResultNamePresent()
        {
            this.ValidateTestIdAndNameInTrx(true);
        }

        [TestMethod]
        public void TestResultHandlerShouldNotChangeGuidAndDisplayNameForNonMsTestResultIfParentNotPresentButTestResultNamePresent()
        {
            this.ValidateTestIdAndNameInTrx(false);
        }

        [TestMethod]
        public void TestResultHandlerShouldAddHierarchicalResultsIfParentTestResultIsPresent()
        {
            ObjectModel.TestCase testCase1 = CreateTestCase("TestCase1");

            Guid parentExecutionId = Guid.NewGuid();

            ObjectModel.TestResult result1 = new ObjectModel.TestResult(testCase1);
            result1.SetPropertyValue<Guid>(TrxLoggerConstants.ExecutionIdProperty, parentExecutionId);

            ObjectModel.TestResult result2 = new ObjectModel.TestResult(testCase1);
            result2.SetPropertyValue<Guid>(TrxLoggerConstants.ExecutionIdProperty, Guid.NewGuid());
            result2.SetPropertyValue<Guid>(TrxLoggerConstants.ParentExecIdProperty, parentExecutionId);

            ObjectModel.TestResult result3 = new ObjectModel.TestResult(testCase1);
            result3.Outcome = ObjectModel.TestOutcome.Failed;
            result3.SetPropertyValue<Guid>(TrxLoggerConstants.ExecutionIdProperty, Guid.NewGuid());
            result3.SetPropertyValue<Guid>(TrxLoggerConstants.ParentExecIdProperty, parentExecutionId);

            Mock<TestResultEventArgs> resultEventArg1 = new Mock<TestResultEventArgs>(result1);
            Mock<TestResultEventArgs> resultEventArg2 = new Mock<TestResultEventArgs>(result2);
            Mock<TestResultEventArgs> resultEventArg3 = new Mock<TestResultEventArgs>(result3);

            this.testableTrxLogger.TestResultHandler(new object(), resultEventArg1.Object);
            this.testableTrxLogger.TestResultHandler(new object(), resultEventArg2.Object);
            this.testableTrxLogger.TestResultHandler(new object(), resultEventArg3.Object);

            Assert.AreEqual(1, this.testableTrxLogger.TestResultCount, "TestResultHandler is not creating hierarchical results when parent result is present.");
            Assert.AreEqual(3, this.testableTrxLogger.TotalTestCount, "TestResultHandler is not adding all inner results in parent test result.");
        }

        [TestMethod]
        public void TestResultHandlerShouldAddSingleTestElementForDataDrivenTests()
        {
            ObjectModel.TestCase testCase1 = CreateTestCase("TestCase1");

            Guid parentExecutionId = Guid.NewGuid();

            ObjectModel.TestResult result1 = new ObjectModel.TestResult(testCase1);
            result1.SetPropertyValue<Guid>(TrxLoggerConstants.ExecutionIdProperty, parentExecutionId);

            ObjectModel.TestResult result2 = new ObjectModel.TestResult(testCase1);
            result2.SetPropertyValue<Guid>(TrxLoggerConstants.ExecutionIdProperty, Guid.NewGuid());
            result2.SetPropertyValue<Guid>(TrxLoggerConstants.ParentExecIdProperty, parentExecutionId);

            ObjectModel.TestResult result3 = new ObjectModel.TestResult(testCase1);
            result3.Outcome = ObjectModel.TestOutcome.Failed;
            result3.SetPropertyValue<Guid>(TrxLoggerConstants.ExecutionIdProperty, Guid.NewGuid());
            result3.SetPropertyValue<Guid>(TrxLoggerConstants.ParentExecIdProperty, parentExecutionId);

            Mock<TestResultEventArgs> resultEventArg1 = new Mock<TestResultEventArgs>(result1);
            Mock<TestResultEventArgs> resultEventArg2 = new Mock<TestResultEventArgs>(result2);
            Mock<TestResultEventArgs> resultEventArg3 = new Mock<TestResultEventArgs>(result3);

            this.testableTrxLogger.TestResultHandler(new object(), resultEventArg1.Object);
            this.testableTrxLogger.TestResultHandler(new object(), resultEventArg2.Object);
            this.testableTrxLogger.TestResultHandler(new object(), resultEventArg3.Object);

            Assert.AreEqual(1, this.testableTrxLogger.UnitTestElementCount, "TestResultHandler is adding multiple test elements for data driven tests.");
        }

        [TestMethod]
        public void TestResultHandlerShouldAddSingleTestEntryForDataDrivenTests()
        {
            ObjectModel.TestCase testCase1 = CreateTestCase("TestCase1");

            Guid parentExecutionId = Guid.NewGuid();

            ObjectModel.TestResult result1 = new ObjectModel.TestResult(testCase1);
            result1.SetPropertyValue<Guid>(TrxLoggerConstants.ExecutionIdProperty, parentExecutionId);

            ObjectModel.TestResult result2 = new ObjectModel.TestResult(testCase1);
            result2.SetPropertyValue<Guid>(TrxLoggerConstants.ExecutionIdProperty, Guid.NewGuid());
            result2.SetPropertyValue<Guid>(TrxLoggerConstants.ParentExecIdProperty, parentExecutionId);

            ObjectModel.TestResult result3 = new ObjectModel.TestResult(testCase1);
            result3.Outcome = ObjectModel.TestOutcome.Failed;
            result3.SetPropertyValue<Guid>(TrxLoggerConstants.ExecutionIdProperty, Guid.NewGuid());
            result3.SetPropertyValue<Guid>(TrxLoggerConstants.ParentExecIdProperty, parentExecutionId);

            Mock<TestResultEventArgs> resultEventArg1 = new Mock<TestResultEventArgs>(result1);
            Mock<TestResultEventArgs> resultEventArg2 = new Mock<TestResultEventArgs>(result2);
            Mock<TestResultEventArgs> resultEventArg3 = new Mock<TestResultEventArgs>(result3);

            this.testableTrxLogger.TestResultHandler(new object(), resultEventArg1.Object);
            this.testableTrxLogger.TestResultHandler(new object(), resultEventArg2.Object);
            this.testableTrxLogger.TestResultHandler(new object(), resultEventArg3.Object);

            Assert.AreEqual(1, this.testableTrxLogger.TestEntryCount, "TestResultHandler is adding multiple test entries for data driven tests.");
        }

        [TestMethod]
        public void TestResultHandlerShouldAddHierarchicalResultsForOrderedTest()
        {
            ObjectModel.TestCase testCase1 = CreateTestCase("TestCase1");
            ObjectModel.TestCase testCase2 = CreateTestCase("TestCase2");
            ObjectModel.TestCase testCase3 = CreateTestCase("TestCase3");

            Guid parentExecutionId = Guid.NewGuid();

            ObjectModel.TestResult result1 = new ObjectModel.TestResult(testCase1);
            result1.SetPropertyValue<Guid>(TrxLoggerConstants.ExecutionIdProperty, parentExecutionId);
            result1.SetPropertyValue<Guid>(TrxLoggerConstants.TestTypeProperty, TrxLoggerConstants.OrderedTestTypeGuid);

            ObjectModel.TestResult result2 = new ObjectModel.TestResult(testCase2);
            result2.SetPropertyValue<Guid>(TrxLoggerConstants.ExecutionIdProperty, Guid.NewGuid());
            result2.SetPropertyValue<Guid>(TrxLoggerConstants.ParentExecIdProperty, parentExecutionId);

            ObjectModel.TestResult result3 = new ObjectModel.TestResult(testCase3);
            result3.Outcome = ObjectModel.TestOutcome.Failed;
            result3.SetPropertyValue<Guid>(TrxLoggerConstants.ExecutionIdProperty, Guid.NewGuid());
            result3.SetPropertyValue<Guid>(TrxLoggerConstants.ParentExecIdProperty, parentExecutionId);

            Mock<TestResultEventArgs> resultEventArg1 = new Mock<TestResultEventArgs>(result1);
            Mock<TestResultEventArgs> resultEventArg2 = new Mock<TestResultEventArgs>(result2);
            Mock<TestResultEventArgs> resultEventArg3 = new Mock<TestResultEventArgs>(result3);

            this.testableTrxLogger.TestResultHandler(new object(), resultEventArg1.Object);
            this.testableTrxLogger.TestResultHandler(new object(), resultEventArg2.Object);
            this.testableTrxLogger.TestResultHandler(new object(), resultEventArg3.Object);

            Assert.AreEqual(1, this.testableTrxLogger.TestResultCount, "TestResultHandler is not creating hierarchical results for ordered test.");
            Assert.AreEqual(3, this.testableTrxLogger.TotalTestCount, "TestResultHandler is not adding all inner results in ordered test.");
        }

        [TestMethod]
        public void TestResultHandlerShouldAddMultipleTestElementsForOrderedTest()
        {
            ObjectModel.TestCase testCase1 = CreateTestCase("TestCase1");
            ObjectModel.TestCase testCase2 = CreateTestCase("TestCase2");
            ObjectModel.TestCase testCase3 = CreateTestCase("TestCase3");

            Guid parentExecutionId = Guid.NewGuid();

            ObjectModel.TestResult result1 = new ObjectModel.TestResult(testCase1);
            result1.SetPropertyValue<Guid>(TrxLoggerConstants.ExecutionIdProperty, parentExecutionId);
            result1.SetPropertyValue<Guid>(TrxLoggerConstants.TestTypeProperty, TrxLoggerConstants.OrderedTestTypeGuid);

            ObjectModel.TestResult result2 = new ObjectModel.TestResult(testCase2);
            result2.SetPropertyValue<Guid>(TrxLoggerConstants.ExecutionIdProperty, Guid.NewGuid());
            result2.SetPropertyValue<Guid>(TrxLoggerConstants.ParentExecIdProperty, parentExecutionId);

            ObjectModel.TestResult result3 = new ObjectModel.TestResult(testCase3);
            result3.Outcome = ObjectModel.TestOutcome.Failed;
            result3.SetPropertyValue<Guid>(TrxLoggerConstants.ExecutionIdProperty, Guid.NewGuid());
            result3.SetPropertyValue<Guid>(TrxLoggerConstants.ParentExecIdProperty, parentExecutionId);

            Mock<TestResultEventArgs> resultEventArg1 = new Mock<TestResultEventArgs>(result1);
            Mock<TestResultEventArgs> resultEventArg2 = new Mock<TestResultEventArgs>(result2);
            Mock<TestResultEventArgs> resultEventArg3 = new Mock<TestResultEventArgs>(result3);

            this.testableTrxLogger.TestResultHandler(new object(), resultEventArg1.Object);
            this.testableTrxLogger.TestResultHandler(new object(), resultEventArg2.Object);
            this.testableTrxLogger.TestResultHandler(new object(), resultEventArg3.Object);

            Assert.AreEqual(3, this.testableTrxLogger.UnitTestElementCount, "TestResultHandler is not adding multiple test elements for ordered test.");
        }

        [TestMethod]
        public void TestResultHandlerShouldAddSingleTestEntryForOrderedTest()
        {
            ObjectModel.TestCase testCase1 = CreateTestCase("TestCase1");
            ObjectModel.TestCase testCase2 = CreateTestCase("TestCase2");
            ObjectModel.TestCase testCase3 = CreateTestCase("TestCase3");

            Guid parentExecutionId = Guid.NewGuid();

            ObjectModel.TestResult result1 = new ObjectModel.TestResult(testCase1);
            result1.SetPropertyValue<Guid>(TrxLoggerConstants.ExecutionIdProperty, parentExecutionId);
            result1.SetPropertyValue<Guid>(TrxLoggerConstants.TestTypeProperty, TrxLoggerConstants.OrderedTestTypeGuid);

            ObjectModel.TestResult result2 = new ObjectModel.TestResult(testCase2);
            result2.SetPropertyValue<Guid>(TrxLoggerConstants.ExecutionIdProperty, Guid.NewGuid());
            result2.SetPropertyValue<Guid>(TrxLoggerConstants.ParentExecIdProperty, parentExecutionId);

            ObjectModel.TestResult result3 = new ObjectModel.TestResult(testCase3);
            result3.Outcome = ObjectModel.TestOutcome.Failed;
            result3.SetPropertyValue<Guid>(TrxLoggerConstants.ExecutionIdProperty, Guid.NewGuid());
            result3.SetPropertyValue<Guid>(TrxLoggerConstants.ParentExecIdProperty, parentExecutionId);

            Mock<TestResultEventArgs> resultEventArg1 = new Mock<TestResultEventArgs>(result1);
            Mock<TestResultEventArgs> resultEventArg2 = new Mock<TestResultEventArgs>(result2);
            Mock<TestResultEventArgs> resultEventArg3 = new Mock<TestResultEventArgs>(result3);

            this.testableTrxLogger.TestResultHandler(new object(), resultEventArg1.Object);
            this.testableTrxLogger.TestResultHandler(new object(), resultEventArg2.Object);
            this.testableTrxLogger.TestResultHandler(new object(), resultEventArg3.Object);

            Assert.AreEqual(1, this.testableTrxLogger.TestEntryCount, "TestResultHandler is adding multiple test entries for ordered test.");
        }

        [TestMethod]
        public void TestRunCompleteHandlerShouldReportFailedOutcomeIfTestRunIsAborted()
        {
            string message = "The information to test";
            TestRunMessageEventArgs trme = new TestRunMessageEventArgs(TestMessageLevel.Error, message);
            this.testableTrxLogger.TestMessageHandler(new object(), trme);

            this.testableTrxLogger.TestRunCompleteHandler(new object(), new TestRunCompleteEventArgs(null, false, true, null, null, TimeSpan.Zero));

            Assert.AreEqual(this.testableTrxLogger.TestResultOutcome, TrxLoggerObjectModel.TestOutcome.Failed);
        }

        [TestMethod]
        public void OutcomeOfRunWillBeFailIfAnyTestsFails()
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

            this.testableTrxLogger.TestResultHandler(new object(), pass1.Object);
            this.testableTrxLogger.TestResultHandler(new object(), pass2.Object);
            this.testableTrxLogger.TestResultHandler(new object(), fail1.Object);
            this.testableTrxLogger.TestResultHandler(new object(), skip1.Object);

            var testRunCompleteEventArgs = CreateTestRunCompleteEventArgs();

            this.testableTrxLogger.TestRunCompleteHandler(new object(), testRunCompleteEventArgs);

            Assert.AreEqual(TrxLoggerObjectModel.TestOutcome.Failed, this.testableTrxLogger.TestResultOutcome);
        }

        [TestMethod]
        public void OutcomeOfRunWillBeCompletedIfNoTestsFails()
        {
            ObjectModel.TestCase passTestCase1 = CreateTestCase("Pass1");
            ObjectModel.TestCase passTestCase2 = CreateTestCase("Pass2");
            ObjectModel.TestCase skipTestCase1 = CreateTestCase("Skip1");

            ObjectModel.TestResult passResult1 = new ObjectModel.TestResult(passTestCase1);
            passResult1.Outcome = ObjectModel.TestOutcome.Passed;

            ObjectModel.TestResult passResult2 = new ObjectModel.TestResult(passTestCase2);
            passResult2.Outcome = ObjectModel.TestOutcome.Passed;

            ObjectModel.TestResult skipResult1 = new ObjectModel.TestResult(skipTestCase1);
            skipResult1.Outcome = ObjectModel.TestOutcome.Skipped;

            Mock<TestResultEventArgs> pass1 = new Mock<TestResultEventArgs>(passResult1);
            Mock<TestResultEventArgs> pass2 = new Mock<TestResultEventArgs>(passResult2);
            Mock<TestResultEventArgs> skip1 = new Mock<TestResultEventArgs>(skipResult1);

            this.testableTrxLogger.TestResultHandler(new object(), pass1.Object);
            this.testableTrxLogger.TestResultHandler(new object(), pass2.Object);
            this.testableTrxLogger.TestResultHandler(new object(), skip1.Object);

            var testRunCompleteEventArgs = CreateTestRunCompleteEventArgs();

            this.testableTrxLogger.TestRunCompleteHandler(new object(), testRunCompleteEventArgs);

            Assert.AreEqual(TrxLoggerObjectModel.TestOutcome.Completed, this.testableTrxLogger.TestResultOutcome);
        }

        [TestMethod]
        public void TheDefaultTrxFileNameShouldNotHaveWhiteSpace()
        {
            // To create default trx file, log file parameter should be null.
            this.parameters[TrxLoggerConstants.LogFileNameKey] = null;
            this.testableTrxLogger.Initialize(this.events.Object, this.parameters);

            this.MakeTestRunComplete();

            bool trxFileNameContainsWhiteSpace = Path.GetFileName(this.testableTrxLogger.trxFile).Contains(' ');
            Assert.IsFalse(trxFileNameContainsWhiteSpace, $"\"{this.testableTrxLogger.trxFile}\": Trx file name should not have white spaces");
        }

        [TestMethod]
        public void DefaultTrxFileShouldCreateIfLogFileNameParameterNotPassed()
        {
            // To create default trx file, If LogFileName parameter not passed
            this.parameters.Remove(TrxLoggerConstants.LogFileNameKey);
            this.testableTrxLogger.Initialize(this.events.Object, this.parameters);

            this.MakeTestRunComplete();

            Assert.IsFalse(string.IsNullOrWhiteSpace(this.testableTrxLogger.trxFile));
        }

        [TestMethod]
        public void DefaultTrxFileNameVerification()
        {
            this.parameters.Remove(TrxLoggerConstants.LogFileNameKey);
            this.parameters[TrxLoggerConstants.LogFilePrefixKey] = DefaultLogFilePrefixParameterValue;

            var time = DateTime.Now;
            var trxFileHelper = new TrxFileHelper(() => time);

            testableTrxLogger = new TestableTrxLogger(new FileHelper(), trxFileHelper);
            testableTrxLogger.Initialize(this.events.Object, this.parameters);

            MakeTestRunComplete();

            var fileName = Path.GetFileName(testableTrxLogger.trxFile);
            var expectedName = $"{DefaultLogFilePrefixParameterValue}{time:_yyyyMMddHHmmss}.trx";

            Assert.AreEqual(expectedName, fileName, "Trx file name pattern has changed. It should be in the form of prefix_yyyyMMddHHmmss.trx, Azure Devops VSTest task depends on this naming.");
        }

        [TestMethod]
        public void DefaultTrxFileShouldIterateIfLogFileNameParameterNotPassed()
        {
            this.parameters.Remove(TrxLoggerConstants.LogFileNameKey);

            var files = TestMultipleTrxLoggers();

            Assert.AreEqual(MultipleLoggerInstanceCount, files.Length, "All logger instances should get different file names!");
        }

        [TestMethod]
        public void TrxFileNameShouldNotIterate()
        {
            var files = TestMultipleTrxLoggers();

            Assert.AreEqual(1, files.Length, "All logger instances should get the same file name!");
        }

        [TestMethod]
        public void TrxPrefixFileNameShouldIterate()
        {
            this.parameters.Remove(TrxLoggerConstants.LogFileNameKey);
            this.parameters[TrxLoggerConstants.LogFilePrefixKey] = DefaultLogFilePrefixParameterValue;

            var files = TestMultipleTrxLoggers();

            Assert.AreEqual(MultipleLoggerInstanceCount, files.Length, "All logger instances should get different file names!");
        }

        private string[] TestMultipleTrxLoggers()
        {
            var files = new string[2];

            try
            {
                var time = new DateTime(2020, 1, 1, 0, 0, 0);

                var trxFileHelper = new TrxFileHelper(() => time);
                var trxLogger1 = new TestableTrxLogger(new FileHelper(), trxFileHelper);
                var trxLogger2 = new TestableTrxLogger(new FileHelper(), trxFileHelper);

                trxLogger1.Initialize(this.events.Object, this.parameters);
                trxLogger2.Initialize(this.events.Object, this.parameters);

                MakeTestRunComplete(trxLogger1);
                files[0] = trxLogger1.trxFile;

                MakeTestRunComplete(trxLogger2);
                files[1] = trxLogger2.trxFile;
            }
            finally
            {
                files = files
                    .Where(i => !string.IsNullOrWhiteSpace(i))
                    .Distinct()
                    .ToArray();

                foreach (var file in files)
                {
                    if (!string.IsNullOrEmpty(file) && File.Exists(file))
                    {
                        File.Delete(file);
                    }
                }
            }

            return files;
        }

        [TestMethod]
        public void CustomTrxFileNameShouldConstructFromLogFileParameter()
        {
            this.MakeTestRunComplete();

            Assert.AreEqual(Path.Combine(TrxLoggerTests.DefaultTestRunDirectory, TrxLoggerTests.DefaultLogFileNameParameterValue), this.testableTrxLogger.trxFile, "Wrong Trx file name");
        }



        /// <summary>
        /// Unit test for reading TestCategories from the TestCase which is part of test result.
        /// </summary>
        [TestMethod]
        public void GetCustomPropertyValueFromTestCaseShouldReadCategoyrAttributesFromTestCase()
        {
            ObjectModel.TestCase testCase1 = CreateTestCase("TestCase1");
            TestProperty testProperty = TestProperty.Register("MSTestDiscoverer.TestCategory", "String array property", string.Empty, string.Empty, typeof(string[]), null, TestPropertyAttributes.Hidden, typeof(TestObject));

            testCase1.SetPropertyValue(testProperty, new[] { "ClassLevel", "AsmLevel" });

            var converter = new Converter(new Mock<IFileHelper>().Object, new TrxFileHelper());
            List<String> listCategoriesActual = converter.GetCustomPropertyValueFromTestCase(testCase1, "MSTestDiscoverer.TestCategory");

            List<String> listCategoriesExpected = new List<string>();
            listCategoriesExpected.Add("ClassLevel");
            listCategoriesExpected.Add("AsmLevel");

            CollectionAssert.AreEqual(listCategoriesExpected, listCategoriesActual);
        }

        [TestMethod]
        public void CRLFCharactersShouldGetRetainedInTrx()
        {
            // To create default trx file, If LogFileName parameter not passed
            this.parameters.Remove(TrxLoggerConstants.LogFileNameKey);
            this.testableTrxLogger.Initialize(this.events.Object, this.parameters);

            string message = $"one line{ Environment.NewLine }second line\r\nthird line";
            var pass = TrxLoggerTests.CreatePassTestResultEventArgsMock("Pass1", new List<TestResultMessage> { new TestResultMessage(TestResultMessage.StandardOutCategory, message) });

            this.testableTrxLogger.TestResultHandler(new object(), pass.Object);

            var testRunCompleteEventArgs = TrxLoggerTests.CreateTestRunCompleteEventArgs();
            this.testableTrxLogger.TestRunCompleteHandler(new object(), testRunCompleteEventArgs);

            Assert.IsTrue(File.Exists(this.testableTrxLogger.trxFile), string.Format("TRX file: {0}, should have got created.", this.testableTrxLogger.trxFile));

            string actualMessage = GetElementValueFromTrx(this.testableTrxLogger.trxFile, "StdOut");

            Assert.IsNotNull(actualMessage);
            Assert.IsTrue(string.Equals(message, actualMessage), string.Format("StdOut messages do not match. Expected:{0}, Actual:{1}", message, actualMessage));
        }

        [TestMethod]
        public void TestRunInformationShouldContainUtcDateTime()
        {
            this.MakeTestRunComplete();
            this.ValidateDateTimeInTrx(this.testableTrxLogger.trxFile);
        }

        private void ValidateDateTimeInTrx(string trxFileName)
        {
            using (FileStream file = File.OpenRead(trxFileName))
            {
                using (XmlReader reader = XmlReader.Create(file))
                {
                    XDocument document = XDocument.Load(reader);
                    var timesNode = document.Descendants(document.Root.GetDefaultNamespace() + "Times").FirstOrDefault();
                    ValidateTimeWithinUtcLimits(DateTimeOffset.Parse(timesNode.Attributes("creation").FirstOrDefault().Value));
                    ValidateTimeWithinUtcLimits(DateTimeOffset.Parse(timesNode.Attributes("start").FirstOrDefault().Value));
                    var resultNode = document.Descendants(document.Root.GetDefaultNamespace() + "UnitTestResult").FirstOrDefault();
                    ValidateTimeWithinUtcLimits(DateTimeOffset.Parse(resultNode.Attributes("endTime").FirstOrDefault().Value));
                    ValidateTimeWithinUtcLimits(DateTimeOffset.Parse(resultNode.Attributes("startTime").FirstOrDefault().Value));
                }
            }
        }

        [TestMethod]
        [DataRow("results")]
        public void CustomTrxFileNameShouldBeConstructedFromRelativeLogFilePrefixParameter(string prefixName)
        {
            this.parameters.Remove(TrxLoggerConstants.LogFileNameKey);
            this.parameters[TrxLoggerConstants.LogFilePrefixKey] = prefixName;
            this.parameters[DefaultLoggerParameterNames.TargetFramework] = ".NETFramework,Version=4.5.1";
            this.testableTrxLogger.Initialize(events.Object, this.parameters);

            this.MakeTestRunComplete();

            string actualFileNameWithoutTimestamp = this.testableTrxLogger.trxFile.Substring(0, this.testableTrxLogger.trxFile.LastIndexOf('_'));

            Assert.AreNotEqual(Path.Combine(TrxLoggerTests.DefaultTestRunDirectory, "results.trx"), this.testableTrxLogger.trxFile, "Expected framework name to appear in file name");
            Assert.AreNotEqual(Path.Combine(TrxLoggerTests.DefaultTestRunDirectory, "results_net451.trx"), this.testableTrxLogger.trxFile, "Expected time stamp to appear in file name");
            Assert.AreEqual(Path.Combine(TrxLoggerTests.DefaultTestRunDirectory, "results_net451"), actualFileNameWithoutTimestamp);
        }

        [TestMethod]
        public void CustomTrxFileNameShouldBeConstructedFromAbsoluteLogFilePrefixParameter()
        {
            this.parameters.Remove(TrxLoggerConstants.LogFileNameKey);
            var trxPrefix = Path.Combine(Path.GetTempPath(), "results");
            this.parameters[TrxLoggerConstants.LogFilePrefixKey] = trxPrefix;
            this.parameters[DefaultLoggerParameterNames.TargetFramework] = ".NETFramework,Version=4.5.1";
            this.testableTrxLogger.Initialize(events.Object, this.parameters);

            this.MakeTestRunComplete();

            string actualFileNameWithoutTimestamp = this.testableTrxLogger.trxFile.Substring(0, this.testableTrxLogger.trxFile.LastIndexOf('_'));

            Assert.AreEqual(trxPrefix + "_net451", actualFileNameWithoutTimestamp);

            File.Delete(this.testableTrxLogger.trxFile);
        }

        [TestMethod]
        public void IntializeShouldThrowExceptionIfBothPrefixAndNameProvided()
        {
            this.parameters[TrxLoggerConstants.LogFileNameKey] = "results.trx";
            var trxPrefix = Path.Combine(Path.GetTempPath(), "results");
            this.parameters[TrxLoggerConstants.LogFilePrefixKey] = trxPrefix;
            this.parameters[DefaultLoggerParameterNames.TargetFramework] = ".NETFramework,Version=4.5.1";

            Assert.ThrowsException<ArgumentException>(() => this.testableTrxLogger.Initialize(events.Object, this.parameters));
        }

        private void ValidateTestIdAndNameInTrx(bool isMstestAdapter)
        {
            ObjectModel.TestCase testCase = CreateTestCase("TestCase");
            testCase.ExecutorUri = isMstestAdapter ? new Uri("some://mstestadapteruri") : new Uri("some://uri");

            ObjectModel.TestResult result = new ObjectModel.TestResult(testCase);
            result.SetPropertyValue<Guid>(TrxLoggerConstants.ExecutionIdProperty, Guid.NewGuid());
            if (isMstestAdapter)
            {
                result.DisplayName = "testDisplayName";
            }

            Mock<TestResultEventArgs> resultEventArg = new Mock<TestResultEventArgs>(result);
            this.testableTrxLogger.TestResultHandler(new object(), resultEventArg.Object);
            var testRunCompleteEventArgs = TrxLoggerTests.CreateTestRunCompleteEventArgs();
            this.testableTrxLogger.TestRunCompleteHandler(new object(), testRunCompleteEventArgs);

            this.ValidateResultAttributesInTrx(this.testableTrxLogger.trxFile, testCase.Id, testCase.DisplayName, isMstestAdapter);
        }

        private void ValidateResultAttributesInTrx(string trxFileName, Guid testId, string testName, bool isMstestAdapter)
        {
            using (FileStream file = File.OpenRead(trxFileName))
            {
                using (XmlReader reader = XmlReader.Create(file))
                {
                    XDocument document = XDocument.Load(reader);
                    var resultNode = document.Descendants(document.Root.GetDefaultNamespace() + "UnitTestResult").FirstOrDefault();
                    if (isMstestAdapter)
                    {
                        Assert.AreNotEqual(resultNode.Attributes("testId").FirstOrDefault().Value, testId.ToString());
                        Assert.AreNotEqual(resultNode.Attributes("testName").FirstOrDefault().Value, testName);
                    }
                    else
                    {
                        Assert.AreEqual(resultNode.Attributes("testId").FirstOrDefault().Value, testId.ToString());
                        Assert.AreEqual(resultNode.Attributes("testName").FirstOrDefault().Value, testName);
                    }
                }
            }
        }

        private void ValidateTimeWithinUtcLimits(DateTimeOffset dateTime)
        {
            Assert.IsTrue(dateTime.UtcDateTime.Subtract(DateTime.UtcNow) < new TimeSpan(0, 0, 0, 60));
        }

        private string GetElementValueFromTrx(string trxFileName, string fieldName)
        {
            using (FileStream file = File.OpenRead(trxFileName))
            using (XmlReader reader = XmlReader.Create(file))
            {
                while (reader.Read())
                {
                    if (reader.Name.Equals(fieldName) && reader.NodeType == XmlNodeType.Element)
                    {
                        return reader.ReadElementContentAsString();
                    }
                }
            }

            return null;
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

        private void MakeTestRunComplete() => this.MakeTestRunComplete(this.testableTrxLogger);

        private void MakeTestRunComplete(TestableTrxLogger testableTrxLogger)
        {
            var pass = TrxLoggerTests.CreatePassTestResultEventArgsMock();
            testableTrxLogger.TestResultHandler(new object(), pass.Object);
            var testRunCompleteEventArgs = TrxLoggerTests.CreateTestRunCompleteEventArgs();
            testableTrxLogger.TestRunCompleteHandler(new object(), testRunCompleteEventArgs);
        }
    }

    internal class TestableTrxLogger : TrxLogger
    {
        public TestableTrxLogger() : base() { }
        public TestableTrxLogger(IFileHelper fileHelper, TrxFileHelper trxFileHelper) : base(fileHelper, trxFileHelper) { }

        public string trxFile;
        internal override void PopulateTrxFile(string trxFileName, XmlElement rootElement)
        {
            this.trxFile = trxFileName;
            base.PopulateTrxFile(trxFile, rootElement);
        }
    }
}
