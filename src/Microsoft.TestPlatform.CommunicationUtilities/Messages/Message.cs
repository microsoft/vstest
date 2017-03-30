// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class Message
    {
        /// <summary>
        /// Gets or sets the message type.
        /// </summary>
        public string MessageType { get; set; }

        /// <summary>
        /// Gets or sets the payload.
        /// </summary>
        public JToken Payload { get; set; }

        /// <summary>
        /// To string implementation.
        /// </summary>
        /// <returns> The <see cref="string"/>. </returns>
        public override string ToString()
        {
            return "(" + this.MessageType + ") -> " + (this.Payload == null ? "null" : this.Payload.ToString(Formatting.Indented));
        }
    }

    public class VersionedMessage : Message
    {
        /// <summary>
        /// Gets or sets the version of the message
        /// </summary>
        public int Version { get; set; }
    }
}