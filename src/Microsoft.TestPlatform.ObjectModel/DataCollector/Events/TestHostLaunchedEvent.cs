// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection
{
    using System;
    using System.Diagnostics;
    using System.Runtime.Serialization;

    /// <summary>
    /// Session End event arguments
    /// </summary>
    [DataContract]
    public sealed class TestHostLaunchedEventArgs : DataCollectionEventArgs
    {
        #region Private members

        /// <summary>
        /// Process id of the test host
        /// </summary>
        private int processId;

        #endregion

        #region Public properties

        public int TestHostProcessId
        {
            get
            {
                return processId;
            }
        }

        #endregion

        #region Constructor
        
        /// <summary>
        /// Initializes a new instance of the <see cref="TestHostLaunchedEventArgs"/> class. 
        /// </summary>
        /// <param name="context">
        /// Data collection context
        /// </param>
        /// <param name="processId">
        /// Process id of test host
        /// </param>
        public TestHostLaunchedEventArgs(DataCollectionContext context, int processId)
            : base(context)
        {
            this.processId = processId;
        }

        #endregion
    }
}
