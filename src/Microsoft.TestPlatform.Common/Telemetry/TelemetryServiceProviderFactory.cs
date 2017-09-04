// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Telemetry
{
    /// <summary>
    /// The telemetry service provider factory.
    /// </summary>
    public static class TelemetryServiceProviderFactory
    {
        /// <summary>
        /// The get default telemetry service provider.
        /// This Method will return the default Telemetry Service Provider.
        /// </summary>
        /// <returns>
        /// The <see cref="IUnitTestTelemetryServiceProvider"/>.
        /// </returns>
        public static IUnitTestTelemetryServiceProvider GetDefaultTelemetryServiceProvider()
        {
            return new UnitTestTelemetryServiceProvider();
        }
    }
}
