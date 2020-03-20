// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        /// Initializes a new instance of the <see cref="TestResultEventArgs"/> class.
        /// </summary>
        /// <param name="result">
        /// Test Result for the event.
        /// </param>
        public TestResultEventArgs(TestResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            this.Result = result;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the Test Result.
        /// </summary>
        public TestResult Result { get; private set; }

        #endregion
    }
}
