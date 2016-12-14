// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Client.RequestHelper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Threading.Tasks;

    /// <summary>
    /// Class used to define the CustomHostLaunchAckPayload sent by the Vstest.console translation layers into design mode
    /// </summary>
    public class CustomHostLaunchAckPayload
    {
        /// <summary>
        /// ProcessId of the TestHost launched by Clients like IDE, LUT etc. 
        /// </summary>
        [DataMember]
        public int HostProcessId { get; set; }

        /// <summary>
        /// ErrorMessage, in cases where custom launch fails 
        /// </summary>
        [DataMember]
        public string ErrorMessage { get; set; }
    }
}
