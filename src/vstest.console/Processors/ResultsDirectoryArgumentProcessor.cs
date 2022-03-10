// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;

using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Security;

using Common;
using Common.Interfaces;

using Microsoft.VisualStudio.TestPlatform.Common.Utilities;

using CommandLineResources = Resources.Resources;

/// <summary>
/// Allows the user to specify a path to save test results.
/// </summary>
internal class ResultsDirectoryArgumentProcessor : IArgumentProcessor
{
    #region Constants

    /// <summary>
    /// The name of the command line argument that the ListTestsArgumentExecutor handles.
    /// </summary>
    public const string CommandName = "/ResultsDirectory";

    private const string RunSettingsPath = "RunConfiguration.ResultsDirectory";
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
                _metadata = new Lazy<IArgumentProcessorCapabilities>(() => new ResultsDirectoryArgumentProcessorCapabilities());
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
                _executor = new Lazy<IArgumentExecutor>(() => new ResultsDirectoryArgumentExecutor(CommandLineOptions.Instance, RunSettingsManager.Instance));
            }

            return _executor;
        }

        set
        {
            _executor = value;
        }
    }
}

/// <summary>
/// The argument capabilities.
/// </summary>
internal class ResultsDirectoryArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
{
    public override string CommandName => ResultsDirectoryArgumentProcessor.CommandName;

    public override bool AllowMultiple => false;

    public override bool IsAction => false;

    public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.AutoUpdateRunSettings;

    public override string HelpContentResourceName => CommandLineResources.ResultsDirectoryArgumentHelp;

    public override HelpContentPriority HelpPriority => HelpContentPriority.ResultsDirectoryArgumentProcessorHelpPriority;
}

/// <summary>
/// The argument executor.
/// </summary>
internal class ResultsDirectoryArgumentExecutor : IArgumentExecutor
{
    #region Fields

    /// <summary>
    /// Used for getting sources.
    /// </summary>
    private readonly CommandLineOptions _commandLineOptions;

    private readonly IRunSettingsProvider _runSettingsManager;

    public const string RunSettingsPath = "RunConfiguration.ResultsDirectory";

    #endregion

    #region Constructor

    /// <summary>
    /// Default constructor.
    /// </summary>
    /// <param name="options"> The options. </param>
    /// <param name="testPlatform">The test platform</param>
    public ResultsDirectoryArgumentExecutor(CommandLineOptions options, IRunSettingsProvider runSettingsManager)
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
        if (string.IsNullOrWhiteSpace(argument))
        {
            throw new CommandLineException(CommandLineResources.ResultsDirectoryValueRequired);
        }

        try
        {
            if (!Path.IsPathRooted(argument))
            {
                argument = Path.GetFullPath(argument);
            }

            var di = Directory.CreateDirectory(argument);
        }
        catch (Exception ex) when (ex is NotSupportedException or SecurityException or ArgumentException or PathTooLongException or IOException)
        {
            throw new CommandLineException(string.Format(CommandLineResources.InvalidResultsDirectoryPathCommand, argument, ex.Message));
        }

        _commandLineOptions.ResultsDirectory = argument;
        _runSettingsManager.UpdateRunSettingsNode(RunSettingsPath, argument);
    }

    /// <summary>
    /// Executes the argument processor.
    /// </summary>
    /// <returns> The <see cref="ArgumentProcessorResult"/>. </returns>
    public ArgumentProcessorResult Execute()
    {
        // Nothing to do since we updated the parameter during initialize parameter
        return ArgumentProcessorResult.Success;
    }

    #endregion
}
