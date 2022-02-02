// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;

/// <summary>
/// Construct with version used for communication
/// Introduced in 15.1.0 version and default message protocol v2 onwards.
/// </summary>
public class VersionedMessage : Message
{
    /// <summary>
    /// Gets or sets the version of the message
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets an id of the consumer this message is meant for.
    /// </summary>
#pragma warning disable RS0016 // Add public types and members to the declared API
    public string Recipient { get; set; }
#pragma warning restore RS0016 // Add public types and members to the declared API
}
