// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Discovery
{
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
    }
}
