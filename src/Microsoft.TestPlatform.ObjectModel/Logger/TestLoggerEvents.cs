// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        /// Raised when a test run starts.
        /// </summary>
        public abstract event EventHandler<TestRunStartEventArgs> TestRunStart;

        /// <summary>
        /// Raised when a test result is received.
        /// </summary>
        public abstract event EventHandler<TestResultEventArgs> TestResult;

        /// <summary>
        /// Raised when a test run is complete.
        /// </summary>
        public abstract event EventHandler<TestRunCompleteEventArgs> TestRunComplete;

        /// <summary>
        /// Raised when test discovery starts
        /// </summary>
        public abstract event EventHandler<DiscoveryStartEventArgs> DiscoveryStart;
        #endregion
    }
}
