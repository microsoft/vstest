// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;

/// <summary>
/// Construct used for communication
/// </summary>
public class RoutableMessage
{
    /// <summary>
    /// Gets or sets the message type.
    /// </summary>
    public string MessageType { get; set; }

    /// <summary>
    /// Gets or sets the version of the message
    /// </summary>
    public int Version { get; set; }

    public string RawMessage { get; set; }

    /// <summary>
    /// To string implementation.
    /// </summary>
    /// <returns> The <see cref="string"/>. </returns>
    public override string ToString()
    {

        return RawMessage != null ? RawMessage : nameof(RoutableMessage);
    }
}
