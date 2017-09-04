// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Telemetry
{
    /// <summary>
    /// The UnitTestTelemetryServiceProvider interface. 
    /// Provides API's that have to be implemented by Telemetry Service Provider like VSTelemetry etc.
    /// </summary>
    public interface IUnitTestTelemetryServiceProvider
    {
        /// <summary>
        /// Log Event
        /// This Method will Aggregate all the properties with values in the eventName
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        /// <param name="property">Name of the property in that event</param>
        /// <param name="value">Value of Property</param>
        void LogEvent(string eventName, string property, string value);

        /// <summary>
        /// Post Event
        /// This method will Post the event to the server
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        void PostEvent(string eventName);

        /// <summary>
        /// Dispose the Telemetry Service Provider
        /// </summary>
        void Dispose();
    }
}
