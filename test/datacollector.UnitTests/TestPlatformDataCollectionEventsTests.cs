// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector.UnitTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.VisualStudio.TestPlatform.DataCollector;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    [TestClass]
    public class TestPlatformDataCollectionEventsTests
    {
        private TestPlatformDataCollectionEvents events;

        private DataCollectionContext context;

        private bool isEventRaised;

        [TestInitialize]
        public void Init()
        {
            events = new TestPlatformDataCollectionEvents();

        }

        [TestMethod]
        public void RaiseEventsShouldThrowExceptionIfEventArgsIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.events.RaiseEvent(null);
            });
        }

        [TestMethod]
        public void RaiseEventsShouldRaiseEventsIfSessionStartEventArgsIsPassed()
        {
            this.isEventRaised = false;
            var testCase = new TestCase();
            this.context = new DataCollectionContext(testCase);

            events.SessionStart += this.SessionStartMessageHandler;
            var eventArgs = new SessionStartEventArgs(this.context);
            this.events.RaiseEvent(eventArgs);

            Assert.IsTrue(this.isEventRaised);
        }

        [TestMethod]
        public void RaiseEventsShouldNotRaiseEventsIfEventIsNotRegisterd()
        {
            this.isEventRaised = false;
            var testCase = new TestCase();
            this.context = new DataCollectionContext(testCase);

            var eventArgs = new SessionStartEventArgs(this.context);
            this.events.RaiseEvent(eventArgs);

            Assert.IsFalse(this.isEventRaised);
        }

        [TestMethod]
        public void RaiseEventsShouldNotRaiseEventsIfEventIsUnRegisterd()
        {
            this.isEventRaised = false;
            var testCase = new TestCase();
            this.context = new DataCollectionContext(testCase);

            events.SessionStart += this.SessionStartMessageHandler;
            events.SessionStart -= this.SessionStartMessageHandler;
            var eventArgs = new SessionStartEventArgs(this.context);
            this.events.RaiseEvent(eventArgs);

            Assert.IsFalse(this.isEventRaised);
        }

        [TestMethod]
        public void RaiseEventsShouldRaiseEventsIfSessionEndEventArgsIsPassed()
        {
            this.isEventRaised = false;
            var testCase = new TestCase();
            this.context = new DataCollectionContext(testCase);

            events.SessionEnd += this.SessionEndMessageHandler;
            var eventArgs = new SessionEndEventArgs(this.context);
            this.events.RaiseEvent(eventArgs);

            Assert.IsTrue(this.isEventRaised);
        }

        [TestMethod]
        public void RaiseEventsShouldRaiseEventsIfTestCaseStartEventArgsIsPassed()
        {
            this.isEventRaised = false;
            var testCase = new TestCase();
            this.context = new DataCollectionContext(testCase);

            events.TestCaseStart += this.TestCaseStartMessageHandler;
            var eventArgs = new TestCaseStartEventArgs(this.context, testCase);
            this.events.RaiseEvent(eventArgs);

            Assert.IsTrue(this.isEventRaised);
        }

        [TestMethod]
        public void RaiseEventsShouldRaiseEventsIfTestCaseEndEventArgsIsPassed()
        {
            this.isEventRaised = false;
            var testCase = new TestCase();
            this.context = new DataCollectionContext(testCase);

            events.TestCaseEnd += this.TestCaseEndMessageHandler;
            var eventArgs = new TestCaseEndEventArgs(this.context, testCase, TestOutcome.Passed);
            this.events.RaiseEvent(eventArgs);

            Assert.IsTrue(this.isEventRaised);
        }

        private void SessionStartMessageHandler(object sender, SessionStartEventArgs e)
        {
            this.isEventRaised = true;
        }

        private void SessionEndMessageHandler(object sender, SessionEndEventArgs e)
        {
            this.isEventRaised = true;
        }

        private void TestCaseStartMessageHandler(object sender, TestCaseStartEventArgs e)
        {
            this.isEventRaised = true;
        }

        private void TestCaseEndMessageHandler(object sender, TestCaseEndEventArgs e)
        {
            this.isEventRaised = true;
        }
    }
}
