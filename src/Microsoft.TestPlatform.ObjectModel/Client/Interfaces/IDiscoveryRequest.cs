// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    /// <summary>
    /// IDiscoverTestsRequest returned after calling GetDiscoveredTestsAsync 
    /// </summary>
    public interface IDiscoveryRequest: IRequest
    {
        /// <summary>
        /// Starts tests discovery async.
        /// </summary>
        void DiscoverAsync();

        /// <summary>
        /// Aborts the discovery request
        /// </summary>
        void Abort();

        /// <summary>
        /// <summary>
        ///  Handler for notifying discovery process is complete
        /// </summary>
        event EventHandler<DiscoveryCompleteEventArgs> OnDiscoveryComplete;

        /// <summary>
        ///  Handler for notifying when newly found tests are available for UI to fetch.
        /// </summary>
        event EventHandler<DiscoveredTestsEventArgs> OnDiscoveredTests;

        /// <summary>
        ///  Handler for receiving error during fetching/execution. This is used for when abnormal error 
        ///  occurs; equivalent of IRunMessageLogger in the current RockSteady core
        /// </summary>
        event EventHandler<TestRunMessageEventArgs> OnDiscoveryMessage;

        /// <summary>
        /// Specifies the discovery criterion
        /// </summary>
        DiscoveryCriteria DiscoveryCriteria
        {
            get;
        }
    }
}
