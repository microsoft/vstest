// Copyright (c) Microsoft. All rights reserved.

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
            return "(" + MessageType + ") -> " + (Payload == null ? "null" : Payload.ToString(Formatting.Indented));
        }
    }
}