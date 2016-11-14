// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using System;
    using System.Collections.Generic;

    public interface ITestPlatform : IDisposable
    {
        /// <summary>
        /// Initialize the test platform with the path to additional unit test extensions. 
        /// If no additional extension is available, then specify null or empty list. 
        /// </summary>
        /// <param name="additionalUnitTestExtensions">Specifies the path to unit test extensions.</param>
        /// <param name="loadOnlyWellKnownExtensions">Specifies whether only well known extensions should be loaded.</param>
        /// <param name="forceX86Discoverer">Forces test discovery in x86 Discoverer process.</param>
        void Initialize(IEnumerable<string> pathToAdditionalExtensions, bool loadOnlyWellKnownExtensions, bool forceX86Discoverer);

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
        /// Creates a discovery request
        /// </summary>
        /// <param name="discoveryCriteria">Specifies the discovery parameters</param>
        /// <returns>DiscoveryRequest object</returns>
        IDiscoveryRequest CreateDiscoveryRequest(DiscoveryCriteria discoveryCriteria);

        /// <summary>
        /// Creates a test run request.
        /// </summary>
        /// <param name="testRunCriteria">Specifies the test run criteria</param>
        /// <returns>RunRequest object</returns>
        ITestRunRequest CreateTestRunRequest(TestRunCriteria testRunCriteria);
    }
}
