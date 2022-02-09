// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;

using Common;
using Common.Interfaces;

using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

using CommandLineResources = Resources.Resources;

/// <summary>
/// Allows the user to specify a path to load custom adapters from.
/// </summary>
internal class TestAdapterPathArgumentProcessor : IArgumentProcessor
{
    #region Constants

    /// <summary>
    /// The name of the command line argument that the ListTestsArgumentExecutor handles.
    /// </summary>
    public const string CommandName = "/TestAdapterPath";

    #endregion

    private Lazy<IArgumentProcessorCapabilities> _metadata;

    private Lazy<IArgumentExecutor> _executor;

    /// <summary>
    /// Gets the metadata.
    /// </summary>
    public Lazy<IArgumentProcessorCapabilities> Metadata
        => _metadata ??= new Lazy<IArgumentProcessorCapabilities>(() => new TestAdapterPathArgumentProcessorCapabilities());

    /// <summary>
    /// Gets or sets the executor.
    /// </summary>
    public Lazy<IArgumentExecutor> Executor
    {
        get => _executor ??= new Lazy<IArgumentExecutor>(() =>
            new TestAdapterPathArgumentExecutor(CommandLineOptions.Instance, RunSettingsManager.Instance,
                ConsoleOutput.Instance, new FileHelper()));

        set => _executor = value;
    }
}

/// <summary>
/// The argument capabilities.
/// </summary>
internal class TestAdapterPathArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
{
    public override string CommandName => TestAdapterPathArgumentProcessor.CommandName;

    public override bool AllowMultiple => true;

    public override bool IsAction => false;

    public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.TestAdapterPath;

    public override string HelpContentResourceName => CommandLineResources.TestAdapterPathHelp;

    public override HelpContentPriority HelpPriority => HelpContentPriority.TestAdapterPathArgumentProcessorHelpPriority;
}

/// <summary>
/// The argument executor.
/// </summary>
internal class TestAdapterPathArgumentExecutor : IArgumentExecutor
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

    /// <summary>
    /// Separators for multiple paths in argument.
    /// </summary>
    private readonly char[] _argumentSeparators = new[] { ';' };

    #endregion

    #region Constructor

    /// <summary>
    /// Default constructor.
    /// </summary>
    /// <param name="options"> The options. </param>
    /// <param name="testPlatform">The test platform</param>
    public TestAdapterPathArgumentExecutor(CommandLineOptions options, IRunSettingsProvider runSettingsManager, IOutput output, IFileHelper fileHelper)
    {
        Contract.Requires(options != null);

        _commandLineOptions = options;
        _runSettingsManager = runSettingsManager;
        _output = output;
        _fileHelper = fileHelper;
    }

    #endregion

    #region IArgumentExecutor

    /// <summary>
    /// Initializes with the argument that was provided with the command.
    /// </summary>
    /// <param name="argument">Argument that was provided with the command.</param>
    public void Initialize(string argument)
    {
        string invalidAdapterPathArgument = argument;

        if (string.IsNullOrWhiteSpace(argument))
        {
            throw new CommandLineException(
                string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestAdapterPathValueRequired));
        }

        string customAdaptersPath;

        try
        {
            var testAdapterPaths = new List<string>();
            var testAdapterFullPaths = new List<string>();

            // VSTS task add double quotes around TestAdapterpath. For example if user has given TestAdapter path C:\temp,
            // Then VSTS task will add TestAdapterPath as "/TestAdapterPath:\"C:\Temp\"".
            // Remove leading and trailing ' " ' chars...
            argument = argument.Trim().Trim(new char[] { '\"' });

            // Get test adapter paths from RunSettings.
            var testAdapterPathsInRunSettings = _runSettingsManager.QueryRunSettingsNode("RunConfiguration.TestAdaptersPaths");

            if (!string.IsNullOrWhiteSpace(testAdapterPathsInRunSettings))
            {
                testAdapterPaths.AddRange(SplitPaths(testAdapterPathsInRunSettings));
            }

            testAdapterPaths.AddRange(SplitPaths(argument));

            foreach (var testAdapterPath in testAdapterPaths)
            {
                // TestAdaptersPaths could contain environment variables
                var testAdapterFullPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(testAdapterPath));

                if (!_fileHelper.DirectoryExists(testAdapterFullPath))
                {
                    invalidAdapterPathArgument = testAdapterPath;
                    throw new DirectoryNotFoundException(CommandLineResources.TestAdapterPathDoesNotExist);
                }

                testAdapterFullPaths.Add(testAdapterFullPath);
            }

            customAdaptersPath = string.Join(";", testAdapterFullPaths.Distinct().ToArray());

            _runSettingsManager.UpdateRunSettingsNode("RunConfiguration.TestAdaptersPaths", customAdaptersPath);
        }
        catch (Exception e)
        {
            throw new CommandLineException(
                string.Format(CultureInfo.CurrentCulture, CommandLineResources.InvalidTestAdapterPathCommand, invalidAdapterPathArgument, e.Message));
        }

        _commandLineOptions.TestAdapterPath = customAdaptersPath;
    }

    /// <summary>
    /// Splits provided paths into array.
    /// </summary>
    /// <param name="paths">Source paths joined by semicolons.</param>
    /// <returns>Paths.</returns>
    private string[] SplitPaths(string paths)
    {
        return string.IsNullOrWhiteSpace(paths) ? (new string[] { }) : paths.Split(_argumentSeparators, StringSplitOptions.RemoveEmptyEntries);
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
