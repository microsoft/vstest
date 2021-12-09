// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities
{
#pragma warning disable RS0016 // Add public types and members to the declared API
    public class MessageMetadata
    {
        public MessageMetadata(int version, string recipient)
        {
            this.Version = version;
            this.Recipient = recipient;
        }

        public int Version { get; }

        public string Recipient { get; }

        internal static MessageMetadata Empty { get; } = new MessageMetadata(0, null);
    }
#pragma warning restore RS0016 // Add public types and members to the declared API
}
