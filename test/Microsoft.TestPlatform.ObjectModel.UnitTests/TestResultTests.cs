// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.ObjectModel.UnitTests
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using TestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TestResultTests
    {
        private readonly TestCase testcase;
        private readonly TestResult result;

        public TestResultTests()
        {
            this.testcase = new TestCase("FQN", new Uri("http://dummyUri"), "dummySource");
            this.result = new TestResult(testcase);
        }

        [TestMethod]
        public void TestResultShouldInitializeEmptyAttachments()
        {
            Assert.AreEqual(0, this.result.Attachments.Count);
        }

        [TestMethod]
        public void TestResultShouldInitializeEmptyMessages()
        {
            Assert.AreEqual(0, this.result.Messages.Count);
        }

        [TestMethod]
        public void TestResultShouldInitializeStartAndEndTimeToCurrent()
        {
            Assert.IsTrue(this.result.StartTime.Subtract(DateTimeOffset.Now) < new TimeSpan(0, 0, 0, 10));
            Assert.IsTrue(this.result.EndTime.Subtract(DateTimeOffset.Now) < new TimeSpan(0, 0, 0, 10));
        }

        #region GetSetPropertyValue Tests

        [TestMethod]
        public void TestResultGetPropertyValueForComputerNameShouldReturnCorrectValue()
        {
            var testComputerName = "computerName";
            this.result.ComputerName = testComputerName;

            Assert.AreEqual(testComputerName, this.result.GetPropertyValue(TestResultProperties.ComputerName));
        }

        [TestMethod]
        public void TestResultGetPropertyValueForDisplayNameShouldReturnCorrectValue()
        {
            var testDisplayName = "displayName";
            this.result.DisplayName = testDisplayName;

            Assert.AreEqual(testDisplayName, this.result.GetPropertyValue(TestResultProperties.DisplayName));
        }

        [TestMethod]
        public void TestResultGetPropertyValueForDurationShouldReturnCorrectValue()
        {
            var testDuration = new TimeSpan(0, 0, 0, 10);
            this.result.Duration = testDuration;

            Assert.AreEqual(testDuration, this.result.GetPropertyValue(TestResultProperties.Duration));
        }

        [TestMethod]
        public void TestResultGetPropertyValueForEndTimeShouldReturnCorrectValue()
        {
            var testEndTime = new DateTimeOffset(new DateTime(2007, 3, 10, 0, 0, 10, DateTimeKind.Utc));
            this.result.EndTime = testEndTime;

            Assert.AreEqual(testEndTime, this.result.GetPropertyValue(TestResultProperties.EndTime));
        }

        [TestMethod]
        public void TestResultGetPropertyValueForErrorMessageShouldReturnCorrectValue()
        {
            var testErrorMessage = "error123";
            this.result.ErrorMessage = testErrorMessage;

            Assert.AreEqual(testErrorMessage, this.result.GetPropertyValue(TestResultProperties.ErrorMessage));
        }

        [TestMethod]
        public void TestResultGetPropertyValueForErrorStackTraceShouldReturnCorrectValue()
        {
            var testErrorStackTrace = "errorStack";
            this.result.ErrorStackTrace = testErrorStackTrace;

            Assert.AreEqual(testErrorStackTrace, this.result.GetPropertyValue(TestResultProperties.ErrorStackTrace));
        }

        [TestMethod]
        public void TestResultGetPropertyValueForTestOutcomeShouldReturnCorrectValue()
        {
            var testOutcome = TestOutcome.Passed;
            this.result.Outcome = testOutcome;

            Assert.AreEqual(testOutcome, this.result.GetPropertyValue(TestResultProperties.Outcome));
        }

        [TestMethod]
        public void TestResultGetPropertyValueForStartTimeShouldReturnCorrectValue()
        {
            var testStartTime = new DateTimeOffset(new DateTime(2007, 3, 10, 0, 0, 0, DateTimeKind.Utc));
            this.result.StartTime = testStartTime;

            Assert.AreEqual(testStartTime, this.result.GetPropertyValue(TestResultProperties.StartTime));
        }

        [TestMethod]
        public void TestResultSetPropertyValueForComputerNameShouldSetValue()
        {
            var testComputerName = "computerNameSet";
            this.result.SetPropertyValue(TestResultProperties.ComputerName, testComputerName);

            Assert.AreEqual(testComputerName, this.result.ComputerName);
        }

        [TestMethod]
        public void TestResultSetPropertyValueForDisplayNameShouldSetValue()
        {
            var testDisplayName = "displayNameSet";
            this.result.SetPropertyValue(TestResultProperties.DisplayName, testDisplayName);

            Assert.AreEqual(testDisplayName, this.result.DisplayName);
        }

        [TestMethod]
        public void TestResultSetPropertyValueForDurationShouldSetValue()
        {
            var testDuration = new TimeSpan(0, 0, 0, 20);
            this.result.SetPropertyValue(TestResultProperties.Duration, testDuration);

            Assert.AreEqual(testDuration, this.result.Duration);
        }

        [TestMethod]
        public void TestResultSetPropertyValueForEndTimeShouldSetValue()
        {
            var testEndTime = new DateTimeOffset(new DateTime(2007, 5, 10, 0, 0, 10, DateTimeKind.Utc));
            this.result.SetPropertyValue(TestResultProperties.EndTime, testEndTime);

            Assert.AreEqual(testEndTime, this.result.EndTime);
        }

        [TestMethod]
        public void TestResultSetPropertyValueForErrorMessageShouldSetValue()
        {
            var testErrorMessage = "error123Set";
            this.result.SetPropertyValue(TestResultProperties.ErrorMessage, testErrorMessage);

            Assert.AreEqual(testErrorMessage, this.result.ErrorMessage);
        }

        [TestMethod]
        public void TestResultSetPropertyValueForErrorStackTraceShouldSetValue()
        {
            var testErrorStackTrace = "errorStackSet";
            this.result.SetPropertyValue(TestResultProperties.ErrorStackTrace, testErrorStackTrace);

            Assert.AreEqual(testErrorStackTrace, this.result.ErrorStackTrace);
        }

        [TestMethod]
        public void TestResultSetPropertyValueForTestOutcomeShouldSetValue()
        {
            var testOutcome = TestOutcome.Failed;
            this.result.SetPropertyValue(TestResultProperties.Outcome, testOutcome);

            Assert.AreEqual(testOutcome, this.result.Outcome);
        }

        [TestMethod]
        public void TestResultSetPropertyValueForStartTimeShouldSetValue()
        {
            var testStartTime = new DateTimeOffset(new DateTime(2007, 5, 10, 0, 0, 0, DateTimeKind.Utc));
            this.result.SetPropertyValue(TestResultProperties.StartTime, testStartTime);

            Assert.AreEqual(testStartTime, this.result.StartTime);
        }

        #endregion

    }
}
