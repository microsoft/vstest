// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.MTP;

/// <summary>
/// Constants shared by the Microsoft.Testing.Platform (MTP) integration.
/// </summary>
internal static class MtpConstants
{
    /// <summary>
    /// Synthetic executor URI reported for tests that run over the MTP protocol. MTP applications are
    /// their own host and do not expose a vstest executor, so vstest attributes their results to this
    /// well-known URI.
    /// </summary>
    public const string DefaultExecutorUri = "executor://MicrosoftTestingPlatform/v1";

    /// <summary>
    /// Id of the <see cref="ObjectModel.TestProperty"/> used to round-trip the MTP test-node uid on a
    /// vstest <see cref="ObjectModel.TestCase"/> so a discovered test can later be run by uid.
    /// </summary>
    public const string MtpUidPropertyId = "MTP.TestNode.Uid";
}
