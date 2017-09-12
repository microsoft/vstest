// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Discovery
{
    using Microsoft.VisualStudio.TestPlatform.Common.Filtering;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

    /// <summary>
    /// Specifies the user specified RunSettings and framework provided context of the discovery. 
    /// </summary>
    public class DiscoveryContext : IDiscoveryContext
    {
        /// <summary>
        /// Gets the run settings specified for this request.
        /// </summary>
        public IRunSettings RunSettings { get; internal set; }

        /// <summary>
        /// Gets or sets the FilterExpressionWrapper instance as created from filter string.
        /// </summary>
        internal FilterExpressionWrapper FilterExpressionWrapper {get; set; }
    }
}
