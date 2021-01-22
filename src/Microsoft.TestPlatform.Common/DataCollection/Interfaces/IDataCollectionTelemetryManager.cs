// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollector.Interfaces
{
    /// <summary>
    /// The IDataCollectionTelemetryManager Interface.
    /// </summary>
    internal interface IDataCollectionTelemetryManager
    {
        /// <summary>
        /// Stores telemetry regarding environment variable added.
        /// </summary>
        /// <param name="dataCollectorInformation">
        /// Data collector information which requested environment variable.
        /// </param>
        /// <param name="name">
        /// Environment variable name.
        /// </param>
        /// <param name="value">
        /// Environment variable value.
        /// </param>
        void OnEnvironmentVariableAdded(DataCollectorInformation dataCollectorInformation, string name, string value);

        /// <summary>
        /// Stores telemetry regarding environment variable is conflicting.
        /// </summary>
        /// <param name="dataCollectorInformation">
        /// Data collector information which requested environment variable.
        /// </param>
        /// <param name="name">
        /// Environment variable name.
        /// </param>
        /// <param name="existingValue">
        /// Environment variable value that was requested previously.
        /// </param>
        void OnEnvironmentVariableConflict(DataCollectorInformation dataCollectorInformation, string name, string existingValue);
    }
}
