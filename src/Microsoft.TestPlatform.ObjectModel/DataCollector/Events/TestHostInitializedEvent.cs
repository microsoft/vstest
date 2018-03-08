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
    public sealed class TestHostInitializedEventArgs : DataCollectionEventArgs
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
        /// Initializes a new instance of the <see cref="SessionEndEventArgs"/> class. 
        /// </summary>
        /// <remarks>
        /// Default constructor with default DataCollectionContext.
        /// DataCollectionContext with empty session signifies that is it irrelevent in the current context.
        /// </remarks>
        public TestHostInitializedEventArgs(int processId)
        {
            this.processId = processId;
        }

        #endregion
    }
}
