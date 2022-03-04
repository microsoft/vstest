// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

using System;

namespace Microsoft.TestPlatform.AcceptanceTests;

public class RunnerInfo
{
    public RunnerInfo(string runnerType, string targetFramework) : this(runnerType, targetFramework, "")
    {
    }

    public RunnerInfo(string runnerType, string targetFramework, string inIsolation)
    {
        RunnerFramework = runnerType;
        TargetFramework = targetFramework;
        InIsolationValue = inIsolation;
        // The value is netcoreapp2.1.
        IsNetRunner = RunnerFramework.StartsWith("netcoreapp", StringComparison.InvariantCultureIgnoreCase);
        // The value is net451.
        IsNetFrameworkRunner = !IsNetRunner;
        IsNetTarget = TargetFramework.StartsWith("netcoreapp", StringComparison.InvariantCultureIgnoreCase);
        IsNetFrameworkTarget = !IsNetTarget;
    }

    /// <summary>
    /// Gets the target framework.
    /// </summary>
    public string TargetFramework
    {
        get;
        set;
    }

    /// <summary>
    /// Gets the inIsolation.
    /// Supported values = <c>/InIsolation</c>.
    /// </summary>
    public string InIsolationValue
    {
        get; set;
    }

    /// <summary>
    /// Gets the application type.
    /// </summary>
    public string RunnerFramework
    {
        get;
        set;
    }

    public override string ToString()
    {
        return string.Join(",", new[] { "RunnerFramework = " + RunnerFramework, " TargetFramework = " + TargetFramework, string.IsNullOrEmpty(InIsolationValue) ? " InProcess" : " InIsolation" });
    }

    /// <summary>
    /// Is running via .NET "Core" vstest.console?
    /// </summary>
    /// <returns></returns>
    public bool IsNetRunner { get; }

    /// <summary>
    /// Is running via .NET Framework vstest.console?
    /// </summary>
    /// <returns></returns>
    public bool IsNetFrameworkRunner { get; }

    /// <summary>
    /// Is running via .NET "Core" testhost?
    /// </summary>
    /// <returns></returns>
    public bool IsNetTarget { get; }

    /// <summary>
    /// Is running via .NET Framework testhost?
    /// </summary>
    /// <returns></returns>
    public bool IsNetFrameworkTarget { get; }
}
