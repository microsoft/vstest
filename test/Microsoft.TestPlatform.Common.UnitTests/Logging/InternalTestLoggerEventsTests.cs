// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.Common.UnitTests.Logging
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    using Microsoft.VisualStudio.TestPlatform.Common.Filtering;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

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
        /// Exception should be thrown if event args passed is null.
        /// </summary>
        [TestMethod]
        public void RaiseDiscoveryStartShouldThrowExceptionIfNullDiscoveryStartEventArgsIsPassed()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                loggerEvents.RaiseDiscoveryStart(null);
            });
        }

        /// <summary>
        /// Exception should be thrown if logger events are already disposed.
        /// </summary>
        [TestMethod]
        public void RaiseDiscoveryStartShouldThrowExceptionIfAlreadyDisposed()
        {
            var loggerEvents = GetDisposedLoggerEvents();
            DiscoveryCriteria discoveryCriteria = new DiscoveryCriteria() { TestCaseFilter = "Name=Test1" };
            TestCaseFilterExpression testCaseFilter = new TestCaseFilterExpression(new FilterExpressionWrapper("Name=Test2"));
            DiscoveryStartEventArgs discoveryStartEventArgs = new DiscoveryStartEventArgs(discoveryCriteria, testCaseFilter);

            Assert.ThrowsException<ObjectDisposedException>(() =>
            {
                loggerEvents.RaiseDiscoveryStart(discoveryStartEventArgs);
            });
        }

        /// <summary>
        /// Check for invocation to registered event handlers.
        /// </summary>
        [TestMethod]
        public void RaiseDiscoveryStartShouldInvokeRegisteredEventHandler()
        {
            bool discoveryStartReceived = false;
            DiscoveryStartEventArgs receivedEventArgs = null;
            EventWaitHandle waitHandle = new AutoResetEvent(false);

            DiscoveryCriteria discoveryCriteria = new DiscoveryCriteria() { TestCaseFilter = "Name=Test1" };
            TestCaseFilterExpression testCaseFilter = new TestCaseFilterExpression(new FilterExpressionWrapper("Name=Test2"));
            DiscoveryStartEventArgs discoveryStartEventArgs = new DiscoveryStartEventArgs(discoveryCriteria, testCaseFilter);

            // Register for the discovery start event.
            loggerEvents.DiscoveryStart += (sender, e) =>
            {
                discoveryStartReceived = true;
                receivedEventArgs = e;
                waitHandle.Set();
            };

            loggerEvents.EnableEvents();
            // Send the discovery start event.
            loggerEvents.RaiseDiscoveryStart(discoveryStartEventArgs);

            var waitSuccess = waitHandle.WaitOne(500);
            Assert.IsTrue(waitSuccess, "Event must be raised within timeout.");
            Assert.IsTrue(discoveryStartReceived);
            Assert.IsNotNull(receivedEventArgs);
            Assert.AreEqual(receivedEventArgs, discoveryStartEventArgs);
            Assert.AreEqual("Name=Test2", receivedEventArgs.FilterExpression.TestCaseFilterValue);
            Assert.AreEqual("Name=Test1", receivedEventArgs.DiscoveryCriteria.TestCaseFilter);
        }

        /// <summary>
        /// Exception should be thrown if event args passed is null.
        /// </summary>
        [TestMethod]
        public void RaiseTestRunStartShouldThrowExceptionIfNullTestRunStartEventArgsIsPassed()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                loggerEvents.RaiseTestRunStart(null);
            });
        }

        /// <summary>
        /// Exception should be thrown if logger events are already disposed.
        /// </summary>
        [TestMethod]
        public void RaiseTestRunStartShouldThrowExceptionIfAlreadyDisposed()
        {
            var loggerEvents = GetDisposedLoggerEvents();
            TestRunCriteria testRunCriteria = new TestRunCriteria(new List<string> { @"x:dummy\foo.dll" }, 10) { TestCaseFilter = "Name=Test1" };
            TestRunStartEventArgs testRunStartEventArgs = new TestRunStartEventArgs(testRunCriteria);

            Assert.ThrowsException<ObjectDisposedException>(() =>
            {
                loggerEvents.RaiseTestRunStart(testRunStartEventArgs);
            });
        }

        /// <summary>
        /// Check for invocation to registered event handlers.
        /// </summary>
        [TestMethod]
        public void RaiseTestRunStartShouldInvokeRegisteredEventHandler()
        {
            bool testRunStartReceived = false;
            TestRunStartEventArgs receivedEventArgs = null;
            EventWaitHandle waitHandle = new AutoResetEvent(false);

            TestRunCriteria testRunCriteria = new TestRunCriteria(new List<string> { @"x:dummy\foo.dll" }, 10) { TestCaseFilter = "Name=Test1" };
            TestRunStartEventArgs testRunStartEventArgs = new TestRunStartEventArgs(testRunCriteria);

            // Register for the test run start event.
            loggerEvents.TestRunStart += (sender, e) =>
            {
                testRunStartReceived = true;
                receivedEventArgs = e;
                waitHandle.Set();
            };

            loggerEvents.EnableEvents();
            // Send the test run start event.
            loggerEvents.RaiseTestRunStart(testRunStartEventArgs);

            var waitSuccess = waitHandle.WaitOne(500);
            Assert.IsTrue(waitSuccess, "Event must be raised within timeout.");
            Assert.IsTrue(testRunStartReceived);
            Assert.IsNotNull(receivedEventArgs);
            Assert.AreEqual(receivedEventArgs, testRunStartEventArgs);
            Assert.AreEqual("Name=Test1", receivedEventArgs.TestRunCriteria.TestCaseFilter);
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