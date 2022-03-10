// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;

using System;
using System.Diagnostics.Contracts;

using CommandLine;

/// <summary>
/// Argument Executor which handles adding the source provided to the TestManager.
/// </summary>
internal class TestSourceArgumentProcessor : IArgumentProcessor
{
    #region Constants

    /// <summary>
    /// The command name.
    /// </summary>
    public const string CommandName = "/TestSource";

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
                _metadata = new Lazy<IArgumentProcessorCapabilities>(() => new TestSourceArgumentProcessorCapabilities());
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
                _executor = new Lazy<IArgumentExecutor>(() => new TestSourceArgumentExecutor(CommandLineOptions.Instance));
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
/// The test source argument processor capabilities.
/// </summary>
internal class TestSourceArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
{
    public override string CommandName => TestSourceArgumentProcessor.CommandName;

    public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.Normal;

    public override bool IsSpecialCommand => true;
}

/// <summary>
/// Argument Executor which handles adding the source provided to the TestManager.
/// </summary>
internal class TestSourceArgumentExecutor : IArgumentExecutor
{
    #region Fields

    /// <summary>
    /// Used for adding sources to the test manager.
    /// </summary>
    private readonly CommandLineOptions _testSources;

    #endregion

    #region Constructor

    /// <summary>
    /// Default constructor.
    /// </summary>
    /// <param name="testSources">
    /// The test Sources.
    /// </param>
    public TestSourceArgumentExecutor(CommandLineOptions testSources)
    {
        Contract.Requires(testSources != null);
        _testSources = testSources;
    }

    #endregion

    #region IArgumentExecutor

    /// <summary>
    /// Initializes with the argument that was provided with the command.
    /// </summary>
    /// <param name="argument">Argument that was provided with the command.</param>
    public void Initialize(string argument)
    {
        Contract.Assert(_testSources != null);
        _testSources.AddSource(argument);
    }

    /// <summary>
    /// Executes the argument processor.
    /// </summary>
    /// <returns>
    /// The <see cref="ArgumentProcessorResult"/>.
    /// </returns>
    public ArgumentProcessorResult Execute()
    {
        // Nothing to do. Our work was done during initialize.
        return ArgumentProcessorResult.Success;
    }

    #endregion

}
