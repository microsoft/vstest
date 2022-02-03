// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;

using System;

/// <summary>
/// Provides a framed data and related properties.
/// </summary>
public class MessageReceivedEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the data contained in message frame.
    /// </summary>
    public string Data { get; set; }
}