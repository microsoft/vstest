﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Payloads;

using System.Runtime.Serialization;

/// <summary>
/// Class used to define the start test session ack payload sent by the design mode client
/// back to the vstest.console translation layers.
/// </summary>
[DataContract]
public class StartTestSessionAckPayload
{
    /// <summary>
    /// Gets or sets the event args.
    /// </summary>
    [DataMember]
    public StartTestSessionCompleteEventArgs EventArgs { get; set; } = null;
}
