// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.VisualStudio.TestPlatform.Common.Utilities;

/// <summary>
/// Helpers for run-as-exe mode, where a test project runs as its own executable instead of being hosted by
/// testhost. In that mode the process is self-contained: it resolves dependencies and discovers test adapters
/// from its own folder (the app base), and ignores the custom assembly resolver and the runner's
/// default/additional extensions.
/// </summary>
internal static class RunAsExeHelper
{
    /// <summary>
    /// Environment variable the test host manager (DefaultTestHostManager / DotnetTestHostManager) sets on the
    /// host process when it launches a test project that runs as its own executable. Kept in sync with the
    /// writer in those managers.
    /// </summary>
    internal const string RunAsExeEnvironmentVariableName = "VSTEST_RUNASEXE";

    /// <summary>
    /// True when the current process is a test project running as its own executable (run-as-exe).
    /// </summary>
    internal static bool IsRunningAsExe { get; } =
        Environment.GetEnvironmentVariable(RunAsExeEnvironmentVariableName) == "1";
}
