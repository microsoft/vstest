// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Base class for all execution event arguments
    /// </summary>
#if NETFRAMEWORK
    [Serializable]
#endif
    public abstract class DataCollectionEventArgs : EventArgs
    {
        #region Constructor

        /// <summary>
        /// Default constructor added for serialization/deserialization.
        /// </summary>
        public DataCollectionEventArgs()
        {
        }

        /// <summary>
        /// Initializes the instance by storing the given information
        /// </summary>
        /// <param name="context">Context information for the event</param>
        protected DataCollectionEventArgs(DataCollectionContext context)
        {
            Context = context;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the context information for the event
        /// </summary>
        public DataCollectionContext Context
        {
            get;
            internal set;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Updates the data collection context stored by this instance.
        /// </summary>
        /// <param name="context">Context to update with.</param>
        /// <remarks>
        /// Generally the data collection context is known in advance, however there
        /// are cases around custom notifications where it is not necessarily known
        /// until the event is being sent.  This is used for updating the context when
        /// sending the event.
        /// </remarks>
        internal void UpdateDataCollectionContext(DataCollectionContext context)
        {
            Debug.Assert(context != null, "'context' cannot be null.");
            Context = context;
        }

        #endregion
    }
}
