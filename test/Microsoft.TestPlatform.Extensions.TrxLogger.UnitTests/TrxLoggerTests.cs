// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.Extensions.TrxLogger;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Utility;

    using VisualStudio.TestPlatform.ObjectModel;
    using VisualStudio.TestPlatform.ObjectModel.Client;
    using VisualStudio.TestPlatform.ObjectModel.Logging;

    using ObjectModel = Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using TrxLoggerObjectModel = Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel;
    using TrxLoggerResources = Microsoft.TestPlatform.Extensions.TrxLogger.Resources.TrxResource;

    [TestClass]
    public class TrxLoggerTests
    {
        private Mock<TestLoggerEvents> events;
        private TrxLogger trxLogger;

        [TestInitialize]
        public void Initialize()
        {
            this.events = new Mock<TestLoggerEvents>();

            this.trxLogger = new TrxLogger();
            this.trxLogger.Initialize(this.events.Object, "dummy");
        }

        [TestMethod]
        public void InitializeShouldThrowExceptionIfEventsIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () =>
                    {
                        this.trxLogger.Initialize(null, "dummy");
                    });
        }

        [TestMethod]
        public void InitializeShouldNotThrowExceptionIfEventsIsNotNull()
        {
            var events = new Mock<TestLoggerEvents>();
            this.trxLogger.Initialize(events.Object, "dummy");
        }

        [TestMethod]
        public void InitializeShouldThrowExceptionIfTestRunDirectoryIsEmptyOrNull()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () =>
                {
                    var events = new Mock<TestLoggerEvents>();
                    this.trxLogger.Initialize(events.Object, null);
                });
        }

        [TestMethod]
        public void InitializeShouldNotThrowExceptionIfTestRunDirectoryIsNeitherEmptyNorNull()
        {
            var events = new Mock<TestLoggerEvents>();
            this.trxLogger.Initialize(events.Object, "dummy");
        }

        [TestMethod]
        public void TestMessageHandlerShouldThrowExceptionIfEventArgsIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.trxLogger.TestMessageHandler(new object(), default(TestRunMessageEventArgs));
            });
        }

        [TestMethod]
        public void TestMessageHandlerShouldAddMessageWhenItIsInformation()
        {
            string message = "The information to test";
            TestRunMessageEventArgs trme = new TestRunMessageEventArgs(TestMessageLevel.Informational, message);
            this.trxLogger.TestMessageHandler(new object(), trme);

            Assert.IsTrue(this.trxLogger.GetRunLevelInformationalMessage().Contains(message));
        }

        [TestMethod]
        public void TestMessageHandlerShouldAddMessageInListIfItIsWarning()
        {
            string message = "The information to test";
            TestRunMessageEventArgs trme = new TestRunMessageEventArgs(TestMessageLevel.Warning, message);
            this.trxLogger.TestMessageHandler(new object(), trme);
            this.trxLogger.TestMessageHandler(new object(), trme);

            Assert.AreEqual(this.trxLogger.GetRunLevelErrorsAndWarnings().Count, 2);
        }

        [TestMethod]
        public void TestMessageHandlerShouldAddMessageInListIfItIsError()
        {
            string message = "The information to test";
            TestRunMessageEventArgs trme = new TestRunMessageEventArgs(TestMessageLevel.Error, message);
            this.trxLogger.TestMessageHandler(new object(), trme);

            Assert.AreEqual(this.trxLogger.GetRunLevelErrorsAndWarnings().Count, 1);
        }

        [TestMethod]
        public void TestResultHandlerShouldCaptureStartTimeInSummaryWithTimeStampDuringIntialize()
        {
            ObjectModel.TestCase testCase = new ObjectModel.TestCase("dummy string", new Uri("some://uri"), "DummySourceFileName");
            ObjectModel.TestResult testResult = new ObjectModel.TestResult(testCase);
            Mock<TestResultEventArgs> e = new Mock<TestResultEventArgs>(testResult);

            trxLogger.TestResultHandler(new object(), e.Object);

            Assert.AreEqual(this.trxLogger.TestRunStartTime, this.trxLogger.LoggerTestRun.Started);
        }
        
        [TestMethod]
        public void TestResultHandlerKeepingTheTrackOfPassedAndFailedTests()
        {
            ObjectModel.TestCase passTestCase1 = new ObjectModel.TestCase("Pass1", new Uri("some://uri"), "DummySourceFileName");
            ObjectModel.TestCase passTestCase2 = new ObjectModel.TestCase("Pass2", new Uri("some://uri"), "DummySourceFileName");
            ObjectModel.TestCase failTestCase1 = new ObjectModel.TestCase("Fail1", new Uri("some://uri"), "DummySourceFileName");
            ObjectModel.TestCase skipTestCase1 = new ObjectModel.TestCase("Skip1", new Uri("some://uri"), "DummySourceFileName");

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


            this.trxLogger.TestResultHandler(new object(), pass1.Object);
            this.trxLogger.TestResultHandler(new object(), pass2.Object);
            this.trxLogger.TestResultHandler(new object(), fail1.Object);
            this.trxLogger.TestResultHandler(new object(), skip1.Object);


            Assert.AreEqual(this.trxLogger.PassedTestCount, 2, "Passed Tests");
            Assert.AreEqual(this.trxLogger.FailedTestCount, 1, "Failed Tests");
        }

        [TestMethod]
        public void TestResultHandlerKeepingTheTrackOfTotalTests()
        {
            ObjectModel.TestCase passTestCase1 = new ObjectModel.TestCase("Pass1", new Uri("some://uri"), "DummySourceFileName");
            ObjectModel.TestCase passTestCase2 = new ObjectModel.TestCase("Pass2", new Uri("some://uri"), "DummySourceFileName");
            ObjectModel.TestCase failTestCase1 = new ObjectModel.TestCase("Fail1", new Uri("some://uri"), "DummySourceFileName");
            ObjectModel.TestCase skipTestCase1 = new ObjectModel.TestCase("Skip1", new Uri("some://uri"), "DummySourceFileName");

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


            this.trxLogger.TestResultHandler(new object(), pass1.Object);
            this.trxLogger.TestResultHandler(new object(), pass2.Object);
            this.trxLogger.TestResultHandler(new object(), fail1.Object);
            this.trxLogger.TestResultHandler(new object(), skip1.Object);


            Assert.AreEqual(this.trxLogger.TotalTestCount, 4, "Passed Tests");
        }

        [TestMethod]
        public void TestResultHandlerLockingAMessageForSkipTest()
        {
            ObjectModel.TestCase skipTestCase1 = new ObjectModel.TestCase("Skip1", new Uri("some://uri"), "DummySourceFileName");

            ObjectModel.TestResult skipResult1 = new ObjectModel.TestResult(skipTestCase1);
            skipResult1.Outcome = ObjectModel.TestOutcome.Skipped;

            Mock<TestResultEventArgs> skip1 = new Mock<TestResultEventArgs>(skipResult1);

            this.trxLogger.TestResultHandler(new object(), skip1.Object);

            string expectedMessage = String.Format(CultureInfo.CurrentCulture, TrxLoggerResources.MessageForSkippedTests, "Skip1");

            Assert.AreEqual(String.Compare(this.trxLogger.GetRunLevelInformationalMessage(), expectedMessage, true), 0);
        }

        [TestMethod]
        public void TestResultHandlerShouldCreateOneTestResultForEachTestCase()
        {
            ObjectModel.TestCase testCase1 = new ObjectModel.TestCase("TestCase1", new Uri("some://uri"), "DummySourceFileName");
            ObjectModel.TestCase testCase2 = new ObjectModel.TestCase("TestCase2", new Uri("some://uri"), "DummySourceFileName");

            ObjectModel.TestResult result1 = new ObjectModel.TestResult(testCase1);
            result1.Outcome = ObjectModel.TestOutcome.Skipped;

            ObjectModel.TestResult result2 = new ObjectModel.TestResult(testCase2);
            result2.Outcome = ObjectModel.TestOutcome.Failed;

            Mock<TestResultEventArgs> resultEventArg1 = new Mock<TestResultEventArgs>(result1);
            Mock<TestResultEventArgs> resultEventArg2 = new Mock<TestResultEventArgs>(result2);

            this.trxLogger.TestResultHandler(new object(), resultEventArg1.Object);
            this.trxLogger.TestResultHandler(new object(), resultEventArg2.Object);

            Assert.AreEqual(this.trxLogger.TestResultCount, 2, "TestResultHandler is not creating test result entry for each test case");
        }

        [TestMethod]
        public void TestResultHandlerShouldCreateOneTestEntryForEachTestCase()
        {
            ObjectModel.TestCase testCase1 = new ObjectModel.TestCase("TestCase1", new Uri("some://uri"), "DummySourceFileName");
            ObjectModel.TestCase testCase2 = new ObjectModel.TestCase("TestCase2", new Uri("some://uri"), "DummySourceFileName");

            ObjectModel.TestResult result1 = new ObjectModel.TestResult(testCase1);
            result1.Outcome = ObjectModel.TestOutcome.Skipped;

            ObjectModel.TestResult result2 = new ObjectModel.TestResult(testCase2);
            result2.Outcome = ObjectModel.TestOutcome.Passed;

            Mock<TestResultEventArgs> resultEventArg1 = new Mock<TestResultEventArgs>(result1);
            Mock<TestResultEventArgs> resultEventArg2 = new Mock<TestResultEventArgs>(result2);

            this.trxLogger.TestResultHandler(new object(), resultEventArg1.Object);
            this.trxLogger.TestResultHandler(new object(), resultEventArg2.Object);

            Assert.AreEqual(this.trxLogger.TestEntryCount, 2, "TestResultHandler is not creating test result entry for each test case");
        }

        [TestMethod]
        public void TestResultHandlerShouldCreateOneUnitTestElementForEachTestCase()
        {
            ObjectModel.TestCase testCase1 = new ObjectModel.TestCase("TestCase1", new Uri("some://uri"), "DummySourceFileName");
            ObjectModel.TestCase testCase2 = new ObjectModel.TestCase("TestCase2", new Uri("some://uri"), "DummySourceFileName");

            ObjectModel.TestResult result1 = new ObjectModel.TestResult(testCase1);

            ObjectModel.TestResult result2 = new ObjectModel.TestResult(testCase2);
            result2.Outcome = ObjectModel.TestOutcome.Failed;

            Mock<TestResultEventArgs> resultEventArg1 = new Mock<TestResultEventArgs>(result1);
            Mock<TestResultEventArgs> resultEventArg2 = new Mock<TestResultEventArgs>(result2);

            this.trxLogger.TestResultHandler(new object(), resultEventArg1.Object);
            this.trxLogger.TestResultHandler(new object(), resultEventArg2.Object);

            Assert.AreEqual(this.trxLogger.UnitTestElementCount, 2, "TestResultHandler is not creating test result entry for each test case");
        }

        [TestMethod]
        public void OutcomeOfRunWillBeFailIfAnyTestsFails()
        {
            ObjectModel.TestCase passTestCase1 = new ObjectModel.TestCase("Pass1", new Uri("some://uri"), "DummySourceFileName");
            ObjectModel.TestCase passTestCase2 = new ObjectModel.TestCase("Pass2", new Uri("some://uri"), "DummySourceFileName");
            ObjectModel.TestCase failTestCase1 = new ObjectModel.TestCase("Fail1", new Uri("some://uri"), "DummySourceFileName");
            ObjectModel.TestCase skipTestCase1 = new ObjectModel.TestCase("Skip1", new Uri("some://uri"), "DummySourceFileName");

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

            this.trxLogger.TestResultHandler(new object(), pass1.Object);
            this.trxLogger.TestResultHandler(new object(), pass2.Object);
            this.trxLogger.TestResultHandler(new object(), fail1.Object);
            this.trxLogger.TestResultHandler(new object(), skip1.Object);

            var testRunCompleteEventArgs = new TestRunCompleteEventArgs(null, false, false, null, new Collection<AttachmentSet>(), new TimeSpan(1, 0, 0, 0));

            try
            {
                // Intentionally making it null so that it will not create actual trx file
                TrxLogger.TrxFileDirectory = null;
                this.trxLogger.TestRunCompleteHandler(new object(), testRunCompleteEventArgs);
            }
            catch (ArgumentNullException)
            {
            }

            Assert.AreEqual(TrxLoggerObjectModel.TestOutcome.Failed, this.trxLogger.TestResultOutcome);
        }

        [TestMethod]
        public void OutcomeOfRunWillBeCompletedIfNoTestsFails()
        {
            ObjectModel.TestCase passTestCase1 = new ObjectModel.TestCase("Pass1", new Uri("some://uri"), "DummySourceFileName");
            ObjectModel.TestCase passTestCase2 = new ObjectModel.TestCase("Pass2", new Uri("some://uri"), "DummySourceFileName");
            ObjectModel.TestCase skipTestCase1 = new ObjectModel.TestCase("Skip1", new Uri("some://uri"), "DummySourceFileName");

            ObjectModel.TestResult passResult1 = new ObjectModel.TestResult(passTestCase1);
            passResult1.Outcome = ObjectModel.TestOutcome.Passed;

            ObjectModel.TestResult passResult2 = new ObjectModel.TestResult(passTestCase2);
            passResult2.Outcome = ObjectModel.TestOutcome.Passed;

            ObjectModel.TestResult skipResult1 = new ObjectModel.TestResult(skipTestCase1);
            skipResult1.Outcome = ObjectModel.TestOutcome.Skipped;

            Mock<TestResultEventArgs> pass1 = new Mock<TestResultEventArgs>(passResult1);
            Mock<TestResultEventArgs> pass2 = new Mock<TestResultEventArgs>(passResult2);
            Mock<TestResultEventArgs> skip1 = new Mock<TestResultEventArgs>(skipResult1);

            this.trxLogger.TestResultHandler(new object(), pass1.Object);
            this.trxLogger.TestResultHandler(new object(), pass2.Object);
            this.trxLogger.TestResultHandler(new object(), skip1.Object);

            var testRunCompleteEventArgs = new TestRunCompleteEventArgs(null, false, false, null, new Collection<AttachmentSet>(), new TimeSpan(1, 0, 0, 0));

            try
            {
                // Intentionally making it null so that it will not create actual trx file
                TrxLogger.TrxFileDirectory = null;
                this.trxLogger.TestRunCompleteHandler(new object(), testRunCompleteEventArgs);
            }
            catch (ArgumentNullException)
            {
            }

            Assert.AreEqual(TrxLoggerObjectModel.TestOutcome.Completed, this.trxLogger.TestResultOutcome);
        }

        /// <summary>
        /// Unit test for reading TestCategories from the TestCase which is part of test result.
        /// </summary>
        [TestMethod]
        public void GetCustomPropertyValueFromTestCaseShouldReadCategoyrAttributesFromTestCase()
        {
            ObjectModel.TestCase testCase1 = new ObjectModel.TestCase("TestCase1", new Uri("some://uri"), "DummySourceFileName");
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
            ObjectModel.TestCase testCase = new ObjectModel.TestCase("TestCase1", new Uri("some://uri"), "DummySourceFileName");
            ObjectModel.TestResult result = new ObjectModel.TestResult(testCase);
            TestProperty testProperty = TestProperty.Register("MSTestDiscoverer.TestCategory", "String array property", string.Empty, string.Empty, typeof(string[]), null, TestPropertyAttributes.Hidden, typeof(TestObject));

            testCase.SetPropertyValue(testProperty, new[] { "AsmLevel", "ClassLevel", "MethodLevel" });

            TrxLoggerObjectModel.UnitTestElement unitTestElement = Converter.GetQToolsTestElementFromTestCase(result);

            object[] expected = new[] { "MethodLevel", "ClassLevel", "AsmLevel" };

            CollectionAssert.AreEqual(expected, unitTestElement.TestCategories.ToArray().OrderByDescending(x =>x.ToString()).ToArray());
        }

        /// <summary>
        /// Unit test for regression when there's no test categories.
        /// </summary>
        [TestMethod]
        public void GetQToolsTestElementFromTestCaseShouldNotFailWhenThereIsNoTestCategoreis()
        {
            ObjectModel.TestCase testCase = new ObjectModel.TestCase("TestCase1", new Uri("some://uri"), "DummySourceFileName");
            ObjectModel.TestResult result = new ObjectModel.TestResult(testCase);

            TrxLoggerObjectModel.UnitTestElement unitTestElement = Converter.GetQToolsTestElementFromTestCase(result);

            object[] expected = Enumerable.Empty<Object>().ToArray();

            CollectionAssert.AreEqual(expected, unitTestElement.TestCategories.ToArray());
        }
    }
}
