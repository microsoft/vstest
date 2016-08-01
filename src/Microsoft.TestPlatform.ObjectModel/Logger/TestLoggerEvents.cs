// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    /// <summary>
    /// Exposes events that Test Loggers can register for.
    /// </summary>
    public abstract class TestLoggerEvents
    {
        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        protected TestLoggerEvents()
        {
        }

        #endregion

        #region Events

        /// <summary>
        /// Raised when a test message is received.
        /// </summary>
        public abstract event EventHandler<TestRunMessageEventArgs> TestRunMessage;

        /// <summary>
        /// Raised when a test result is received.
        /// </summary>
        public abstract event EventHandler<TestResultEventArgs> TestResult;

        /// <summary>
        /// Raised when a test run is complete.
        /// </summary>
        public abstract event EventHandler<TestRunCompleteEventArgs> TestRunComplete;

        #endregion
    }
}
