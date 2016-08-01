// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging
{
    using System;

    /// <summary>
    /// Event arguments used for raising Test Result events.
    /// </summary>
    public class TestResultEventArgs : EventArgs
    {
        #region Constructor

        /// <summary>
        /// Initializes with the test result for the event.
        /// </summary>
        /// <param name="result">Test Result for the event.</param>
        public TestResultEventArgs(TestResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            
            Result = result;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Test Result.
        /// </summary>
        public TestResult Result { get; private set; }

        #endregion
    }
}
