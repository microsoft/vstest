// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Base class for all execution event arguments
    /// </summary>
#if NET46
    [Serializable] 
#endif
    public abstract class DataCollectionEventArgs : EventArgs
    {
        #region Constructor

        /// <summary>
        /// Initializes the instance by storing the given information
        /// </summary>
        /// <param name="context">Context information for the event</param>
        protected DataCollectionEventArgs(DataCollectionContext context) :
            this(context, null)
        {
        }

        protected DataCollectionEventArgs(DataCollectionContext context, Uri targetDataCollectorUri)
        {
            //EqtTrace.FailIf(context == null, "Context should not be null.");

            Context = context;
            TargetDataCollectorUri = targetDataCollectorUri;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the context information for the event
        /// </summary>
        public DataCollectionContext Context
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets Data collector Uri this notification is targeted for
        /// </summary>
        public Uri TargetDataCollectorUri
        {
            get;
            set;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Updates the data collection context stored by this instance.
        /// </summary>
        /// <param name="context">Context to update with.</param>
        /// <remarks>
        /// Generally the data collection context is known in advance, however there
        /// are cases around custom notifications where it is not necessiarly known
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
