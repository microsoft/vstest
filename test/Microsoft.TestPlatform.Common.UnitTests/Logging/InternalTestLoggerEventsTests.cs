// Copyright (c) Microsoft. All rights reserved.

namespace TestPlatform.Common.UnitTests.Logging
{
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Threading;
    using TestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;

    [TestClass]
    public class InternalTestLoggerEventsBehaviors
    {
        private TestSessionMessageLogger testSessionMessageLogger;
        private InternalTestLoggerEvents loggerEvents;

        [TestInitialize]
        public void Initialize()
        {
            testSessionMessageLogger = TestSessionMessageLogger.Instance;
            loggerEvents = new InternalTestLoggerEvents(testSessionMessageLogger);
        }

        [TestCleanup]
        public void Dispose()
        {
            loggerEvents.Dispose();
            TestSessionMessageLogger.Instance = null;
        }

        [TestMethod]
        public void RaiseMessageShouldNotThrowExceptionIfNoEventHandlersAreRegistered()
        {
            // Send the test mesage event.
            loggerEvents.RaiseMessage(new TestRunMessageEventArgs(TestMessageLevel.Informational,"This is a string."));
        }

        [TestMethod]
        public void RaiseMessageShouldInvokeRegisteredEventHandlerIfTestRunMessageEventArgsIsPassed()
        {
            EventWaitHandle waitHandle = new AutoResetEvent(false);
            bool testMessageReceived = false;
            TestRunMessageEventArgs eventArgs = null;
            var message = "This is the test message";

            // Register for the test message event.
            loggerEvents.TestRunMessage += (sender, e) =>
            {
                testMessageReceived = true;
                eventArgs = e;
                waitHandle.Set();
            };

            loggerEvents.EnableEvents();
            // Send the test mesage event.
            loggerEvents.RaiseMessage(new TestRunMessageEventArgs(TestMessageLevel.Informational, message));

            var waitSuccess = waitHandle.WaitOne(500);
            Assert.IsTrue(waitSuccess, "Event must be raised within timeout.");

            Assert.IsTrue(testMessageReceived);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(message, eventArgs.Message);
            Assert.AreEqual(TestMessageLevel.Informational, eventArgs.Level);
        }

        [TestMethod]
        public void RaiseMessageShouldInvokeRegisteredEventHandlerIfTestTestResultEventArgsIsPassed()
        {
            EventWaitHandle waitHandle = new AutoResetEvent(false);
            bool testResultReceived = false;
            TestResultEventArgs eventArgs = null;
            var result =new TestResult(new TestCase("This is a string.", new Uri("some://uri"), "DummySourceFileName"));

            // Register for the test result event.
            loggerEvents.TestResult += (sender, e) =>
            {
                testResultReceived = true;
                eventArgs = e;
                waitHandle.Set();
            };

            loggerEvents.EnableEvents();
            // Send the test result event.
            loggerEvents.RaiseTestResult(new TestResultEventArgs(result));

            var waitSuccess = waitHandle.WaitOne(500);
            Assert.IsTrue(waitSuccess, "Event must be raised within timeout.");

            Assert.IsTrue(testResultReceived);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(result, eventArgs.Result);
        }

        [TestMethod]
        public void RaiseTestResultShouldThrowExceptionIfNullTestResultEventArgsIsPassed()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                loggerEvents.RaiseTestResult(null);
            });
        }

        [TestMethod]
        public void RaiseMessageShouldThrowExceptioIfNullTestRunMessageEventArgsIsPassed()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                loggerEvents.RaiseMessage(null);
            });
        }

        [TestMethod]
        public void CompleteTestRunShouldInvokeRegisteredEventHandler()
        {
            bool testRunCompleteReceived = false;
            TestRunCompleteEventArgs eventArgs = null;

            EventWaitHandle waitHandle = new AutoResetEvent(false);

            // Register for the test run complete event.
            loggerEvents.TestRunComplete += (sender, e) =>
            {
                testRunCompleteReceived = true;
                eventArgs = e;
                waitHandle.Set();
            };

            loggerEvents.EnableEvents();
            // Send the test run complete event.
            loggerEvents.CompleteTestRun(null, false, false, null, null, new TimeSpan());

            var waitSuccess = waitHandle.WaitOne(500);
            Assert.IsTrue(waitSuccess, "Event must be raised within timeout.");
            Assert.IsTrue(testRunCompleteReceived);
            Assert.IsNotNull(eventArgs);
        }

        [TestMethod]
        public void EnableEventsShouldSendEventsAlreadyPresentInQueueToRegisteredEventHandlers()
        {
            bool testResultReceived = false;
            bool testMessageReceived = false;

            // Send the events.
            loggerEvents.RaiseMessage(new TestRunMessageEventArgs(TestMessageLevel.Error,"This is a string."));
            loggerEvents.RaiseTestResult(new TestResultEventArgs(new TestResult(new TestCase("This is a string.", new Uri("some://uri"), "DummySourceFileName"))));

            // Register for the events.
            loggerEvents.TestResult += (sender, e) =>
            {
                testResultReceived = true;
            };

            loggerEvents.TestRunMessage += (sender, e) =>
            {
                testMessageReceived = true;
            };

            // Enable events and verify that the events are received.
            loggerEvents.EnableEvents();

            Assert.IsTrue(testResultReceived);
            Assert.IsTrue(testMessageReceived);
        }

        [TestMethod]
        public void DisposeShouldNotThrowExceptionIfCalledMultipleTimes()
        {
            var loggerEvents = GetDisposedLoggerEvents();
            loggerEvents.Dispose();
        }

        [TestMethod]
        public void RaiseTestResultShouldThrowExceptionIfDisposedIsAlreadyCalled()
        {
            var loggerEvents = GetDisposedLoggerEvents();

            Assert.ThrowsException<ObjectDisposedException>(() =>
            {
                loggerEvents.RaiseTestResult(new TestResultEventArgs(new TestResult(new TestCase("This is a string.", new Uri("some://uri"), "DummySourceFileName"))));
            });
        }

        [TestMethod]
        public void RaiseMessageShouldThrowExceptionIfDisposeIsAlreadyCalled()
        {
            var loggerEvents = GetDisposedLoggerEvents();

            Assert.ThrowsException<ObjectDisposedException>(() =>
            {
                loggerEvents.RaiseMessage(new TestRunMessageEventArgs(TestMessageLevel.Error,"This is a string."));
            });
        }

        [TestMethod]
        public void CompleteTestRunShouldThrowExceptionIfAlreadyDisposed()
        {
            var loggerEvents = GetDisposedLoggerEvents();

            Assert.ThrowsException<ObjectDisposedException>(() =>
            {
                loggerEvents.CompleteTestRun(null, true, false, null, null, new TimeSpan());
            });
        }

        [TestMethod]
        public void EnableEventsShouldThrowExceptionIfAlreadyDisposed()
        {
            var loggerEvents = GetDisposedLoggerEvents();

            Assert.ThrowsException<ObjectDisposedException>(() =>
            {
                loggerEvents.EnableEvents();
            });
        }

        [TestMethod]
        public void TestRunMessageLoggerProxySendMessageShouldInvokeRegisteredEventHandler()
        {
            var receivedRunMessage = false;
            using (loggerEvents)
            {
                loggerEvents.TestRunMessage += (sender, e) =>
                {
                    receivedRunMessage = true;
                };

                testSessionMessageLogger.SendMessage(TestMessageLevel.Error,"This is a string.");
            }

            Assert.IsTrue(receivedRunMessage);
        }

        [TestMethod]
        public void TestLoggerProxySendMessageShouldNotInvokeRegisterdEventHandlerIfAlreadyDisposed()
        {
            var receivedRunMessage = false;
            loggerEvents.TestRunMessage += (sender, e) =>
            {
                receivedRunMessage = true;
            };

            // Dispose the logger events, send a message, and verify it is not received.
            loggerEvents.Dispose();
            testSessionMessageLogger.SendMessage(TestMessageLevel.Error,"This is a string.");

            Assert.IsFalse(receivedRunMessage);
        }
        /// <summary>
        /// Gets a disposed instance of the logger events.
        /// </summary>
        /// <returns>Disposed instance.</returns>
        private InternalTestLoggerEvents GetDisposedLoggerEvents()
        {
            var loggerEvents = new InternalTestLoggerEvents(testSessionMessageLogger);
            loggerEvents.Dispose();

            return loggerEvents;
        }
    }



}