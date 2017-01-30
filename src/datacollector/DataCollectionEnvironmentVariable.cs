// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    /// <summary>
    /// An environment variable requested to be set in the test execution environment by a data collector, including the
    /// friendly names of data collectors that requested it.
    /// This is needed to find list of environment variables needed for test run after eliminating the duplicate name and keys.
    /// For details check DataCollectionPluginManager.AddCollectorEnvironmentVariables() method.
    /// </summary>
    internal class DataCollectionEnvironmentVariable
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
        /// Initializes a new instance of the <see cref="DataCollectionEnvironmentVariable"/> class. 
        /// </summary>
        /// <param name="variable">
        /// Variable name and requested value.
        /// </param>
        /// <param name="requestingDataCollectorFriendlyName">
        /// Friendly name of the data collector requesting it.
        /// </param>
        public DataCollectionEnvironmentVariable(
            KeyValuePair<string, string> variable,
            string requestingDataCollectorFriendlyName)
        {
            ValidateArg.NotNullOrEmpty(variable.Key, nameof(variable.Key));
            ValidateArg.NotNullOrEmpty(requestingDataCollectorFriendlyName, nameof(requestingDataCollectorFriendlyName));

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
            ValidateArg.NotNullOrEmpty(requestingDataCollectorFriendlyName, nameof(requestingDataCollectorFriendlyName));
            this.dataCollectorsThatRequested.Add(requestingDataCollectorFriendlyName);
        }

        #endregion
    }
}
