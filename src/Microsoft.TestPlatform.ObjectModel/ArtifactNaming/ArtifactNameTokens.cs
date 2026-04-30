// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.ArtifactNaming;

/// <summary>
/// Well-known token names for artifact name templates.
/// </summary>
public static class ArtifactNameTokens
{
    /// <summary>The test results output directory.</summary>
    public const string TestResultsDirectory = "TestResultsDirectory";

    /// <summary>The target framework short name (e.g., "net8.0").</summary>
    public const string Tfm = "Tfm";

    /// <summary>UTC timestamp in sortable ISO 8601 compact format with milliseconds: 20260415T105100.123</summary>
    public const string Timestamp = "Timestamp";

    /// <summary>UTC date only: 2026-04-15</summary>
    public const string Date = "Date";

    /// <summary>The machine name.</summary>
    public const string MachineName = "MachineName";

    /// <summary>The current user name.</summary>
    public const string UserName = "UserName";

    /// <summary>The current process ID.</summary>
    public const string Pid = "Pid";

    /// <summary>Short 8-character hex prefix of the test run ID.</summary>
    public const string RunId = "RunId";

    /// <summary>Full test session GUID.</summary>
    public const string SessionId = "SessionId";

    /// <summary>The test assembly file name without extension.</summary>
    public const string AssemblyName = "AssemblyName";

    /// <summary>The runtime architecture (e.g., "x64", "x86", "arm64").</summary>
    public const string Architecture = "Architecture";

    /// <summary>The build configuration (e.g., "Debug", "Release").</summary>
    public const string Configuration = "Configuration";

    /// <summary>The project name.</summary>
    public const string ProjectName = "ProjectName";

    /// <summary>The test host index in parallel execution.</summary>
    public const string HostId = "HostId";
}
