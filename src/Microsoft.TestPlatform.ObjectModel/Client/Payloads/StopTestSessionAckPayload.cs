// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Payloads
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Class used to define the stop test session ack payload sent by the design mode client
    /// back to the vstest.console translation layers.
    /// </summary>
    public class StopTestSessionAckPayload
    {
        /// <summary>
        /// Gets or sets the test session info.
        /// </summary>
        [DataMember]
        public TestSessionInfo TestSessionInfo { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if the session was successfully stopped or not.
        /// </summary>
        [DataMember]
        public bool IsStopped { get; set; }
    }
}
