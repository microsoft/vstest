// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection
{
    using System;

    /// <summary>
    /// Collects the required and optional information needed for requesting a file transfer from a data collector.
    /// </summary>
    public abstract class BasicTransferInformation
    {
        #region Fields

        private string description;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes with the data collection context for the transfer.
        /// </summary>
        /// <param name="context">The data collection context for the transfer.</param>
        protected BasicTransferInformation(DataCollectionContext context)
        {
            //EqtAssert.ParameterNotNull(context, "context");

            Context = context;
            Description = String.Empty;
        }

        #endregion

        #region  Required Parameters.

        /// <summary>
        /// The data collection context the transfer will be associated with.
        /// </summary>
        public DataCollectionContext Context { get; private set; }

        #endregion

        #region Optional Parameters.

        /// <summary>
        /// A short description of the data being sent.
        /// </summary>
        public string Description
        {
            get
            {
                return description;
            }
            set
            {
                // If we don't have a description, use an empty string.
                if (value == null)
                {
                    description = String.Empty;
                }
                else
                {
                    description = value;
                }
            }
        }

        /// <summary>
        /// Token which will be included with the callback to identify this file transfer.
        /// </summary>
        public object UserToken { get; set; }

        /// <summary>
        /// The ID of the request that this file should be associated with. This is used
        /// for sending transient data which will be associated only with this
        /// data request and not the session or test cases that are currently running.
        /// </summary>
        public RequestId RequestId { get; set; }

        /// <summary>
        /// Indicates if cleanup should be performed after transferring the resource.  This 
        /// can be known by different names in the derived classes so it is protected internal
        /// so that we can refer to it in a consistent way.
        /// </summary>
        protected internal abstract bool PerformCleanup
        {
            get;
        }

        /// <summary>
        /// The name of the file to use on the client machine.  This 
        /// can be known by different names in the derived classes so it is protected internal
        /// so that we can refer to it in a consistent way.
        /// </summary>
        protected internal abstract string ClientFileName
        {
            get;
        }

        #endregion
    }
}
