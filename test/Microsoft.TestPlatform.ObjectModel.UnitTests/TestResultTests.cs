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
    }
}
