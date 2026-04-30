// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;

/// <summary>
/// Payload for the TestCaseStarting message, sent from testhost to console when a test begins execution.
/// </summary>
[DataContract]
public class TestCaseStartingPayload
{
    /// <summary>
    /// Gets or sets the test case ID.
    /// </summary>
    [DataMember]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the display name of the test case.
    /// </summary>
    [DataMember]
    public string? DisplayName { get; set; }
}
