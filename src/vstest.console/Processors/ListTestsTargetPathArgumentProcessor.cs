// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;

using System;
using System.Diagnostics.Contracts;
using System.Globalization;

/// <summary>
//  An argument processor to provide path to the file for listing fully qualified tests.
/// To be used only with ListFullyQualifiedTests
/// </summary>
internal class ListTestsTargetPathArgumentProcessor : IArgumentProcessor
{
    #region Constants

    public const string CommandName = "/ListTestsTargetPath";

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
                _metadata = new Lazy<IArgumentProcessorCapabilities>(() => new ListTestsTargetPathArgumentProcessorCapabilities());
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
                _executor = new Lazy<IArgumentExecutor>(() => new ListTestsTargetPathArgumentExecutor(CommandLineOptions.Instance));
            }

            return _executor;
        }

        set
        {
            _executor = value;
        }
    }
}

internal class ListTestsTargetPathArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
{
    public override string CommandName => ListTestsTargetPathArgumentProcessor.CommandName;

    public override bool AllowMultiple => false;

    public override bool IsAction => false;

    public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.Normal;
}

internal class ListTestsTargetPathArgumentExecutor : IArgumentExecutor
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
    public ListTestsTargetPathArgumentExecutor(CommandLineOptions options)
    {
        Contract.Requires(options != null);
        _commandLineOptions = options;
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
            // Not adding this string to resources because this processor is only used internally.
            throw new CommandLineException(string.Format(CultureInfo.CurrentUICulture, "ListTestsTargetPath is required with ListFullyQualifiedTests!"));
        }

        _commandLineOptions.ListTestsTargetPath = argument;
    }

    /// <summary>
    /// The ListTestsTargetPath is already set, return success.
    /// </summary>
    /// <returns> The <see cref="ArgumentProcessorResult"/> Success </returns>
    public ArgumentProcessorResult Execute()
    {
        return ArgumentProcessorResult.Success;
    }
    #endregion
}
