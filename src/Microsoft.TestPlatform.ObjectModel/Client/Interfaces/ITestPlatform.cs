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
        ///     Specifies the path to unit test extensions.
        ///     If no additional extension is available, then specify null or empty list.
        /// </param>
        /// <param name="skipExtensionFilters"></param>
        void UpdateExtensions(IEnumerable<string> pathToAdditionalExtensions, bool skipExtensionFilters);

        /// <summary>
        /// Clear the extensions
        /// </summary>
        void ClearExtensions();

        /// <summary>
        /// Creates a discovery request
        /// </summary>
        /// <param name="requestData">Providing common services and data for Discovery</param>
        /// <param name="discoveryCriteria">Specifies the discovery parameters</param>
        /// <param name="options">Test platform options.</param>
        /// <returns>DiscoveryRequest object</returns>
        IDiscoveryRequest CreateDiscoveryRequest(IRequestData requestData, DiscoveryCriteria discoveryCriteria, TestPlatformOptions options);

        /// <summary>
        /// Creates a test run request.
        /// </summary>
        /// <param name="requestData">Providing common services and data for Execution</param>
        /// <param name="testRunCriteria">Specifies the test run criteria</param>
        /// <param name="options">Test platform options.</param>
        /// <returns>RunRequest object</returns>
        ITestRunRequest CreateTestRunRequest(IRequestData requestData, TestRunCriteria testRunCriteria, TestPlatformOptions options);
    }
}
