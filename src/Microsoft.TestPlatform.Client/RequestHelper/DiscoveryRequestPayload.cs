// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Client.RequestHelper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Threading.Tasks;

    /// <summary>
    /// Class used to define the DiscoveryRequestPayload sent by the Vstest.console translation layers into design mode
    /// </summary>
    public class DiscoveryRequestPayload
    {
        /// <summary>
        /// Settings used for the discovery request. 
        /// </summary>
        [DataMember]
        public IEnumerable<string> Sources { get; set; }

        /// <summary>
        /// Settings used for the discovery request. 
        /// </summary>
        [DataMember]
        public string RunSettings { get; set; }
    }
}
