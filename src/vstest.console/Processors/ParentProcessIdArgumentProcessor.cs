// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;

using System;
using System.Diagnostics.Contracts;

using CommandLineResources = Resources.Resources;

/// <summary>
/// Argument Processor for the "--ParentProcessId|/ParentProcessId" command line argument.
/// </summary>
internal class ParentProcessIdArgumentProcessor : IArgumentProcessor
{
    #region Constants

    /// <summary>
    /// The name of the command line argument that the ParentProcessIdArgumentExecutor handles.
    /// </summary>
    public const string CommandName = "/ParentProcessId";

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
                _metadata = new Lazy<IArgumentProcessorCapabilities>(() => new ParentProcessIdArgumentProcessorCapabilities());
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
                _executor = new Lazy<IArgumentExecutor>(() =>
                    new ParentProcessIdArgumentExecutor(CommandLineOptions.Instance));
            }

            return _executor;
        }

        set
        {
            _executor = value;
        }
    }
}

internal class ParentProcessIdArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
{
    public override string CommandName => ParentProcessIdArgumentProcessor.CommandName;

    public override bool AllowMultiple => false;

    public override bool IsAction => false;

    public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.DesignMode;

    public override string HelpContentResourceName => CommandLineResources.ParentProcessIdArgumentHelp;

    public override HelpContentPriority HelpPriority => HelpContentPriority.ParentProcessIdArgumentProcessorHelpPriority;
}

/// <summary>
/// Argument Executor for the "/ParentProcessId" command line argument.
/// </summary>
internal class ParentProcessIdArgumentExecutor : IArgumentExecutor
{
    #region Fields

    /// <summary>
    /// Used for getting sources.
    /// </summary>
    private readonly CommandLineOptions _commandLineOptions;

    #endregion

    #region Constructor

    /// <summary>
    /// Default constructor.
    /// </summary>
    /// <param name="options">
    /// The options.
    /// </param>
    public ParentProcessIdArgumentExecutor(CommandLineOptions options)
    {
        Contract.Requires(options != null);
        _commandLineOptions = options;
    }

    #endregion

    /// <summary>
    /// Initializes with the argument that was provided with the command.
    /// </summary>
    /// <param name="argument">Argument that was provided with the command.</param>
    public void Initialize(string argument)
    {
        if (string.IsNullOrWhiteSpace(argument) || !int.TryParse(argument, out int parentProcessId))
        {
            throw new CommandLineException(CommandLineResources.InvalidParentProcessIdArgument);
        }

        _commandLineOptions.ParentProcessId = parentProcessId;
    }

    /// <summary>
    /// ParentProcessId is already set, return success.
    /// </summary>
    /// <returns> The <see cref="ArgumentProcessorResult"/> Success </returns>
    public ArgumentProcessorResult Execute()
    {
        // Nothing to do here, the work was done in initialization.
        return ArgumentProcessorResult.Success;
    }
}
