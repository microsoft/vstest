// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.Extensions.TrxLogger;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Utility;

    using VisualStudio.TestPlatform.ObjectModel;
    using VisualStudio.TestPlatform.ObjectModel.Client;
    using VisualStudio.TestPlatform.ObjectModel.Logging;

    using ObjectModel = Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using TrxLoggerObjectModel = Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel;
    using TrxLoggerResources = Microsoft.VisualStudio.TestPlatform.Extensions.TrxLogger.Resources.TrxResource;

    [TestClass]
    public class TrxLoggerTests
    {
        private Mock<TestLoggerEvents> events;
        private TestableTrxLogger testableTrxLogger;
        private Dictionary<string, string> parameters;
        private static string DefaultTestRunDirectory = AppContext.BaseDirectory;
        private static string DefaultLogFileParameterValue = Path.Combine(DefaultTestRunDirectory, "logfilevalue.trx");

        [TestInitialize]
        public void Initialize()
        {
            this.events = new Mock<TestLoggerEvents>();

            this.testableTrxLogger = new TestableTrxLogger();
            this.parameters = new Dictionary<string, string>(2);
            this.parameters[DefaultLoggerParameterNames.TestRunDirectory] = TrxLoggerTests.DefaultTestRunDirectory;
            this.parameters[TrxLogger.LogFileParameterKey] = TrxLoggerTests.DefaultLogFileParameterValue;
            this.testableTrxLogger.Initialize(this.events.Object, this.parameters);
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
            string message = "The information to test";
            TestRunMessageEventArgs trme = new TestRunMessageEventArgs(TestMessageLevel.Informational, message);
            this.testableTrxLogger.TestMessageHandler(new object(), trme);

            Assert.IsTrue(this.testableTrxLogger.GetRunLevelInformationalMessage().Contains(message));
        }

        [TestMethod]
        public void TestMessageHandlerShouldAddMessageInListIfItIsWarning()
        {
            string message = "The information to test";
            TestRunMessageEventArgs trme = new TestRunMessageEventArgs(TestMessageLevel.Warning, message);
            this.testableTrxLogger.TestMessageHandler(new object(), trme);
            this.testableTrxLogger.TestMessageHandler(new object(), trme);

            Assert.AreEqual(this.testableTrxLogger.GetRunLevelErrorsAndWarnings().Count, 2);
        }

        [TestMethod]
        public void TestMessageHandlerShouldAddMessageInListIfItIsError()
        {
            string message = "The information to test";
            TestRunMessageEventArgs trme = new TestRunMessageEventArgs(TestMessageLevel.Error, message);
            this.testableTrxLogger.TestMessageHandler(new object(), trme);

            Assert.AreEqual(this.testableTrxLogger.GetRunLevelErrorsAndWarnings().Count, 1);
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


            Assert.AreEqual(this.testableTrxLogger.PassedTestCount, 2, "Passed Tests");
            Assert.AreEqual(this.testableTrxLogger.FailedTestCount, 1, "Failed Tests");
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


            Assert.AreEqual(this.testableTrxLogger.TotalTestCount, 4, "Passed Tests");
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

            Assert.AreEqual(String.Compare(this.testableTrxLogger.GetRunLevelInformationalMessage(), expectedMessage, true), 0);
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

            Assert.AreEqual(this.testableTrxLogger.TestResultCount, 2, "TestResultHandler is not creating test result entry for each test case");
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

            Assert.AreEqual(this.testableTrxLogger.TestEntryCount, 2, "TestResultHandler is not creating test result entry for each test case");
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

            Assert.AreEqual(this.testableTrxLogger.UnitTestElementCount, 2, "TestResultHandler is not creating test result entry for each test case");
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
            this.parameters[TrxLogger.LogFileParameterKey] = null;
            this.testableTrxLogger.Initialize(this.events.Object, this.parameters);

            Mock<TestResultEventArgs> pass = CreatePassTestResultEventArgsMock();

            this.testableTrxLogger.TestResultHandler(new object(), pass.Object);

            var testRunCompleteEventArgs = CreateTestRunCompleteEventArgs();

            this.testableTrxLogger.TestRunCompleteHandler(new object(), testRunCompleteEventArgs);

            bool trxFileNameContainsWhiteSpace = Path.GetFileName(this.testableTrxLogger.trxFile).Contains(' ');
            Assert.IsFalse(trxFileNameContainsWhiteSpace, $"\"{this.testableTrxLogger.trxFile}\": Trx file name should not have white spaces");
        }

        [TestMethod]
        public void DefaultTrxFileShouldCreateIfLogFileParameterNotPassed()
        {
            // To create default trx file, If log file parameter not passed
            this.parameters.Remove(TrxLogger.LogFileParameterKey);
            this.testableTrxLogger.Initialize(this.events.Object, this.parameters);

            Mock<TestResultEventArgs> pass = CreatePassTestResultEventArgsMock();

            this.testableTrxLogger.TestResultHandler(new object(), pass.Object);

            var testRunCompleteEventArgs = CreateTestRunCompleteEventArgs();

            this.testableTrxLogger.TestRunCompleteHandler(new object(), testRunCompleteEventArgs);
            Assert.IsFalse(string.IsNullOrWhiteSpace(this.testableTrxLogger.trxFile));
        }

        [TestMethod]
        public void TrxFileNameShouldConstructFromLogFileParameter()
        {
            var pass = CreatePassTestResultEventArgsMock();

            this.testableTrxLogger.TestResultHandler(new object(), pass.Object);

            var testRunCompleteEventArgs = CreateTestRunCompleteEventArgs();

            this.testableTrxLogger.TestRunCompleteHandler(new object(), testRunCompleteEventArgs);
            Assert.AreEqual(DefaultLogFileParameterValue, this.testableTrxLogger.trxFile, "Wrong Trx file name");
        }

        [TestMethod]
        public void TrxFilePathShouldConstructProperlyIfRelativePathPassedInLogFileParameter()
        {
            var trxRelativePath = @".some\relative\path\results.trx";
            this.parameters[TrxLogger.LogFileParameterKey] = trxRelativePath;
            this.testableTrxLogger.Initialize(this.events.Object, this.parameters);
            var pass = CreatePassTestResultEventArgsMock();

            this.testableTrxLogger.TestResultHandler(new object(), pass.Object);

            var testRunCompleteEventArgs = CreateTestRunCompleteEventArgs();

            this.testableTrxLogger.TestRunCompleteHandler(new object(), testRunCompleteEventArgs);
            var expectedTrxFileName = Path.Combine(Directory.GetCurrentDirectory(), trxRelativePath);
            Assert.AreEqual(expectedTrxFileName, this.testableTrxLogger.trxFile, "Wrong Trx file name");
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

            List<String> listCategoriesActual = Converter.GetCustomPropertyValueFromTestCase(testCase1, "MSTestDiscoverer.TestCategory");

            List<String> listCategoriesExpected = new List<string>();
            listCategoriesExpected.Add("ClassLevel");
            listCategoriesExpected.Add("AsmLevel");

            CollectionAssert.AreEqual(listCategoriesExpected, listCategoriesActual);
        }

        /// <summary>
        /// Unit test for assigning or populating test categories read to the unit test element.
        /// </summary>
        [TestMethod]
        public void GetQToolsTestElementFromTestCaseShouldAssignTestCategoryOfUnitTestElement()
        {
            ObjectModel.TestCase testCase = CreateTestCase("TestCase1");
            ObjectModel.TestResult result = new ObjectModel.TestResult(testCase);
            TestProperty testProperty = TestProperty.Register("MSTestDiscoverer.TestCategory", "String array property", string.Empty, string.Empty, typeof(string[]), null, TestPropertyAttributes.Hidden, typeof(TestObject));

            testCase.SetPropertyValue(testProperty, new[] { "AsmLevel", "ClassLevel", "MethodLevel" });

            TrxLoggerObjectModel.UnitTestElement unitTestElement = Converter.GetQToolsTestElementFromTestCase(result);

            object[] expected = new[] { "MethodLevel", "ClassLevel", "AsmLevel" };

            CollectionAssert.AreEqual(expected, unitTestElement.TestCategories.ToArray().OrderByDescending(x => x.ToString()).ToArray());
        }

        /// <summary>
        /// Unit test for regression when there's no test categories.
        /// </summary>
        [TestMethod]
        public void GetQToolsTestElementFromTestCaseShouldNotFailWhenThereIsNoTestCategoreis()
        {
            ObjectModel.TestCase testCase = CreateTestCase("TestCase1");
            ObjectModel.TestResult result = new ObjectModel.TestResult(testCase);

            TrxLoggerObjectModel.UnitTestElement unitTestElement = Converter.GetQToolsTestElementFromTestCase(result);

            object[] expected = Enumerable.Empty<Object>().ToArray();

            CollectionAssert.AreEqual(expected, unitTestElement.TestCategories.ToArray());
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

        private Mock<TestResultEventArgs> CreatePassTestResultEventArgsMock()
        {
            ObjectModel.TestCase passTestCase = CreateTestCase("Pass1");
            ObjectModel.TestResult passResult = new ObjectModel.TestResult(passTestCase);
            Mock<TestResultEventArgs> pass = new Mock<TestResultEventArgs>(passResult);
            return pass;
        }
    }

    internal class TestableTrxLogger : TrxLogger
    {
        public string trxFile;
        internal override void PopulateTrxFile(string trxFileName, XmlElement rootElement)
        {
            this.trxFile = trxFileName;
        }
    }
}
