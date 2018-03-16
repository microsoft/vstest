// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollector
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    /// <summary>
    /// The test platform data collection events.
    /// </summary>
    internal sealed class TestPlatformDataCollectionEvents : DataCollectionEvents
    {
        /// <summary>
        /// Maps the type of event args to the multicast delegate for that event
        /// </summary>
        private Dictionary<Type, EventInvoker> eventArgsToEventInvokerMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestPlatformDataCollectionEvents"/> class by mapping the types of expected event args to the multicast
        /// delegate that invokes the event on registered targets
        /// </summary>
        internal TestPlatformDataCollectionEvents()
        {
            this.eventArgsToEventInvokerMap = new Dictionary<Type, EventInvoker>(4);

            this.eventArgsToEventInvokerMap[typeof(TestHostLaunchedEventArgs)] = this.OnTestHostLaunched;
            this.eventArgsToEventInvokerMap[typeof(SessionStartEventArgs)] = this.OnSessionStart;
            this.eventArgsToEventInvokerMap[typeof(SessionEndEventArgs)] = this.OnSessionEnd;
            this.eventArgsToEventInvokerMap[typeof(TestCaseStartEventArgs)] = this.OnTestCaseStart;
            this.eventArgsToEventInvokerMap[typeof(TestCaseEndEventArgs)] = this.OnTestCaseEnd;
        }

        /// <summary>
        /// Delegate for the event invoker methods (OnSessionStart, OnTestCaseResume, etc.)
        /// </summary>
        /// <param name="e">
        /// Contains the event data
        /// </param>
        private delegate void EventInvoker(DataCollectionEventArgs e);

        /// <summary>
        /// Raised when test host process has initialized
        /// </summary>
        public override event EventHandler<TestHostLaunchedEventArgs> TestHostLaunched;

        /// <summary>
        /// Raised when a session is starting
        /// </summary>
        public override event EventHandler<SessionStartEventArgs> SessionStart;

        /// <summary>
        /// Raised when a session is ending
        /// </summary>
        public override event EventHandler<SessionEndEventArgs> SessionEnd;

        /// <summary>
        /// Raised when a test case is starting
        /// </summary>
        public override event EventHandler<TestCaseStartEventArgs> TestCaseStart;

        /// <summary>
        /// Raised when a test case is ending
        /// </summary>
        public override event EventHandler<TestCaseEndEventArgs> TestCaseEnd;

        /// <summary>
        /// Raises the event corresponding to the event arguments to all registered handlers
        /// </summary>
        /// <param name="e">
        /// Contains the event data
        /// </param>
        internal void RaiseEvent(DataCollectionEventArgs e)
        {
            ValidateArg.NotNull<DataCollectionEventArgs>(e, "DataCollectionEventArgs");

            EventInvoker onEvent;

            if (this.eventArgsToEventInvokerMap.TryGetValue(e.GetType(), out onEvent))
            {
                onEvent(e);
            }
            else
            {
                EqtTrace.Fail("TestPlatformDataCollectionEvents.RaiseEvent: Unrecognized data collection event of type {0}.", e.GetType().FullName);
            }
        }

        /// <summary>
        /// Checks whether any data collector has subscribed for test case events.
        /// </summary>
        internal bool AreTestCaseEventsSubscribed()
        {
            bool valueOnFailure = false;
            return (HasEventListener(this.TestCaseStart, valueOnFailure) || HasEventListener(this.TestCaseEnd, valueOnFailure));
        }

        private bool HasEventListener(MulticastDelegate eventToCheck, bool valueOnFailure)
        {
            try
            {
                if (eventToCheck == null)
                {
                    return false;
                }

                Delegate[] listeners = eventToCheck.GetInvocationList();
                if (listeners == null || listeners.Length == 0)
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                if (EqtTrace.IsErrorEnabled)
                {
                    EqtTrace.Error("TestPlatformDataCollectionEvents.AreTestCaseLevelEventsRequired: Exception occured while checking whether event {0} has any listeners or not. {1}", eventToCheck, ex);
                }

                return valueOnFailure;
            }
        }

        /// <summary>
        /// Raises the TestHostLaunched event
        /// </summary>
        /// <param name="e">
        /// Contains the event data
        /// </param>
        private void OnTestHostLaunched(DataCollectionEventArgs e)
        {
            this.TestHostLaunched.SafeInvoke(this, e, "DataCollectionEvents.TestHostLaunched");
        }

        /// <summary>
        /// Raises the SessionStart event
        /// </summary>
        /// <param name="e">
        /// Contains the event data
        /// </param>
        private void OnSessionStart(DataCollectionEventArgs e)
        {
            this.SessionStart.SafeInvoke(this, e, "DataCollectionEvents.SessionStart");
        }

        /// <summary>
        /// Raises the SessionEnd event
        /// </summary>
        /// <param name="e">
        /// Contains the event data
        /// </param>
        private void OnSessionEnd(DataCollectionEventArgs e)
        {
            this.SessionEnd.SafeInvoke(this, e, "DataCollectionEvents.SessionEnd");
        }

        /// <summary>
        /// Raises the TestCaseStart event
        /// </summary>
        /// <param name="e">
        /// Contains the event data
        /// </param>
        private void OnTestCaseStart(DataCollectionEventArgs e)
        {
            this.TestCaseStart.SafeInvoke(this, e, "DataCollectionEvents.TestCaseStart");
        }

        /// <summary>
        /// Raises the TestCaseEnd event
        /// </summary>
        /// <param name="e">
        /// Contains the event data
        /// </param>
        private void OnTestCaseEnd(DataCollectionEventArgs e)
        {
            this.TestCaseEnd.SafeInvoke(this, e, "DataCollectionEvents.TestCaseEnd");
        }
    }
}
