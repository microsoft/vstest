// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection
{
    using System;

    /// <summary>
    /// Class defining execution events that will be registered for by collectors
    /// </summary>
    public abstract class DataCollectionEvents
    {
        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        protected DataCollectionEvents()
        {
        }

        #endregion

        #region Events

        #region Session events

        /// <summary>
        /// Raised when a session is starting
        /// </summary>
        public abstract event EventHandler<SessionStartEventArgs> SessionStart;

        /// <summary>
        /// Raised when a session is ending
        /// </summary>
        public abstract event EventHandler<SessionEndEventArgs> SessionEnd;

        /// <summary>
        /// Raised when a session is paused
        /// </summary>
        public abstract event EventHandler<SessionPauseEventArgs> SessionPause;

        /// <summary>
        /// Raised when a session is resuming
        /// </summary>
        public abstract event EventHandler<SessionResumeEventArgs> SessionResume;
        #endregion

        #region Test case events

        /// <summary>
        /// Raised when a test case is starting
        /// </summary>
        public abstract event EventHandler<TestCaseStartEventArgs> TestCaseStart;

        /// <summary>
        /// Raised when a test case is ending
        /// </summary>
        public abstract event EventHandler<TestCaseEndEventArgs> TestCaseEnd;

        /// <summary>
        /// Raised when a test case is pausing
        /// </summary>
        public abstract event EventHandler<TestCasePauseEventArgs> TestCasePause;

        /// <summary>
        /// Raised when a test case is resuming
        /// </summary>
        public abstract event EventHandler<TestCaseResumeEventArgs> TestCaseResume;

        /// <summary>
        /// Raised when a test case is reset
        /// </summary>
        public abstract event EventHandler<TestCaseResetEventArgs> TestCaseReset;

        /// <summary>
        /// Raised when a test case has failed.
        /// </summary>
        /// <remarks>
        /// This event is only raised for test types which send test failure notifications.
        /// </remarks>
        public abstract event EventHandler<TestCaseFailedEventArgs> TestCaseFailed;

        #endregion

        #region Other events

        /// <summary>
        /// Raised when intermediate data is requested. Can be a test case-specific event, or just
        /// a session event. When sent with a test case-specific context, intermediate data for the
        /// test case is requested, and when sent with only a session-specific context,
        /// intermediate data for a session is requested.
        /// </summary>
        public abstract event EventHandler<DataRequestEventArgs> DataRequest;

        /// <summary>
        /// Raised on a custom notification
        /// </summary>
        public abstract event EventHandler<CustomNotificationEventArgs> CustomNotification;

        #endregion

        #endregion
    }
}
