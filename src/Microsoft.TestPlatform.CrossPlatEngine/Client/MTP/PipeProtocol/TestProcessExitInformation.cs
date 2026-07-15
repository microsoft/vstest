// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// This file is vendored from https://github.com/Youssef1313/MTPSharding (YTest.MTP.PipeProtocol),
// used under the MIT license with the author's permission. See THIRD-PARTY-NOTICES.txt.
#nullable enable

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.MTP.PipeProtocol;

/// <summary>
/// Information about the test process after it exists
/// </summary>
public class TestProcessExitInformation
{
    /// <summary>
    /// The standard output of the test process.
    /// </summary>
    public required string StandardOutput { get; init; }

    /// <summary>
    /// The standard error of the test process.
    /// </summary>
    public required string StandardError { get; init; }

    /// <summary>
    /// The exit code of the test process.
    /// </summary>
    public required int ExitCode { get; init; }
}
