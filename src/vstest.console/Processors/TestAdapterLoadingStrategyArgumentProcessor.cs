// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;

using System;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;

using Microsoft.VisualStudio.TestPlatform.Common;
using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;

/// <summary>
/// Allows the user to specify a order of loading custom adapters from.
/// </summary>
internal class TestAdapterLoadingStrategyArgumentProcessor : IArgumentProcessor
{
    #region Constants

    /// <summary>
    /// The name of the command line argument that the TestAdapterLoadingStrategyArgumentProcessor handles.
    /// </summary>
    public const string CommandName = "/TestAdapterLoadingStrategy";

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
                _metadata = new Lazy<IArgumentProcessorCapabilities>(() => new TestAdapterLoadingStrategyArgumentProcessorCapabilities());
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
                _executor = new Lazy<IArgumentExecutor>(() => new TestAdapterLoadingStrategyArgumentExecutor(CommandLineOptions.Instance, RunSettingsManager.Instance, ConsoleOutput.Instance, new FileHelper()));
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
internal class TestAdapterLoadingStrategyArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
{
    public override string CommandName => TestAdapterLoadingStrategyArgumentProcessor.CommandName;

    public override bool AllowMultiple => false;

    public override bool IsAction => false;

    public override bool AlwaysExecute => true;

    public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.TestAdapterLoadingStrategy;

    public override string HelpContentResourceName => CommandLineResources.TestAdapterLoadingStrategyHelp;

    public override HelpContentPriority HelpPriority => HelpContentPriority.TestAdapterLoadingStrategyArgumentProcessorHelpPriority;
}

/// <summary>
/// The argument executor.
/// </summary>
internal class TestAdapterLoadingStrategyArgumentExecutor : IArgumentExecutor
{
    #region Fields
    /// <summary>
    /// Used for getting sources.
    /// </summary>
    private readonly CommandLineOptions _commandLineOptions;

    /// <summary>
    /// Run settings provider.
    /// </summary>
    private readonly IRunSettingsProvider _runSettingsManager;

    /// <summary>
    /// Used for sending output.
    /// </summary>
    private readonly IOutput _output;

    /// <summary>
    /// For file related operation
    /// </summary>
    private readonly IFileHelper _fileHelper;


    private static readonly string[] EmptyStringArray =
#if NET451
                                                        new string[0];
#else
                                                        Array.Empty<string>();
#endif

    #endregion

    public const string RunSettingsPath = "RunConfiguration.TestAdapterLoadingStrategy";
    public const string DefaultStrategy = "Default";
    public const string ExplicitStrategy = "Explicit";

    /// <summary>
    /// Default constructor.
    /// </summary>
    /// <param name="options"> The options. </param>
    /// <param name="testPlatform">The test platform</param>
    public TestAdapterLoadingStrategyArgumentExecutor(CommandLineOptions options!!, IRunSettingsProvider runSettingsManager!!, IOutput output!!, IFileHelper fileHelper!!)
    {
        Contract.Requires(options != null);

        _commandLineOptions = options;
        _runSettingsManager = runSettingsManager;
        _output = output;
        _fileHelper = fileHelper;
    }

    #region IArgumentExecutor

    /// <summary>
    /// Initializes with the argument that was provided with the command.
    /// </summary>
    /// <param name="argument">Argument that was provided with the command.</param>
    public void Initialize(string argument)
    {
        ExtractStrategy(argument, out var strategy);

        if (strategy == TestAdapterLoadingStrategy.Recursive)
        {
            throw new CommandLineException(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestAdapterLoadingStrategyValueInvalidRecursive, $"{nameof(TestAdapterLoadingStrategy.Explicit)}, {nameof(TestAdapterLoadingStrategy.NextToSource)}"));
        }

        if (strategy == TestAdapterLoadingStrategy.Default)
        {
            InitializeDefaultStrategy();
            return;
        }

        InitializeStrategy(strategy);
    }

    private void ExtractStrategy(string value, out TestAdapterLoadingStrategy strategy)
    {
        value ??= _runSettingsManager.QueryRunSettingsNode(RunSettingsPath);

        if (string.IsNullOrWhiteSpace(value))
        {
            strategy = TestAdapterLoadingStrategy.Default;
            return;
        }

        if (!Enum.TryParse(value, out strategy))
        {
            throw new CommandLineException(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestAdapterLoadingStrategyValueInvalid, value));
        }
    }

    private void InitializeDefaultStrategy()
    {
        ValidateTestAdapterPaths(TestAdapterLoadingStrategy.Default);

        SetStrategy(TestAdapterLoadingStrategy.Default);
    }

    private void InitializeStrategy(TestAdapterLoadingStrategy strategy)
    {
        ValidateTestAdapterPaths(strategy);

        if (!_commandLineOptions.TestAdapterPathsSet && (strategy & TestAdapterLoadingStrategy.Explicit) == TestAdapterLoadingStrategy.Explicit)
        {
            throw new CommandLineException(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestAdapterPathValueRequiredWhenStrategyXIsUsed, ExplicitStrategy));
        }

        SetStrategy(strategy);
    }

    private void ForceIsolation()
    {
        if (_commandLineOptions.InIsolation)
        {
            return;
        }

        _commandLineOptions.InIsolation = true;
        _runSettingsManager.UpdateRunSettingsNode(InIsolationArgumentExecutor.RunSettingsPath, "true");
    }

    private void ValidateTestAdapterPaths(TestAdapterLoadingStrategy strategy)
    {
        var testAdapterPaths = _commandLineOptions.TestAdapterPath ?? EmptyStringArray;
        if (!_commandLineOptions.TestAdapterPathsSet)
        {
            testAdapterPaths = TestAdapterPathArgumentExecutor.SplitPaths(_runSettingsManager.QueryRunSettingsNode(TestAdapterPathArgumentExecutor.RunSettingsPath)).Union(testAdapterPaths).Distinct().ToArray();
        }

        for (var i = 0; i < testAdapterPaths.Length; i++)
        {
            var adapterPath = testAdapterPaths[i];
            var testAdapterPath = _fileHelper.GetFullPath(Environment.ExpandEnvironmentVariables(adapterPath));

            if (strategy == TestAdapterLoadingStrategy.Default)
            {
                if (!_fileHelper.DirectoryExists(testAdapterPath))
                {
                    throw new CommandLineException(
                        string.Format(CultureInfo.CurrentCulture, CommandLineResources.InvalidTestAdapterPathCommand, adapterPath, CommandLineResources.TestAdapterPathDoesNotExist)
                    );
                }
            }

            testAdapterPaths[i] = testAdapterPath;
        }

        _runSettingsManager.UpdateRunSettingsNode(TestAdapterPathArgumentExecutor.RunSettingsPath, string.Join(";", testAdapterPaths));
    }

    private void SetStrategy(TestAdapterLoadingStrategy strategy)
    {
        var adapterStrategy = strategy.ToString();

        _commandLineOptions.TestAdapterLoadingStrategy = strategy;
        _runSettingsManager.UpdateRunSettingsNode(RunSettingsPath, adapterStrategy);
        if ((strategy & TestAdapterLoadingStrategy.Explicit) == TestAdapterLoadingStrategy.Explicit)
        {
            ForceIsolation();
        }
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
