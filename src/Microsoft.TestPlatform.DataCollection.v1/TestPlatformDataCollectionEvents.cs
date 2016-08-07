// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.DataCollection.V1
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Threading;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestTools.Common;
    using Microsoft.VisualStudio.TestTools.DataCollection;
    using Microsoft.VisualStudio.TestTools.Execution;

    /// <summary>
    /// Class defining execution events that will be registered for by collectors
    /// </summary>
    internal sealed class TestPlatformDataCollectionEvents : DataCollectionEvents
    {
        #region Fields

        /// <summary>
        /// A factory for creating user work items
        /// </summary>
        private readonly SafeAbortableUserWorkItemFactory userWorkItemFactory;

        /// <summary>
        /// Maps the type of event args to the multicast delegate for that event
        /// </summary>
        private Dictionary<Type, EventInvoker> eventArgsToEventInvokerMap;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="TestPlatformDataCollectionEvents"/> class by mapping the types of expected event args to the multicast
        /// delegate that invokes the event on registered targets
        /// </summary>
        /// <param name="userWorkItemFactory">
        /// A factory for creating user work items.
        /// The user work items are used to invoke delegates on data collectors on worker threads.
        /// </param>
        internal TestPlatformDataCollectionEvents(SafeAbortableUserWorkItemFactory userWorkItemFactory)
        {
            EqtAssert.ParameterNotNull(userWorkItemFactory, "userWorkItemFactory");
            this.userWorkItemFactory = userWorkItemFactory;

            this.eventArgsToEventInvokerMap = new Dictionary<Type, EventInvoker>(14);

            this.eventArgsToEventInvokerMap[typeof(SessionStartEventArgs)] = this.OnSessionStart;
            this.eventArgsToEventInvokerMap[typeof(SessionEndEventArgs)] = this.OnSessionEnd;
            this.eventArgsToEventInvokerMap[typeof(SessionPauseEventArgs)] = this.OnSessionPause;
            this.eventArgsToEventInvokerMap[typeof(SessionResumeEventArgs)] = this.OnSessionResume;
            this.eventArgsToEventInvokerMap[typeof(TestCaseStartEventArgs)] = this.OnTestCaseStart;
            this.eventArgsToEventInvokerMap[typeof(TestCaseEndEventArgs)] = this.OnTestCaseEnd;
            this.eventArgsToEventInvokerMap[typeof(TestCasePauseEventArgs)] = this.OnTestCasePause;
            this.eventArgsToEventInvokerMap[typeof(TestCaseResumeEventArgs)] = this.OnTestCaseResume;
            this.eventArgsToEventInvokerMap[typeof(TestCaseResetEventArgs)] = this.OnTestCaseReset;
            this.eventArgsToEventInvokerMap[typeof(TestCaseFailedEventArgs)] = this.OnTestCaseFailed;
            this.eventArgsToEventInvokerMap[typeof(TestStepStartEventArgs)] = this.OnTestStepStart;
            this.eventArgsToEventInvokerMap[typeof(TestStepEndEventArgs)] = this.OnTestStepEnd;
            this.eventArgsToEventInvokerMap[typeof(DataRequestEventArgs)] = this.OnDataRequest;
            this.eventArgsToEventInvokerMap[typeof(WrapperCustomNotificationEventArgs)] = this.OnCustomNotification;
        }

        #endregion

        /// <summary>
        /// Delegate for the event invoker methods (OnSessionStart, OnTestCaseResume, etc.)
        /// </summary>
        /// <param name="e">Contains the event data</param>
        /// <returns>List of invocation errors that occurred while raising the event</returns>
        private delegate List<DataCollectorInvocationError> EventInvoker(DataCollectionEventArgs e);

        #region Events

        #region Session events

        /// <summary>
        /// Raised when a session is starting
        /// </summary>
        public override event EventHandler<SessionStartEventArgs> SessionStart;

        /// <summary>
        /// Raised when a session is ending
        /// </summary>
        public override event EventHandler<SessionEndEventArgs> SessionEnd;

        /// <summary>
        /// Raised when a session is paused
        /// </summary>
        public override event EventHandler<SessionPauseEventArgs> SessionPause;

        /// <summary>
        /// Raised when a session is resuming
        /// </summary>
        public override event EventHandler<SessionResumeEventArgs> SessionResume;
        #endregion

        #region Test case events

        /// <summary>
        /// Raised when a test case is starting
        /// </summary>
        public override event EventHandler<TestCaseStartEventArgs> TestCaseStart;

        /// <summary>
        /// Raised when a test case is ending
        /// </summary>
        public override event EventHandler<TestCaseEndEventArgs> TestCaseEnd;

        /// <summary>
        /// Raised when a test case is pausing
        /// </summary>
        public override event EventHandler<TestCasePauseEventArgs> TestCasePause;

        /// <summary>
        /// Raised when a test case is resuming
        /// </summary>
        public override event EventHandler<TestCaseResumeEventArgs> TestCaseResume;

        /// <summary>
        /// Raised when a test case is reset
        /// </summary>
        public override event EventHandler<TestCaseResetEventArgs> TestCaseReset;

        /// <summary>
        /// Raised when a test case has failed.
        /// </summary>
        /// <remarks>
        /// This event is only raised for test types which send test failure notifications.
        /// </remarks>
        public override event EventHandler<TestCaseFailedEventArgs> TestCaseFailed;

        #endregion

        #region Test step events

        /// <summary>
        /// Raised when a test step is starting
        /// </summary>
        public override event EventHandler<TestStepStartEventArgs> TestStepStart;

        /// <summary>
        /// Raised when a test step is ending
        /// </summary>
        public override event EventHandler<TestStepEndEventArgs> TestStepEnd;

        #endregion

        #region Other events

        /// <summary>
        /// Raised when intermediate data is requested. Can be a test case-specific event, or just
        /// a session event. When sent with a test case-specific context, intermediate data for the
        /// test case is requested, and when sent with only a session-specific context,
        /// intermediate data for a session is requested.
        /// </summary>
        public override event EventHandler<DataRequestEventArgs> DataRequest;

        /// <summary>
        /// Raised on a custom notification
        /// </summary>
        public override event EventHandler<CustomNotificationEventArgs> CustomNotification;

        #endregion

        #endregion

        #region Methods

        /// <summary>
        /// Raises the event corresponding to the event arguments to all registered handlers
        /// </summary>
        /// <param name="e">Contains the event data</param>
        /// <returns>List of invocation errors that occurred while raising the event</returns>
        internal List<DataCollectorInvocationError> RaiseEvent(DataCollectionEventArgs e)
        {
            Debug.Assert(e != null, "'e' is null");

            EventInvoker onEvent;
            if (this.eventArgsToEventInvokerMap.TryGetValue(e.GetType(), out onEvent))
            {
                return onEvent(e);
            }
            else if (e.GetType().IsSubclassOf(typeof(CustomNotificationEventArgs)))
            {
                return this.OnCustomNotification(e);
            }
            else
            {
                EqtTrace.Fail("InternalDataCollectionEvents.RaiseEvent: Unrecognized data collection event of type {0}.", e.GetType().FullName);
            }

            return new List<DataCollectorInvocationError>();
        }

        /// <summary>
        /// Returns whether there is any event listener for test case start/end/failed events. 
        /// </summary>
        /// <param name="valueOnFailure">
        /// The value On Failure.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        internal bool HasTestCaseStartOrEndOrFailedEventListener(bool valueOnFailure)
        {
            return HasEventListener(this.TestCaseStart, valueOnFailure) || HasEventListener(this.TestCaseEnd, valueOnFailure) || HasEventListener(this.TestCaseFailed, valueOnFailure);
        }

        /// <summary>
        /// Checks whether parameter event has any listener or not
        /// </summary>
        /// <param name="eventToCheck">
        /// The event To Check.
        /// </param>
        /// <param name="valueOnFailure">
        /// The value On Failure.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private static bool HasEventListener(MulticastDelegate eventToCheck, bool valueOnFailure)
        {
            try
            {
                if (eventToCheck == null)
                {
                    return false;
                }

                Delegate[] listeners = eventToCheck.GetInvocationList();
                if (listeners == null || listeners.Count() == 0)
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                EqtTrace.Error("Exception occured while checking whether event {0} has any listeners or not. {1}", eventToCheck, ex);
                return valueOnFailure;
            }
        }

        /// <summary>
        /// Raises the SessionStart event
        /// </summary>
        /// <param name="e">Contains the event data</param>
        /// <returns>List of invocation errors that occurred while raising the event</returns>
        private List<DataCollectorInvocationError> OnSessionStart(DataCollectionEventArgs e)
        {
            return this.On(this.SessionStart, e);
        }

        /// <summary>
        /// Raises the SessionEnd event
        /// </summary>
        /// <param name="e">Contains the event data</param>
        /// <returns>List of invocation errors that occurred while raising the event</returns>
        private List<DataCollectorInvocationError> OnSessionEnd(DataCollectionEventArgs e)
        {
            return this.On(this.SessionEnd, e);
        }

        /// <summary>
        /// Raises the SessionPause event
        /// </summary>
        /// <param name="e">Contains the event data</param>
        /// <returns>List of invocation errors that occurred while raising the event</returns>
        private List<DataCollectorInvocationError> OnSessionPause(DataCollectionEventArgs e)
        {
            return this.On(this.SessionPause, e);
        }

        /// <summary>
        /// Raises the SessionResume event
        /// </summary>
        /// <param name="e">Contains the event data</param>
        /// <returns>List of invocation errors that occurred while raising the event</returns>
        private List<DataCollectorInvocationError> OnSessionResume(DataCollectionEventArgs e)
        {
            return this.On(this.SessionResume, e);
        }

        /// <summary>
        /// Raises the TestCaseStart event
        /// </summary>
        /// <param name="e">Contains the event data</param>
        /// <returns>List of invocation errors that occurred while raising the event</returns>
        private List<DataCollectorInvocationError> OnTestCaseStart(DataCollectionEventArgs e)
        {
            return this.On(this.TestCaseStart, e);
        }

        /// <summary>
        /// Raises the TestCaseEnd event
        /// </summary>
        /// <param name="e">Contains the event data</param>
        /// <returns>List of invocation errors that occurred while raising the event</returns>
        private List<DataCollectorInvocationError> OnTestCaseEnd(DataCollectionEventArgs e)
        {
            return this.On(this.TestCaseEnd, e);
        }

        /// <summary>
        /// Raises the TestCasePause event
        /// </summary>
        /// <param name="e">Contains the event data</param>
        /// <returns>List of invocation errors that occurred while raising the event</returns>
        private List<DataCollectorInvocationError> OnTestCasePause(DataCollectionEventArgs e)
        {
            return this.On(this.TestCasePause, e);
        }

        /// <summary>
        /// Raises the TestCaseResume event
        /// </summary>
        /// <param name="e">Contains the event data</param>
        /// <returns>List of invocation errors that occurred while raising the event</returns>
        private List<DataCollectorInvocationError> OnTestCaseResume(DataCollectionEventArgs e)
        {
            return this.On(this.TestCaseResume, e);
        }

        /// <summary>
        /// Raises the TestCaseReset event
        /// </summary>
        /// <param name="e">Contains the event data</param>
        /// <returns>List of invocation errors that occurred while raising the event</returns>
        private List<DataCollectorInvocationError> OnTestCaseReset(DataCollectionEventArgs e)
        {
            return this.On(this.TestCaseReset, e);
        }

        /// <summary>
        /// Raises the TestCaseFailed event
        /// </summary>
        /// <param name="e">Contains the event data</param>
        /// <returns>List of invocation errors that occurred while raising the event</returns>
        private List<DataCollectorInvocationError> OnTestCaseFailed(DataCollectionEventArgs e)
        {
            return this.On(this.TestCaseFailed, e);
        }

        /// <summary>
        /// Raises the TestStepStart event
        /// </summary>
        /// <param name="e">Contains the event data</param>
        /// <returns>List of invocation errors that occurred while raising the event</returns>
        private List<DataCollectorInvocationError> OnTestStepStart(DataCollectionEventArgs e)
        {
            return this.On(this.TestStepStart, e);
        }

        /// <summary>
        /// Raises the TestStepEnd event
        /// </summary>
        /// <param name="e">Contains the event data</param>
        /// <returns>List of invocation errors that occurred while raising the event</returns>
        private List<DataCollectorInvocationError> OnTestStepEnd(DataCollectionEventArgs e)
        {
            return this.On(this.TestStepEnd, e);
        }

        /// <summary>
        /// Raises the DataRequest event
        /// </summary>
        /// <param name="e">Contains the event data</param>
        /// <returns>List of invocation errors that occurred while raising the event</returns>
        private List<DataCollectorInvocationError> OnDataRequest(DataCollectionEventArgs e)
        {
            return this.On(this.DataRequest, e);
        }

        /// <summary>
        /// Raises the CustomNotification event
        /// </summary>
        /// <param name="e">Contains the event data</param>
        /// <returns>List of invocation errors that occurred while raising the event</returns>
        private List<DataCollectorInvocationError> OnCustomNotification(DataCollectionEventArgs e)
        {
            // If this is a wrapper around the custom notification event arguments,
            // then extract the actual event arguments.
            var wrapperArgs = e as WrapperCustomNotificationEventArgs;
            if (wrapperArgs != null)
            {
                // If the custom notification event arguments is null,
                // the it can not be deseralized and we can not raise the event.
                if (wrapperArgs.CustomNotificationEventArgs == null)
                {
                    return new List<DataCollectorInvocationError>();
                }

                e = wrapperArgs.CustomNotificationEventArgs;
            }

            return this.On(this.CustomNotification, e);
        }

        /// <summary>
        /// Raises the event represented by the multicast delegate with the event data
        /// </summary>
        /// <param name="multicastDel">The multicast delegate representing the event to raise</param>
        /// <param name="e">Contains the event data</param>
        /// <returns>List of invocation errors that occurred while raising the event</returns>
        private List<DataCollectorInvocationError> On(MulticastDelegate multicastDel, DataCollectionEventArgs e)
        {
            MulticastDelegateInvoker invoker = new MulticastDelegateInvoker(multicastDel, e, this.userWorkItemFactory);
            return invoker.Invoke();
        }
        #endregion

        #region Types
        
        /// <summary>
        /// A helper class for accumulating the errors that occur while invoking
        /// the delegates in a <see cref="MulticaseDelegate"/>.
        /// </summary>
        private sealed class MulticastDelegateInvoker
        {
            #region Fields
            /// <summary>
            /// The multicast delegate being invoked
            /// </summary>
            private readonly MulticastDelegate multicastDelegate;

            /// <summary>
            /// The event args being passed as an argument to each delegate
            /// </summary>
            private readonly DataCollectionEventArgs eventArgs;

            /// <summary>
            /// A factory for creating user work items
            /// </summary>
            private readonly SafeAbortableUserWorkItemFactory userWorkItemFactory;

            /// <summary>
            /// The errors that have occurred while invoking the delegates
            /// </summary>
            private readonly List<DataCollectorInvocationError> invocationErrors;

            /// <summary>
            /// True if the time out value for invoking all of the delegates has been surpassed.
            /// </summary>
            private volatile bool timeout;
            #endregion Fields

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the <see cref="MulticastDelegateInvoker"/> class. 
            /// Constructor
            /// </summary>
            /// <param name="multicastDelegate">
            /// The multicast delegate being invoked
            /// </param>
            /// <param name="eventArgs">
            /// The event args being passed as an argument to each delegate
            /// </param>
            /// <param name="userWorkItemFactory">
            /// A factory for creating user work items.
            /// The user work items are used to invoke the delegates on worker threads.
            /// </param>
            public MulticastDelegateInvoker(
                MulticastDelegate multicastDelegate,
                DataCollectionEventArgs eventArgs,
                SafeAbortableUserWorkItemFactory userWorkItemFactory)
            {
                EqtAssert.ParameterNotNull(userWorkItemFactory, "userWorkItemFactory");

                this.multicastDelegate = multicastDelegate;
                this.eventArgs = eventArgs;
                this.userWorkItemFactory = userWorkItemFactory;
                this.invocationErrors = new List<DataCollectorInvocationError>();
            }
            #endregion Constructors            

            #region Public Methods

            /// <summary>
            /// Invokes the delegates in the multicast delegate in parallel,
            /// passing the supplied event args as an argument.
            /// </summary>
            /// <returns>a list of the errors that occurred while invoking the delegates</returns>
            public List<DataCollectorInvocationError> Invoke()
            {
                if (this.multicastDelegate != null)
                {
                    // A list of events on which we will wait after invoking the delegates asynchronously
                    var waitEvents = new List<ManualResetEvent>();

                    // A dictionary to keep track of the user work items and corresponding delegates
                    var userWorkItems = new Dictionary<ISafeAbortableUserWorkItem, Delegate>();

                    // Iterate through each delegate in the multicast delegate invocation list, and invoke
                    // each one individually, collecting any errors that occurred during the invocations
                    foreach (Delegate del in this.multicastDelegate.GetInvocationList())
                    {
                        ISafeAbortableUserWorkItem newUserWorkItem;
                        ManualResetEvent waitEvent = this.InvokeDelegateAsync(del, out newUserWorkItem);
                        waitEvents.Add(waitEvent);
                        userWorkItems[newUserWorkItem] = del;
                    }

                    if (!WaitHandle.WaitAll(waitEvents.ToArray(), ExecutionPluginManager.DataCollectionEventTimeout, false))
                    {
                        this.HandleTimeout(userWorkItems);
                    }
                }

                return this.invocationErrors;
            }
            #endregion Public Methods

            #region Private Methods            

            /// <summary>
            /// Invokes a single delegate in the multicast delegate on a thread pool thread.
            /// </summary>
            /// <param name="del">The delegate to be invoked</param>
            /// <param name="newUserWorkItem">The user work item that is created to execute the delegate</param>
            /// <returns>A wait event that will be set when the invocation of the delegate is complete.
            /// The caller is responsible for closing this handle.</returns>
            private ManualResetEvent InvokeDelegateAsync(
                Delegate del,
                out ISafeAbortableUserWorkItem newUserWorkItem)
            {
                var completeEvent = new ManualResetEvent(false);

                var traceMessageFormat =
                    "DataCollectionEvents: {0} event " +
                    string.Format(
                            CultureInfo.CurrentCulture,
                            "'{0}' to '{1}'",
                            del,
                        del.Target == null ? "(static)" : del.Target);

                WaitCallback invokeDelegate = state =>
                {
                    EqtTrace.Verbose(traceMessageFormat, "Raising");
                    del.DynamicInvoke(this, this.eventArgs);
                    EqtTrace.Verbose(traceMessageFormat, "Raised");
                };

                ISafeAbortableUserWorkItem userWorkItem = this.userWorkItemFactory.Create(invokeDelegate);

                userWorkItem.Name = string.Format(CultureInfo.CurrentCulture, traceMessageFormat, "Raising");
                userWorkItem.Complete += (sender, e) =>
                {
                    try
                    {
                        if (userWorkItem.Exception != null)
                        {
                            this.HandleException(del, userWorkItem.Exception);
                        }

                        completeEvent.Set();
                    }
                    catch (ObjectDisposedException)
                    {
                        // Delegate already timed out
                    }
                };

                newUserWorkItem = userWorkItem;

                userWorkItem.Queue();

                return completeEvent;
            }

            /// <summary>
            /// Handles the scenario in which not all delegates have completed prior to the time out interval expiring.
            /// </summary>
            /// <param name="userWorkItems">the user work items used to execute the delegates on thread pool threads.</param>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
            private void HandleTimeout(Dictionary<ISafeAbortableUserWorkItem, Delegate> userWorkItems)
            {
                EqtTrace.Warning("DataCollectionEvents: Timed out raising event '{0}'", this.eventArgs == null ? "(null)" : this.eventArgs.GetType().Name);

                // Record an error for each invocation that timed out
                foreach (var keyValuePair in userWorkItems.Where(pair => !pair.Key.IsComplete))
                {
                    this.AddTimeoutError(keyValuePair.Value);
                }

                // Abort any threads that have not completed.  This can be done on a background thread.
                ThreadPool.QueueUserWorkItem(state =>
                {
                    foreach (var userWorkItem in userWorkItems.Keys)
                    {
                        if (!userWorkItem.IsComplete)
                        {
                            try
                            {
                                userWorkItem.Abort();
                            }
                            catch (Exception ex)
                            {
                                EqtTrace.Warning(
                                    "DataCollectionEvents: Exception while calling Abort on ISafeAbortableUserWorkItem '{0}': {1}",
                                    userWorkItem.Name,
                                    ex);
                            }
                        }
                    }
                });
            }
           
            /// <summary>
            /// Handles an exception thrown by the invocation of a delegate.
            /// </summary>
            /// <param name="del">the delegate that threw an exception</param>
            /// <param name="ex">the exception that was thrown by the delegate</param>
            private void HandleException(Delegate del, Exception ex)
            {
                // Create an appropriate message, and unwrap the exception if it is a TargetInvocationException
                string message;
                var targetInvocationException = ex as TargetInvocationException;
                if (targetInvocationException != null)
                {
                    ex = targetInvocationException.InnerException;
                    message = "DataCollectionEvents: Target '{0}' event handler '{1}' invoked with event args '{2}' threw exception: {3}";
                }
                else
                {
                    message = "DataCollectionEvents: Exception occurred while invoking target '{0}' event handler '{1}' with event args '{2}': {3}";
                }

                EqtTrace.Warning(
                    message,
                    del.Target == null ? "(static)" : del.Target.GetType().FullName,
                    del.Method.Name,
                    this.eventArgs == null ? "(null)" : this.eventArgs.GetType().FullName,
                    ex);

                this.AddError(new DataCollectorInvocationError(del, this.eventArgs, ex));
            }

            /// <summary>
            /// Adds an invocation error.  Once the time out value for invoking all of the delegates
            /// has been surpassed, this method becomes a no-op. 
            /// </summary>
            /// <param name="error">the invocation error to be added to the list of errors</param>
            private void AddError(DataCollectorInvocationError error)
            {
                // After we've timed out, threads may still complete and attempt to report an invocation error.
                // Since we don't our error list to be modified once we've returned to our caller,
                // don't allow such errors to be added to the list.  Rather, add an error recording each
                // thread that timed out.
                if (!this.timeout)
                {
                    this.AddErrorImpl(error);
                }
            }

            /// <summary>
            /// Adds an invocation error to indicate that the given delegate has timed out.
            /// </summary>
            /// <param name="del">the delegate that timed out</param>
            private void AddTimeoutError(Delegate del)
            {
                this.timeout = true;

                EqtTrace.Error(
                    "DataCollectionEvents: Timed out invoking target '{0}' event handler '{1}' with event args '{2}'",
                    del.Target == null ? "(static)" : del.Target.GetType().FullName,
                    del.Method.Name,
                    this.eventArgs == null ? "(null)" : this.eventArgs.GetType().FullName);

                var error = new DataCollectorInvocationError(
                    del,
                    this.eventArgs,
                    new TimeoutException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            Resource.Execution_DataCollectorEventTimeout,
                            del.Method.Name)));

                this.AddErrorImpl(error);
            }

            /// <summary>
            /// Implementation method for adding an invocation error
            /// </summary>
            /// <param name="error">the invocation error to be added to the list of errors</param>
            private void AddErrorImpl(DataCollectorInvocationError error)
            {
                lock (this.invocationErrors)
                {
                    this.invocationErrors.Add(error);
                }
            }            

            #endregion Private Methods
        }
        #endregion
    }
}