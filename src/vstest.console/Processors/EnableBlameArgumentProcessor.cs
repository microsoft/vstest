﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

using Utilities;
using Common;
using Common.Interfaces;
using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
using ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
using PlatformAbstractions;
using PlatformAbstractions.Interfaces;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

using CommandLineResources = Resources.Resources;

internal class EnableBlameArgumentProcessor : IArgumentProcessor
{
    /// <summary>
    /// The name of the command line argument that the ListTestsArgumentExecutor handles.
    /// </summary>
    public const string CommandName = "/Blame";

    private Lazy<IArgumentProcessorCapabilities> _metadata;

    private Lazy<IArgumentExecutor> _executor;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnableBlameArgumentProcessor"/> class.
    /// </summary>
    public EnableBlameArgumentProcessor()
    {
    }

    public Lazy<IArgumentProcessorCapabilities> Metadata
    {
        get
        {
            if (_metadata == null)
            {
                _metadata = new Lazy<IArgumentProcessorCapabilities>(() => new EnableBlameArgumentProcessorCapabilities());
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
                _executor = new Lazy<IArgumentExecutor>(() => new EnableBlameArgumentExecutor(RunSettingsManager.Instance, new PlatformEnvironment(), new FileHelper()));
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
internal class EnableBlameArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
{
    public override string CommandName => EnableBlameArgumentProcessor.CommandName;

    public override bool AllowMultiple => false;

    public override bool IsAction => false;

    public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.Logging;

    public override string HelpContentResourceName => CommandLineResources.EnableBlameUsage;

    public override HelpContentPriority HelpPriority => HelpContentPriority.EnableDiagArgumentProcessorHelpPriority;
}

/// <summary>
/// The argument executor.
/// </summary>
internal class EnableBlameArgumentExecutor : IArgumentExecutor
{
    /// <summary>
    /// Blame logger and data collector friendly name
    /// </summary>
    private static readonly string BlameFriendlyName = "blame";

    /// <summary>
    /// Run settings manager
    /// </summary>
    private readonly IRunSettingsProvider _runSettingsManager;

    /// <summary>
    /// Platform environment
    /// </summary>
    private readonly IEnvironment _environment;

    /// <summary>
    /// For file related operation
    /// </summary>
    private readonly IFileHelper _fileHelper;

    internal EnableBlameArgumentExecutor(IRunSettingsProvider runSettingsManager, IEnvironment environment, IFileHelper fileHelper)
    {
        _runSettingsManager = runSettingsManager;
        _environment = environment;
        Output = ConsoleOutput.Instance;
        _fileHelper = fileHelper;
    }

    internal IOutput Output { get; set; }


    #region IArgumentExecutor

    /// <summary>
    /// Initializes with the argument that was provided with the command.
    /// </summary>
    /// <param name="argument">Argument that was provided with the command.</param>
    public void Initialize(string argument)
    {
        var enableDump = false;
        var enableHangDump = false;
        var exceptionMessage = string.Format(CultureInfo.CurrentUICulture, CommandLineResources.InvalidBlameArgument, argument);
        Dictionary<string, string> collectDumpParameters = null;

        if (!string.IsNullOrWhiteSpace(argument))
        {
            // Get blame argument list.
            var blameArgumentList = ArgumentProcessorUtilities.GetArgumentList(argument, ArgumentProcessorUtilities.SemiColonArgumentSeparator, exceptionMessage);
            Func<string, bool> isDumpCollect = a => Constants.BlameCollectDumpKey.Equals(a, StringComparison.OrdinalIgnoreCase);
            Func<string, bool> isHangDumpCollect = a => Constants.BlameCollectHangDumpKey.Equals(a, StringComparison.OrdinalIgnoreCase);

            // Get collect dump key.
            var hasCollectDumpKey = blameArgumentList.Any(isDumpCollect);
            var hasCollectHangDumpKey = blameArgumentList.Any(isHangDumpCollect);

            // Check if dump should be enabled or not.
            enableDump = hasCollectDumpKey && IsDumpCollectionSupported();

            // Check if dump should be enabled or not.
            enableHangDump = hasCollectHangDumpKey && IsHangDumpCollectionSupported();

            if (!enableDump && !enableHangDump)
            {
                Output.Warning(false, string.Format(CultureInfo.CurrentUICulture, CommandLineResources.BlameIncorrectOption, argument));
            }
            else
            {
                // Get collect dump parameters.
                var collectDumpParameterArgs = blameArgumentList.Where(a => !isDumpCollect(a) && !isHangDumpCollect(a));
                collectDumpParameters = ArgumentProcessorUtilities.GetArgumentParameters(collectDumpParameterArgs, ArgumentProcessorUtilities.EqualNameValueSeparator, exceptionMessage);
            }
        }

        // Initialize blame.
        InitializeBlame(enableDump, enableHangDump, collectDumpParameters);
    }

    /// <summary>
    /// Executes the argument processor.
    /// </summary>
    /// <returns>The <see cref="ArgumentProcessorResult"/>.</returns>
    public ArgumentProcessorResult Execute()
    {
        // Nothing to do since we updated the logger and data collector list in initialize
        return ArgumentProcessorResult.Success;
    }

    /// <summary>
    /// Initialize blame.
    /// </summary>
    /// <param name="enableCrashDump">Enable dump.</param>
    /// <param name="blameParameters">Blame parameters.</param>
    private void InitializeBlame(bool enableCrashDump, bool enableHangDump, Dictionary<string, string> collectDumpParameters)
    {
        // Add Blame Logger
        LoggerUtilities.AddLoggerToRunSettings(BlameFriendlyName, null, _runSettingsManager);

        // Add Blame Data Collector
        CollectArgumentExecutor.AddDataCollectorToRunSettings(BlameFriendlyName, _runSettingsManager, _fileHelper);


        // Add default run settings if required.
        if (_runSettingsManager.ActiveRunSettings?.SettingsXml == null)
        {
            _runSettingsManager.AddDefaultRunSettings();
        }
        var settings = _runSettingsManager.ActiveRunSettings?.SettingsXml;

        // Get results directory from RunSettingsManager
        var resultsDirectory = GetResultsDirectory(settings);

        // Get data collection run settings. Create if not present.
        var dataCollectionRunSettings = XmlRunSettingsUtilities.GetDataCollectionRunSettings(settings);
        if (dataCollectionRunSettings == null)
        {
            dataCollectionRunSettings = new DataCollectionRunSettings();
        }

        // Create blame configuration element.
        var xmlDocument = new XmlDocument();
        var outernode = xmlDocument.CreateElement("Configuration");
        var node = xmlDocument.CreateElement("ResultsDirectory");
        outernode.AppendChild(node);
        node.InnerText = resultsDirectory;

        // Add collect dump node in configuration element.
        if (enableCrashDump)
        {
            var dumpParameters = collectDumpParameters
                .Where(p => new[] { "CollectAlways", "DumpType" }.Contains(p.Key, StringComparer.OrdinalIgnoreCase))
                .ToDictionary(p => p.Key, p => p.Value, StringComparer.OrdinalIgnoreCase);

            if (!dumpParameters.ContainsKey("DumpType"))
            {
                dumpParameters.Add("DumpType", "Full");
            }

            AddCollectDumpNode(dumpParameters, xmlDocument, outernode);
        }

        // Add collect hang dump node in configuration element.
        if (enableHangDump)
        {
            var hangDumpParameters = collectDumpParameters
                .Where(p => new[] { "TestTimeout", "HangDumpType" }.Contains(p.Key, StringComparer.OrdinalIgnoreCase))
                .ToDictionary(p => p.Key, p => p.Value, StringComparer.OrdinalIgnoreCase);

            if (!hangDumpParameters.ContainsKey("TestTimeout"))
            {
                hangDumpParameters.Add("TestTimeout", TimeSpan.FromHours(1).TotalMilliseconds.ToString());
            }

            if (!hangDumpParameters.ContainsKey("HangDumpType"))
            {
                hangDumpParameters.Add("HangDumpType", "Full");
            }

            AddCollectHangDumpNode(hangDumpParameters, xmlDocument, outernode);
        }

        // Add blame configuration element to blame collector.
        foreach (var item in dataCollectionRunSettings.DataCollectorSettingsList)
        {
            if (item.FriendlyName.Equals(BlameFriendlyName))
            {
                item.Configuration = outernode;
            }
        }

        // Update run settings.
        _runSettingsManager.UpdateRunSettingsNodeInnerXml(Constants.DataCollectionRunSettingsName, dataCollectionRunSettings.ToXml().InnerXml);
    }

    /// <summary>
    /// Get results directory.
    /// </summary>
    /// <param name="settings">Settings xml.</param>
    /// <returns>Results directory.</returns>
    private string GetResultsDirectory(string settings)
    {
        string resultsDirectory = null;
        if (settings != null)
        {
            try
            {
                RunConfiguration runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(settings);
                resultsDirectory = RunSettingsUtilities.GetTestResultsDirectory(runConfiguration);
            }
            catch (SettingsException se)
            {
                EqtTrace.Error("EnableBlameArgumentProcessor: Unable to get the test results directory: Error {0}", se);
            }
        }

        return resultsDirectory;
    }

    /// <summary>
    /// Checks if crash dump collection is supported.
    /// </summary>
    /// <returns>Dump collection supported flag.</returns>
    private bool IsDumpCollectionSupported()
    {
        var dumpCollectionSupported =
            _environment.OperatingSystem == PlatformOperatingSystem.Unix ||
            _environment.OperatingSystem == PlatformOperatingSystem.OSX ||
            (_environment.OperatingSystem == PlatformOperatingSystem.Windows
             && _environment.Architecture != PlatformArchitecture.ARM64
             && _environment.Architecture != PlatformArchitecture.ARM);

        if (!dumpCollectionSupported)
        {
            Output.Warning(false, CommandLineResources.BlameCollectDumpNotSupportedForPlatform);
        }

        return dumpCollectionSupported;
    }

    /// <summary>
    /// Checks if hang dump collection is supported.
    /// </summary>
    /// <returns>Dump collection supported flag.</returns>
    private bool IsHangDumpCollectionSupported()
    {
        var dumpCollectionSupported = true;

        if (!dumpCollectionSupported)
        {
            Output.Warning(false, CommandLineResources.BlameCollectDumpTestTimeoutNotSupportedForPlatform);
        }

        return dumpCollectionSupported;
    }

    /// <summary>
    /// Adds collect dump node in outer node.
    /// </summary>
    /// <param name="parameters">Parameters.</param>
    /// <param name="xmlDocument">Xml document.</param>
    /// <param name="outernode">Outer node.</param>
    private void AddCollectDumpNode(Dictionary<string, string> parameters, XmlDocument xmlDocument, XmlElement outernode)
    {
        var dumpNode = xmlDocument.CreateElement(Constants.BlameCollectDumpKey);
        if (parameters != null && parameters.Count > 0)
        {
            foreach (KeyValuePair<string, string> entry in parameters)
            {
                var attribute = xmlDocument.CreateAttribute(entry.Key);
                attribute.Value = entry.Value;
                dumpNode.Attributes.Append(attribute);
            }
        }
        outernode.AppendChild(dumpNode);
    }

    /// <summary>
    /// Adds collect dump node in outer node.
    /// </summary>
    /// <param name="parameters">Parameters.</param>
    /// <param name="xmlDocument">Xml document.</param>
    /// <param name="outernode">Outer node.</param>
    private void AddCollectHangDumpNode(Dictionary<string, string> parameters, XmlDocument xmlDocument, XmlElement outernode)
    {
        var dumpNode = xmlDocument.CreateElement(Constants.CollectDumpOnTestSessionHang);
        if (parameters != null && parameters.Count > 0)
        {
            foreach (KeyValuePair<string, string> entry in parameters)
            {
                var attribute = xmlDocument.CreateAttribute(entry.Key);
                attribute.Value = entry.Value;
                dumpNode.Attributes.Append(attribute);
            }
        }
        outernode.AppendChild(dumpNode);
    }

    #endregion
}
