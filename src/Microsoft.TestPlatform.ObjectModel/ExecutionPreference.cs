// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel;

/// <summary>
/// Describes how the test platform should host and communicate with a given test source.
/// </summary>
public enum ExecutionPreference
{
    /// <summary>
    /// The default vstest hosting: the source is loaded into a testhost process (or run in-process) and
    /// driven over the vstest JSON-RPC testhost protocol.
    /// </summary>
    Default,

    /// <summary>
    /// The source is a Microsoft.Testing.Platform (MTP) application: it is its own executable that hosts the
    /// test framework directly. The test platform launches it in server mode and drives it over the MTP
    /// JSON-RPC protocol instead of spawning a vstest testhost.
    /// </summary>
    MicrosoftTestingPlatform,
}
