// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollector.UnitTests
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TestPlatformDataCollectionEventsTests
    {
        private readonly TestPlatformDataCollectionEvents events;

        private DataCollectionContext context;

        private bool isEventRaised;

        public TestPlatformDataCollectionEventsTests()
        {
            events = new TestPlatformDataCollectionEvents();
        }

        [TestMethod]
        public void RaiseEventsShouldThrowExceptionIfEventArgsIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() => events.RaiseEvent(null));
        }

        [TestMethod]
        public void RaiseEventsShouldRaiseEventsIfSessionStartEventArgsIsPassed()
        {
            isEventRaised = false;
            var testCase = new TestCase();
            context = new DataCollectionContext(testCase);

            events.SessionStart += SessionStartMessageHandler;
            var eventArgs = new SessionStartEventArgs(context, new Dictionary<string, object>());
            events.RaiseEvent(eventArgs);

            Assert.IsTrue(isEventRaised);
        }

        [TestMethod]
        public void RaiseEventsShouldNotRaiseEventsIfEventIsNotRegisterd()
        {
            isEventRaised = false;
            var testCase = new TestCase();
            context = new DataCollectionContext(testCase);

            var eventArgs = new SessionStartEventArgs(context, new Dictionary<string, object>());
            events.RaiseEvent(eventArgs);

            Assert.IsFalse(isEventRaised);
        }

        [TestMethod]
        public void RaiseEventsShouldNotRaiseEventsIfEventIsUnRegisterd()
        {
            isEventRaised = false;
            var testCase = new TestCase();
            context = new DataCollectionContext(testCase);

            events.SessionStart += SessionStartMessageHandler;
            events.SessionStart -= SessionStartMessageHandler;
            var eventArgs = new SessionStartEventArgs(context, new Dictionary<string, object>());
            events.RaiseEvent(eventArgs);

            Assert.IsFalse(isEventRaised);
        }

        [TestMethod]
        public void RaiseEventsShouldRaiseEventsIfSessionEndEventArgsIsPassed()
        {
            isEventRaised = false;
            var testCase = new TestCase();
            context = new DataCollectionContext(testCase);

            events.SessionEnd += SessionEndMessageHandler;
            var eventArgs = new SessionEndEventArgs(context);
            events.RaiseEvent(eventArgs);

            Assert.IsTrue(isEventRaised);
        }

        [TestMethod]
        public void RaiseEventsShouldRaiseEventsIfTestCaseStartEventArgsIsPassed()
        {
            isEventRaised = false;
            var testCase = new TestCase();
            context = new DataCollectionContext(testCase);

            events.TestCaseStart += TestCaseStartMessageHandler;
            var eventArgs = new TestCaseStartEventArgs(context, testCase);
            events.RaiseEvent(eventArgs);

            Assert.IsTrue(isEventRaised);
        }

        [TestMethod]
        public void RaiseEventsShouldRaiseEventsIfTestCaseEndEventArgsIsPassed()
        {
            isEventRaised = false;
            var testCase = new TestCase();
            context = new DataCollectionContext(testCase);

            events.TestCaseEnd += TestCaseEndMessageHandler;
            var eventArgs = new TestCaseEndEventArgs(context, testCase, TestOutcome.Passed);
            events.RaiseEvent(eventArgs);

            Assert.IsTrue(isEventRaised);
        }

        [TestMethod]
        public void AreTestCaseEventsSubscribedShouldReturnTrueIfTestCaseStartIsSubscribed()
        {
            events.TestCaseStart += TestCaseStartMessageHandler;

            Assert.IsTrue(events.AreTestCaseEventsSubscribed());
        }

        [TestMethod]
        public void AreTestCaseEventsSubscribedShouldReturnTrueIfTestCaseEndIsSubscribed()
        {
            events.TestCaseEnd += TestCaseEndMessageHandler;

            Assert.IsTrue(events.AreTestCaseEventsSubscribed());
        }

        [TestMethod]
        public void AreTestCaseEventsSubscribedShouldFalseIfTestCaseEventsAreNotSubscribed()
        {
            Assert.IsFalse(events.AreTestCaseEventsSubscribed());
        }

        private void SessionStartMessageHandler(object sender, SessionStartEventArgs e)
        {
            isEventRaised = true;
        }

        private void SessionEndMessageHandler(object sender, SessionEndEventArgs e)
        {
            isEventRaised = true;
        }

        private void TestCaseStartMessageHandler(object sender, TestCaseStartEventArgs e)
        {
            isEventRaised = true;
        }

        private void TestCaseEndMessageHandler(object sender, TestCaseEndEventArgs e)
        {
            isEventRaised = true;
        }
    }
}
