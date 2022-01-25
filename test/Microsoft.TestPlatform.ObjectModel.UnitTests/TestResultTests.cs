// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.ObjectModel.UnitTests
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using TestResult = VisualStudio.TestPlatform.ObjectModel.TestResult;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TestResultTests
    {
        private readonly TestCase testcase;
        private readonly TestResult result;

        public TestResultTests()
        {
            testcase = new TestCase("FQN", new Uri("http://dummyUri"), "dummySource");
            result = new TestResult(testcase);
        }

        [TestMethod]
        public void TestResultShouldInitializeEmptyAttachments()
        {
            Assert.AreEqual(0, result.Attachments.Count);
        }

        [TestMethod]
        public void TestResultShouldInitializeEmptyMessages()
        {
            Assert.AreEqual(0, result.Messages.Count);
        }

        [TestMethod]
        public void TestResultShouldInitializeStartAndEndTimeToCurrent()
        {
            Assert.IsTrue(result.StartTime.Subtract(DateTimeOffset.UtcNow) < new TimeSpan(0, 0, 0, 10));
            Assert.IsTrue(result.EndTime.Subtract(DateTimeOffset.UtcNow) < new TimeSpan(0, 0, 0, 10));
        }

        #region GetSetPropertyValue Tests

        [TestMethod]
        public void TestResultGetPropertyValueForComputerNameShouldReturnCorrectValue()
        {
            var testComputerName = "computerName";
            result.ComputerName = testComputerName;

            Assert.AreEqual(testComputerName, result.GetPropertyValue(TestResultProperties.ComputerName));
        }

        [TestMethod]
        public void TestResultGetPropertyValueForDisplayNameShouldReturnCorrectValue()
        {
            var testDisplayName = "displayName";
            result.DisplayName = testDisplayName;

            Assert.AreEqual(testDisplayName, result.GetPropertyValue(TestResultProperties.DisplayName));
        }

        [TestMethod]
        public void TestResultGetPropertyValueForDurationShouldReturnCorrectValue()
        {
            var testDuration = new TimeSpan(0, 0, 0, 10);
            result.Duration = testDuration;

            Assert.AreEqual(testDuration, result.GetPropertyValue(TestResultProperties.Duration));
        }

        [TestMethod]
        public void TestResultGetPropertyValueForEndTimeShouldReturnCorrectValue()
        {
            var testEndTime = new DateTimeOffset(new DateTime(2007, 3, 10, 0, 0, 10, DateTimeKind.Utc));
            result.EndTime = testEndTime;

            Assert.AreEqual(testEndTime, result.GetPropertyValue(TestResultProperties.EndTime));
        }

        [TestMethod]
        public void TestResultGetPropertyValueForErrorMessageShouldReturnCorrectValue()
        {
            var testErrorMessage = "error123";
            result.ErrorMessage = testErrorMessage;

            Assert.AreEqual(testErrorMessage, result.GetPropertyValue(TestResultProperties.ErrorMessage));
        }

        [TestMethod]
        public void TestResultGetPropertyValueForErrorStackTraceShouldReturnCorrectValue()
        {
            var testErrorStackTrace = "errorStack";
            result.ErrorStackTrace = testErrorStackTrace;

            Assert.AreEqual(testErrorStackTrace, result.GetPropertyValue(TestResultProperties.ErrorStackTrace));
        }

        [TestMethod]
        public void TestResultGetPropertyValueForTestOutcomeShouldReturnCorrectValue()
        {
            var testOutcome = TestOutcome.Passed;
            result.Outcome = testOutcome;

            Assert.AreEqual(testOutcome, result.GetPropertyValue(TestResultProperties.Outcome));
        }

        [TestMethod]
        public void TestResultGetPropertyValueForStartTimeShouldReturnCorrectValue()
        {
            var testStartTime = new DateTimeOffset(new DateTime(2007, 3, 10, 0, 0, 0, DateTimeKind.Utc));
            result.StartTime = testStartTime;

            Assert.AreEqual(testStartTime, result.GetPropertyValue(TestResultProperties.StartTime));
        }

        [TestMethod]
        public void TestResultSetPropertyValueForComputerNameShouldSetValue()
        {
            var testComputerName = "computerNameSet";
            result.SetPropertyValue(TestResultProperties.ComputerName, testComputerName);

            Assert.AreEqual(testComputerName, result.ComputerName);
        }

        [TestMethod]
        public void TestResultSetPropertyValueForDisplayNameShouldSetValue()
        {
            var testDisplayName = "displayNameSet";
            result.SetPropertyValue(TestResultProperties.DisplayName, testDisplayName);

            Assert.AreEqual(testDisplayName, result.DisplayName);
        }

        [TestMethod]
        public void TestResultSetPropertyValueForDurationShouldSetValue()
        {
            var testDuration = new TimeSpan(0, 0, 0, 20);
            result.SetPropertyValue(TestResultProperties.Duration, testDuration);

            Assert.AreEqual(testDuration, result.Duration);
        }

        [TestMethod]
        public void TestResultSetPropertyValueForEndTimeShouldSetValue()
        {
            var testEndTime = new DateTimeOffset(new DateTime(2007, 5, 10, 0, 0, 10, DateTimeKind.Utc));
            result.SetPropertyValue(TestResultProperties.EndTime, testEndTime);

            Assert.AreEqual(testEndTime, result.EndTime);
        }

        [TestMethod]
        public void TestResultSetPropertyValueForErrorMessageShouldSetValue()
        {
            var testErrorMessage = "error123Set";
            result.SetPropertyValue(TestResultProperties.ErrorMessage, testErrorMessage);

            Assert.AreEqual(testErrorMessage, result.ErrorMessage);
        }

        [TestMethod]
        public void TestResultSetPropertyValueForErrorStackTraceShouldSetValue()
        {
            var testErrorStackTrace = "errorStackSet";
            result.SetPropertyValue(TestResultProperties.ErrorStackTrace, testErrorStackTrace);

            Assert.AreEqual(testErrorStackTrace, result.ErrorStackTrace);
        }

        [TestMethod]
        public void TestResultSetPropertyValueForTestOutcomeShouldSetValue()
        {
            var testOutcome = TestOutcome.Failed;
            result.SetPropertyValue(TestResultProperties.Outcome, testOutcome);

            Assert.AreEqual(testOutcome, result.Outcome);
        }

        [TestMethod]
        public void TestResultSetPropertyValueForStartTimeShouldSetValue()
        {
            var testStartTime = new DateTimeOffset(new DateTime(2007, 5, 10, 0, 0, 0, DateTimeKind.Utc));
            result.SetPropertyValue(TestResultProperties.StartTime, testStartTime);

            Assert.AreEqual(testStartTime, result.StartTime);
        }

        #endregion

    }
}
