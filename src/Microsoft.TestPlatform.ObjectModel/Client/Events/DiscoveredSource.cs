// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

/// <summary>
/// Describes a source file that was discovered, or that was requested to be discovered.
/// </summary>
public class DiscoveredSource
{
    [DataMember]
    public string? Source { get; set; }
}
