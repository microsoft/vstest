// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
internal class ShowDeprecateDotnetVStestMessageArgumentProcessor : IArgumentProcessor
{
    public const string CommandName = "/ShowDeprecateDotnetVSTestMessage";
    private Lazy<IArgumentProcessorCapabilities>? _metadata;
    private Lazy<IArgumentExecutor>? _executor;

    public Lazy<IArgumentExecutor>? Executor
    {
        get => _executor ??= new Lazy<IArgumentExecutor>(() =>
            new ShowDeprecateDotnetVStestMessageProcessorExecutor());

        set => _executor = value;
    }

    public Lazy<IArgumentProcessorCapabilities> Metadata
        => _metadata ??= new Lazy<IArgumentProcessorCapabilities>(() => new ShowDeprecateDotnetVStestMessageProcessorCapabilities());

    public static bool ContainsShowDeprecateDotnetVSTestMessageCommand(string[]? args) =>
          args?.Contains("--ShowDeprecateDotnetVSTestMessage", StringComparer.OrdinalIgnoreCase) == true;
}

internal class ShowDeprecateDotnetVStestMessageProcessorCapabilities : BaseArgumentProcessorCapabilities
{
    public override string CommandName => ShowDeprecateDotnetVStestMessageArgumentProcessor.CommandName;

    public override bool AllowMultiple => false;

    // We put priority at the same level of the argument processor for runsettings passed as argument through cli.
    // We'll be sure to run before test run arg processor.
    public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.CliRunSettings;

    public override HelpContentPriority HelpPriority => HelpContentPriority.None;

    // We want to be sure that this command won't show in user help
    public override string? HelpContentResourceName => null;
}

internal class ShowDeprecateDotnetVStestMessageProcessorExecutor : IArgumentExecutor
{
    public ArgumentProcessorResult Execute() => ArgumentProcessorResult.Success;

    public void Initialize(string? argument) { }
}
