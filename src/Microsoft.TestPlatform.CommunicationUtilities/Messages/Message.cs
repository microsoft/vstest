// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities
{
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Construct used for communication
    /// </summary>
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
            // Redact by scanning for property names that look like they hold a secret, rather than by
            // listing the message types that carry one. A list has to be updated whenever a message or a
            // variable is added or renamed, and the one time it is not, the value lands in a log. This
            // covers sensitive properties in environment blocks that messages carry to a child process.
            return "(" + this.MessageType + ") -> " + (this.Payload == null ? "null" : SecretRedaction.RedactSecretsInJson(this.Payload.ToString(Formatting.Indented)));
        }
    }
}
