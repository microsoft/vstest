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
/// Parallel Option Argument processor that allows the user to specify if tests are to be run in parallel.
/// </summary>
internal class ParallelArgumentProcessor : IArgumentProcessor
{
    #region Constants

    public const string CommandName = "/Parallel";

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
                _metadata = new Lazy<IArgumentProcessorCapabilities>(() => new ParallelArgumentProcessorCapabilities());
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
                _executor = new Lazy<IArgumentExecutor>(() => new ParallelArgumentExecutor(CommandLineOptions.Instance, RunSettingsManager.Instance));
            }

            return _executor;
        }

        set
        {
            _executor = value;
        }
    }
}

internal class ParallelArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
{
    public override string CommandName => ParallelArgumentProcessor.CommandName;

    public override bool AllowMultiple => false;

    public override bool IsAction => false;

    public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.AutoUpdateRunSettings;

    public override string HelpContentResourceName => CommandLineResources.ParallelArgumentProcessorHelp;

    public override HelpContentPriority HelpPriority => HelpContentPriority.ParallelArgumentProcessorHelpPriority;
}

/// <summary>
/// Argument Executor for the "/Parallel" command line argument.
/// </summary>
internal class ParallelArgumentExecutor : IArgumentExecutor
{
    #region Fields

    /// <summary>
    /// Used for getting sources.
    /// </summary>
    private readonly CommandLineOptions _commandLineOptions;

    private readonly IRunSettingsProvider _runSettingsManager;

    public const string RunSettingsPath = "RunConfiguration.MaxCpuCount";

    #endregion

    #region Constructor

    /// <summary>
    /// Default constructor.
    /// </summary>
    /// <param name="options"> The options. </param>
    /// <param name="runSettingsManager"> The runsettings manager. </param>
    public ParallelArgumentExecutor(CommandLineOptions options, IRunSettingsProvider runSettingsManager)
    {
        Contract.Requires(options != null);
        Contract.Requires(runSettingsManager != null);
        _commandLineOptions = options;
        _runSettingsManager = runSettingsManager;
    }

    #endregion

    #region IArgumentExecutor

    /// <summary>
    /// Initializes with the argument that was provided with the command.
    /// </summary>
    /// <param name="argument">Argument that was provided with the command.</param>
    public void Initialize(string argument)
    {
        // parallel does not require any argument, throws exception if argument specified
        if (!string.IsNullOrWhiteSpace(argument))
        {
            throw new CommandLineException(
                string.Format(CultureInfo.CurrentCulture, CommandLineResources.InvalidParallelCommand, argument));
        }

        _commandLineOptions.Parallel = true;
        _runSettingsManager.UpdateRunSettingsNode(RunSettingsPath, "0");
    }

    /// <summary>
    /// The output path is already set, return success.
    /// </summary>
    /// <returns> The <see cref="ArgumentProcessorResult"/> Success </returns>
    public ArgumentProcessorResult Execute()
    {
        return ArgumentProcessorResult.Success;
    }

    #endregion
}
