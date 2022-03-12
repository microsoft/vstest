// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.TestPlatform.AcceptanceTests;

/// <summary>
///
/// </summary>
/// <param name="RunnerFramework"></param>
/// <param name="TargetFramework"></param>
/// <param name="InIsolationValue">Supported value = <c>/InIsolation</c>.</param>
public record RunnerInfo(string RunnerFramework, string TargetFramework, string InIsolationValue = "")
{
    /// <summary>
    /// Is running via .NET "Core" vstest.console?
    /// </summary>
    public bool IsNetRunner => !IsNetFrameworkRunner;

    /// <summary>
    /// Is running via .NET Framework vstest.console?
    /// </summary>
    public bool IsNetFrameworkRunner => RunnerFramework.StartsWith("net4", StringComparison.InvariantCultureIgnoreCase);

    /// <summary>
    /// Is running via .NET "Core" testhost?
    /// </summary>
    public bool IsNetTarget => !IsNetFrameworkTarget;

    /// <summary>
    /// Is running via .NET Framework testhost?
    /// </summary>
    public bool IsNetFrameworkTarget => TargetFramework.StartsWith("net4", StringComparison.InvariantCultureIgnoreCase);

    public override string ToString()
        => string.Join(
            ",",
            new[]
            {
                "RunnerFramework = " + RunnerFramework,
                " TargetFramework = " + TargetFramework,
                string.IsNullOrEmpty(InIsolationValue) ? " InProcess" : " InIsolation",
            });
}
