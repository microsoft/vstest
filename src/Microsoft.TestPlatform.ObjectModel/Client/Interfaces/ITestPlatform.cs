// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
{
    using System;
    using System.Collections.Generic;

    public interface ITestPlatform : IDisposable
    {
        /// <summary>
        /// Update the extensions to be used by the test service
        /// </summary>
        /// <param name="pathToAdditionalExtensions">
        /// Specifies the path to unit test extensions. 
        /// If no additional extension is available, then specify null or empty list.
        /// </param>
        /// <param name="loadOnlyWellKnownExtensions">Specifies whether only well known extensions should be loaded.</param>
        void UpdateExtensions(IEnumerable<string> pathToAdditionalExtensions, bool loadOnlyWellKnownExtensions);

        /// <summary>
        /// Clear the extensions
        /// </summary>
        void ClearExtensions();

        /// <summary>
        /// Creates a discovery request
        /// </summary>
        /// <param name="discoveryCriteria">Specifies the discovery parameters</param>
        /// <param name="protocolConfig">Protocol related information</param>
        /// <returns>DiscoveryRequest object</returns>
        IDiscoveryRequest CreateDiscoveryRequest(DiscoveryCriteria discoveryCriteria, ProtocolConfig protocolConfig);

        /// <summary>
        /// Creates a test run request.
        /// </summary>
        /// <param name="testRunCriteria">Specifies the test run criteria</param>
        /// <param name="protocolConfig">Protocol related information</param>
        /// <returns>RunRequest object</returns>
        ITestRunRequest CreateTestRunRequest(TestRunCriteria testRunCriteria, ProtocolConfig protocolConfig);
    }
}
