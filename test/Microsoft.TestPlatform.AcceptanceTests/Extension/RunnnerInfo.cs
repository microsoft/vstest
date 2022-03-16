﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.TestPlatform.TestUtilities;

namespace Microsoft.TestPlatform.AcceptanceTests;

/// <summary>
///
/// </summary>
/// <param name="RunnerFramework"></param>
/// <param name="TargetFramework"></param>
/// <param name="InIsolationValue">Supported value = <c>/InIsolation</c>.</param>
[Serializable]
public class RunnerInfo
{
    public RunnerInfo(string runnerFramework, string targetFramework, string? inIsolationValue = "")
    {
        RunnerFramework = runnerFramework;
        TargetFramework = targetFramework;
        InIsolationValue = inIsolationValue;
    }

    public string RunnerFramework { get; set; }

    public VSTestConsoleInfo VSTestConsoleInfo { get; set; }


    public string TargetFramework { get; set; }
    public string? InIsolationValue { get; set; }

    public DebugInfo? DebugInfo { get; set; }

    public List<DllInfo> DllInfos { get; set; } = new();

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

    public override string ToString() => $"Runner = {RunnerFramework}, TargetFramework = {TargetFramework}, {(string.IsNullOrEmpty(InIsolationValue) ? "InProcess" : "InIsolation")}, {VSTestConsoleInfo}, {string.Join(",", DllInfos)}";
}
