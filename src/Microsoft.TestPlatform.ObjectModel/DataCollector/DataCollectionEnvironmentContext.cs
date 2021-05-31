// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection
{
#if NETFRAMEWORK
    using System;
#endif

    /// <summary>
    /// Encapsulates the context of the environment a data collector is being hosted in.
    /// </summary>
#if NETFRAMEWORK
    [Serializable]
#endif
    public sealed class DataCollectionEnvironmentContext
    {
        #region Fields
        /// <summary>
        /// DataCollectionContext for the session.
        /// </summary>
        private DataCollectionContext sessionDataCollectionContext;

        #endregion

        #region Constructors and initialization

        /// <summary>
        /// Default Constructor
        /// </summary>
        internal DataCollectionEnvironmentContext()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes with the DataCollectionContext
        /// </summary>
        public DataCollectionEnvironmentContext(DataCollectionContext sessionDataCollectionContext)
        {
            SessionDataCollectionContext = sessionDataCollectionContext;
        }


        /// <summary>
        /// Creates an environment context for a local (hosted) agent and controller
        /// </summary>
        /// <returns>An environment context for a local (hosted) agent and controller</returns>
        public static DataCollectionEnvironmentContext CreateForLocalEnvironment()
        {
            return CreateForLocalEnvironment(null);
        }

        /// <summary>
        /// Creates an environment context for a local (hosted) agent and controller
        /// </summary>
        /// <param name="sessionDataCollectionContext">Session level data collection context.</param>
        /// <returns>An environment context for a local (hosted) agent and controller</returns>
        public static DataCollectionEnvironmentContext CreateForLocalEnvironment(DataCollectionContext sessionDataCollectionContext)
        {
            var dataCollectionEnvironmentContext = new DataCollectionEnvironmentContext();
            dataCollectionEnvironmentContext.SessionDataCollectionContext = sessionDataCollectionContext;

            return dataCollectionEnvironmentContext;
        }

        #endregion

        #region Properties

        /// <summary>
        /// DataCollectionContext for the session.
        /// </summary>
        public DataCollectionContext SessionDataCollectionContext
        {
            get
            {
                return sessionDataCollectionContext;
            }
            internal set
            {
                sessionDataCollectionContext = value;
            }
        }

        #endregion
    }
}
