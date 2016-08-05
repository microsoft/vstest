// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.DataCollection.V1
{
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// An environment variable requested to be set in the test execution environment by a data collector, including the
    /// friendly names of data collectors that requested it.
    /// This is needed to find list of environment variables needed for test run after eliminating the duplicate name and keys.
    /// For details check DataCollectionPluginManager.AddCollectorEnvironmentVariables() method.
    /// </summary>
    internal sealed class CollectorRequestedEnvironmentVariable
    {
        #region Fields

        /// <summary>
        /// Variable name and requested value
        /// </summary>
        private readonly KeyValuePair<string, string> variable;

        /// <summary>
        /// Friendly names of data collectors that requested this environment variable
        /// </summary>
        private List<string> dataCollectorsThatRequested;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectorRequestedEnvironmentVariable"/> class.
        /// </summary>
        /// <param name="variable">
        /// Variable name and requested value.
        /// </param>
        /// <param name="requestingDataCollectorFriendlyName">
        /// Friendly name of the data collector requesting it.
        /// </param>
        public CollectorRequestedEnvironmentVariable(
            KeyValuePair<string, string> variable,
            string requestingDataCollectorFriendlyName)
        {
            Debug.Assert(!string.IsNullOrEmpty(variable.Key), "'variable.Key' is null or empty");
            Debug.Assert(
                    !string.IsNullOrEmpty(requestingDataCollectorFriendlyName),
                "'requestingDataCollectorFriendlyName' is null or empty");

            this.variable = variable;
            this.dataCollectorsThatRequested = new List<string> { requestingDataCollectorFriendlyName };
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets variable name.
        /// </summary>
        public string Name
        {
            get
            {
                return this.variable.Key;
            }
        }

        /// <summary>
        /// Gets requested value
        /// </summary>
        public string Value
        {
            get
            {
                return this.variable.Value;
            }
        }

        /// <summary>
        /// Gets friendly name of the first data collector that requested this environment variable
        /// </summary>
        public string FirstDataCollectorThatRequested
        {
            get
            {
                return this.dataCollectorsThatRequested[0];
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds the data collector to the list of data collectors that requested this variable. 
        /// </summary>
        /// <param name="requestingDataCollectorFriendlyName">Friendly name of requesting data collector.</param>
        public void AddRequestingDataCollector(string requestingDataCollectorFriendlyName)
        {
            Debug.Assert(
                !this.dataCollectorsThatRequested.Contains(requestingDataCollectorFriendlyName),
                "'dataCollectorsThatRequested' already contains the data collector '"
                + requestingDataCollectorFriendlyName + "'");
            this.dataCollectorsThatRequested.Add(requestingDataCollectorFriendlyName);
        }

        #endregion
    }
}