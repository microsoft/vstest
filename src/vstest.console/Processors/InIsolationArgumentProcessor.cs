// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;

using System;
using System.Diagnostics.Contracts;
using System.Globalization;

using Common;
using Common.Interfaces;

using Microsoft.VisualStudio.TestPlatform.Common.Utilities;

using CommandLineResources = Resources.Resources;

/// <summary>
///  An argument processor that allows the user to specify whether the execution
///  should happen in the current vstest.console.exe process or a new different process.
/// </summary>
internal class InIsolationArgumentProcessor : IArgumentProcessor
{
    #region Constants

    public const string CommandName = "/InIsolation";

    #endregion

    private Lazy<IArgumentProcessorCapabilities> _metadata;

    private Lazy<IArgumentExecutor> _executor;

    /// <summary>
    /// Gets the metadata.
    /// </summary>
    public Lazy<IArgumentProcessorCapabilities> Metadata
    {
        get
        {
            if (_metadata == null)
            {
                _metadata = new Lazy<IArgumentProcessorCapabilities>(() => new InIsolationArgumentProcessorCapabilities());
            }

            return _metadata;
        }
    }

    /// <summary>
    /// Gets or sets the executor.
    /// </summary>
    public Lazy<IArgumentExecutor> Executor
    {
        get
        {
            if (_executor == null)
            {
                _executor =
                    new Lazy<IArgumentExecutor>(
                        () =>
                            new InIsolationArgumentExecutor(CommandLineOptions.Instance, RunSettingsManager.Instance));
            }

            return _executor;
        }

        set
        {
            _executor = value;
        }
    }
}

internal class InIsolationArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
{
    public override string CommandName => InIsolationArgumentProcessor.CommandName;

    public override bool AllowMultiple => false;

    public override bool IsAction => false;

    public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.AutoUpdateRunSettings;

    public override string HelpContentResourceName => CommandLineResources.InIsolationHelp;

    public override HelpContentPriority HelpPriority => HelpContentPriority.InIsolationArgumentProcessorHelpPriority;
}

internal class InIsolationArgumentExecutor : IArgumentExecutor
{
    private readonly CommandLineOptions _commandLineOptions;
    private readonly IRunSettingsProvider _runSettingsManager;

    public const string RunSettingsPath = "RunConfiguration.InIsolation";

    #region Constructors
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="options">Command line options</param>
    /// <param name="runSettingsManager">the runsettings manager</param>
    public InIsolationArgumentExecutor(CommandLineOptions options, IRunSettingsProvider runSettingsManager)
    {
        Contract.Requires(options != null);
        _commandLineOptions = options;
        _runSettingsManager = runSettingsManager;
    }
    #endregion

    #region IArgumentProcessor

    /// <summary>
    /// Initializes with the argument that was provided with the command.
    /// </summary>
    /// <param name="argument">Argument that was provided with the command.</param>
    public void Initialize(string argument)
    {
        // InIsolation does not require any argument, throws exception if argument specified
        if (!string.IsNullOrWhiteSpace(argument))
        {
            throw new CommandLineException(
                string.Format(CultureInfo.CurrentCulture, CommandLineResources.InvalidInIsolationCommand, argument));
        }

        _commandLineOptions.InIsolation = true;
        _runSettingsManager.UpdateRunSettingsNode(RunSettingsPath, "true");
    }

    /// <summary>
    /// Execute.
    /// </summary>
    public ArgumentProcessorResult Execute()
    {
        // Nothing to do since we updated the parameter during initialize parameter
        return ArgumentProcessorResult.Success;
    }

    #endregion
}
