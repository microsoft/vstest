// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

    /// <summary>
    /// Multi test run finalization complete payload.
    /// </summary>
    public class MultiTestRunFinalizationProgressPayload
    {
        /// <summary>
        /// Gets or sets the multi test run finalization complete args.
        /// </summary>
        public MultiTestRunFinalizationProgressEventArgs FinalizationProgressEventArgs { get; set; }
    }
}
