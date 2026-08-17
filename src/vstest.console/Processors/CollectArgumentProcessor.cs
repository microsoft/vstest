// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;

using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities;
using Microsoft.VisualStudio.TestPlatform.Common;
using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;

/// <summary>
/// The argument processor for enabling data collectors.
/// </summary>
internal class CollectArgumentProcessor : IArgumentProcessor
{
    /// <summary>
    /// The name of command for enabling code coverage.
    /// </summary>
    public const string CommandName = "/Collect";

    private Lazy<IArgumentProcessorCapabilities>? _metadata;
    private Lazy<IArgumentExecutor>? _executor;
    private readonly IRunSettingsProvider _runSettingsProvider;

    public CollectArgumentProcessor(IRunSettingsProvider runSettingsProvider)
    {
        _runSettingsProvider = runSettingsProvider;
    }

    /// <summary>
    /// Gets the metadata.
    /// </summary>
    public Lazy<IArgumentProcessorCapabilities> Metadata
        => _metadata ??= new Lazy<IArgumentProcessorCapabilities>(() =>
            new CollectArgumentProcessorCapabilities());

    /// <summary>
    /// Gets or sets the executor.
    /// </summary>
    public Lazy<IArgumentExecutor>? Executor
    {
        get => _executor ??= new Lazy<IArgumentExecutor>(() =>
            new CollectArgumentExecutor(_runSettingsProvider, new FileHelper()));

        set => _executor = value;
    }
}

internal class CollectArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
{
    public override string CommandName => CollectArgumentProcessor.CommandName;

    public override bool AllowMultiple => true;

    public override bool IsAction => false;

    public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.AutoUpdateRunSettings;

    public override string HelpContentResourceName => CommandLineResources.CollectArgumentHelp;

    public override HelpContentPriority HelpPriority => HelpContentPriority.CollectArgumentProcessorHelpPriority;
}

/// <inheritdoc />
internal class CollectArgumentExecutor : IArgumentExecutor
{
    private readonly IRunSettingsProvider _runSettingsManager;
    private readonly IFileHelper _fileHelper;
    internal static List<string> EnabledDataCollectors = new();
    internal CollectArgumentExecutor(IRunSettingsProvider runSettingsManager, IFileHelper fileHelper)
    {
        _runSettingsManager = runSettingsManager;
        _fileHelper = fileHelper;
    }

    /// <inheritdoc />
    public void Initialize(string? argument)
    {
        // 1. Disable all other data collectors. Enable only those data collectors that are explicitly specified by user.
        // 2. Check if Code Coverage Data Collector is specified in runsettings, if not add it and also set enable to true.

        string exceptionMessage = string.Format(CultureInfo.CurrentCulture, CommandLineResources.DataCollectorFriendlyNameInvalid, argument);

        // if argument is null or doesn't contain any element, don't do anything.
        if (argument.IsNullOrWhiteSpace())
        {
            throw new CommandLineException(exceptionMessage);
        }

        // Get collect argument list.
        var collectArgumentList = ArgumentProcessorUtilities.GetArgumentList(argument, ArgumentProcessorUtilities.SemiColonArgumentSeparator, exceptionMessage);

        // First argument is collector name. Remaining are key value pairs for configurations.
        if (collectArgumentList[0].Contains("="))
        {
            throw new CommandLineException(exceptionMessage);
        }

        if (InferRunSettingsHelper.IsTestSettingsEnabled(_runSettingsManager.ActiveRunSettings?.SettingsXml))
        {
            throw new SettingsException(string.Format(CultureInfo.CurrentCulture, CommandLineResources.CollectWithTestSettingErrorMessage, argument));
        }
        AddDataCollectorToRunSettings(collectArgumentList, _runSettingsManager, _fileHelper, exceptionMessage);

        if (string.Equals(collectArgumentList[0], MicrosoftCodeCoverageConstants.FriendlyName, StringComparison.OrdinalIgnoreCase))
        {
            // Auto-discover the Microsoft Code Coverage adapter from the NuGet global packages directory when
            // --collect:"Code Coverage" is used in DLL mode (where the MSBuild task is never invoked and
            // VSTestTraceDataCollectorDirectoryPath is never set). This is intentionally scoped to the
            // --collect CLI path only; --enable-code-coverage relies on the project's MSBuild setup.
            TryAddCodeCoverageAdapterPath(_runSettingsManager);
        }
    }

    /// <summary>
    /// Returns coverlet code base searching coverlet.collector.dll assembly inside adaptersPaths
    /// </summary>
    private static string? GetCoverletCodeBasePath(IRunSettingsProvider runSettingProvider, IFileHelper fileHelper)
    {
        foreach (string adapterPath in RunSettingsUtilities.GetTestAdaptersPaths(runSettingProvider.ActiveRunSettings?.SettingsXml))
        {
            string collectorPath = Path.Combine(adapterPath, CoverletConstants.CoverletDataCollectorCodebase);
            if (fileHelper.Exists(collectorPath))
            {
                EqtTrace.Verbose("CoverletDataCollector in-process codeBase path '{0}'", collectorPath);
                return collectorPath;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public ArgumentProcessorResult Execute()
    {
        return ArgumentProcessorResult.Success;
    }

    internal static DataCollectorSettings EnableDataCollectorUsingFriendlyName(string argument, DataCollectionRunSettings dataCollectionRunSettings)
    {

        if (!DoesDataCollectorSettingsExist(argument, dataCollectionRunSettings, out var dataCollectorSettings))
        {
            dataCollectorSettings = new DataCollectorSettings();
            dataCollectorSettings.FriendlyName = argument;
            dataCollectorSettings.IsEnabled = true;
            dataCollectionRunSettings.DataCollectorSettingsList.Add(dataCollectorSettings);
        }
        else
        {
            dataCollectorSettings.IsEnabled = true;
        }

        return dataCollectorSettings;
    }

    private static void AddDataCollectorConfigurations(string[] configurations, DataCollectorSettings dataCollectorSettings, string exceptionMessage)
    {
        if (dataCollectorSettings.Configuration == null)
        {
            XmlDocument doc = new();
            dataCollectorSettings.Configuration = doc.CreateElement("Configuration");
        }

        foreach (var configuration in configurations)
        {
            var keyValuePair = ArgumentProcessorUtilities.GetArgumentList(configuration, ArgumentProcessorUtilities.EqualNameValueSeparator, exceptionMessage);

            if (keyValuePair.Length == 2)
            {
                AddOrUpdateConfiguration(dataCollectorSettings.Configuration, keyValuePair[0], keyValuePair[1]);
            }
            else
            {
                throw new CommandLineException(exceptionMessage);
            }
        }
    }

    private static void AddOrUpdateConfiguration(XmlElement configuration, string configurationName, string configurationValue)
    {
        var existingConfigurations = configuration.GetElementsByTagName(configurationName);

        // Update existing configuration if present.
        if (existingConfigurations.Count == 0)
        {
            XmlElement newConfiguration = configuration.OwnerDocument.CreateElement(configurationName);
            newConfiguration.InnerText = configurationValue;
            configuration.AppendChild(newConfiguration);
            return;
        }

        foreach (XmlNode? existingConfiguration in existingConfigurations)
        {
            TPDebug.Assert(existingConfiguration is not null, "existingConfiguration is null");
            existingConfiguration.InnerText = configurationValue;
        }
    }

    /// <summary>
    /// Enables coverlet in-proc datacollector
    /// </summary>
    internal static void EnableCoverletInProcDataCollector(string argument, DataCollectionRunSettings dataCollectionRunSettings, IRunSettingsProvider runSettingProvider, IFileHelper fileHelper)
    {

        if (!DoesDataCollectorSettingsExist(argument, dataCollectionRunSettings, out DataCollectorSettings? dataCollectorSettings))
        {
            // Create a new setting with default values
            dataCollectorSettings = new DataCollectorSettings();
            dataCollectorSettings.FriendlyName = argument;
            dataCollectorSettings.AssemblyQualifiedName = CoverletConstants.CoverletDataCollectorAssemblyQualifiedName;
            dataCollectorSettings.CodeBase = GetCoverletCodeBasePath(runSettingProvider, fileHelper) ?? CoverletConstants.CoverletDataCollectorCodebase;
            dataCollectorSettings.IsEnabled = true;
            dataCollectionRunSettings.DataCollectorSettingsList.Add(dataCollectorSettings);
        }
        else
        {
            // Set Assembly qualified name and code base if not already set
            dataCollectorSettings.AssemblyQualifiedName ??= CoverletConstants.CoverletDataCollectorAssemblyQualifiedName;
            dataCollectorSettings.CodeBase = (dataCollectorSettings.CodeBase ?? GetCoverletCodeBasePath(runSettingProvider, fileHelper)) ?? CoverletConstants.CoverletDataCollectorCodebase;
            dataCollectorSettings.IsEnabled = true;
        }
    }

    private static bool DoesDataCollectorSettingsExist(string friendlyName,
        DataCollectionRunSettings dataCollectionRunSettings,
        [NotNullWhen(returnValue: true)] out DataCollectorSettings? dataCollectorSettings)
    {
        dataCollectorSettings = null;
        foreach (var dataCollectorSetting in dataCollectionRunSettings.DataCollectorSettingsList)
        {
            if (string.Equals(dataCollectorSetting.FriendlyName, friendlyName, StringComparison.OrdinalIgnoreCase))
            {
                dataCollectorSettings = dataCollectorSetting;
                return true;
            }
        }

        return false;
    }

    internal static void AddDataCollectorToRunSettings(string arguments, IRunSettingsProvider runSettingsManager, IFileHelper fileHelper)
    {
        AddDataCollectorToRunSettings([arguments], runSettingsManager, fileHelper, string.Empty);
    }

    internal static void AddDataCollectorToRunSettings(string[] arguments, IRunSettingsProvider runSettingsManager, IFileHelper fileHelper, string exceptionMessage)
    {
        var collectorName = arguments[0];
        var additionalConfigurations = arguments.Skip(1).ToArray();
        EnabledDataCollectors.Add(collectorName.ToLower(CultureInfo.CurrentCulture));

        var settings = runSettingsManager.ActiveRunSettings?.SettingsXml;
        if (settings == null)
        {
            runSettingsManager.AddDefaultRunSettings();
            settings = runSettingsManager.ActiveRunSettings?.SettingsXml;
        }

        var dataCollectionRunSettings = XmlRunSettingsUtilities.GetDataCollectionRunSettings(settings) ?? new DataCollectionRunSettings();
        var inProcDataCollectionRunSettings = XmlRunSettingsUtilities.GetInProcDataCollectionRunSettings(settings)
                                              ?? new DataCollectionRunSettings(
                                                  Constants.InProcDataCollectionRunSettingsName,
                                                  Constants.InProcDataCollectorsSettingName,
                                                  Constants.InProcDataCollectorSettingName);

        // Add data collectors if not already present, enable if already present.
        var dataCollectorSettings = EnableDataCollectorUsingFriendlyName(collectorName, dataCollectionRunSettings);

        if (additionalConfigurations.Length > 0)
        {
            AddDataCollectorConfigurations(additionalConfigurations, dataCollectorSettings, exceptionMessage);
        }

        runSettingsManager.UpdateRunSettingsNodeInnerXml(Constants.DataCollectionRunSettingsName, dataCollectionRunSettings.ToXml().InnerXml);

        if (string.Equals(collectorName, CoverletConstants.CoverletDataCollectorFriendlyName, StringComparison.OrdinalIgnoreCase))
        {
            // Add in-proc data collector to runsettings if coverlet code coverage is enabled
            EnableCoverletInProcDataCollector(collectorName, inProcDataCollectionRunSettings, runSettingsManager, fileHelper);
            runSettingsManager.UpdateRunSettingsNodeInnerXml(Constants.InProcDataCollectionRunSettingsName, inProcDataCollectionRunSettings.ToXml().InnerXml);
        }
    }

    internal static void AddDataCollectorFriendlyName(string friendlyName)
    {
        EnabledDataCollectors.Add(friendlyName.ToLower(CultureInfo.CurrentCulture));
    }

    /// <summary>
    /// Attempts to add the Microsoft Code Coverage adapter path to the run settings by
    /// auto-discovering the <c>microsoft.codecoverage</c> NuGet package in the global packages directory.
    /// No-ops silently when the package cannot be found.
    /// </summary>
    internal static void TryAddCodeCoverageAdapterPath(IRunSettingsProvider runSettingsManager, string? nugetPackagesOverride = null)
    {
        // Ask the run settings first, so a run that already has adapter paths pays no file system cost.
        // An entry only counts when it is an actual path, a node holding nothing but whitespace is not
        // a configured path.
        var existingPathsRaw = runSettingsManager.QueryRunSettingsNode(TestAdapterPathArgumentExecutor.RunSettingsPath);
        if (!existingPathsRaw.IsNullOrWhiteSpace() && existingPathsRaw.Split(';').Any(p => !p.IsNullOrWhiteSpace()))
        {
            // User explicitly configured adapter paths — don't clobber with auto-discovered NuGet path.
            return;
        }

        if (!TryGetCodeCoverageAdapterPath(out var ccAdapterPath, nugetPackagesOverride))
        {
            EqtTrace.Verbose("CollectArgumentExecutor.TryAddCodeCoverageAdapterPath: Microsoft.CodeCoverage package not found in NuGet global packages; skipping auto-injection.");
            return;
        }

        runSettingsManager.UpdateRunSettingsNode(TestAdapterPathArgumentExecutor.RunSettingsPath, ccAdapterPath);
        EqtTrace.Verbose("CollectArgumentExecutor.TryAddCodeCoverageAdapterPath: Injected Code Coverage adapter path '{0}'.", ccAdapterPath);
    }

    /// <summary>
    /// Finds the <c>build/</c> directory of the latest installed <c>microsoft.codecoverage</c>
    /// NuGet package. Returns <see langword="false"/> when no suitable package is found.
    /// </summary>
    internal static bool TryGetCodeCoverageAdapterPath([NotNullWhen(true)] out string? path, string? nugetPackagesOverride = null)
    {
        path = null;

        try
        {
            path = FindCodeCoverageAdapterPath(nugetPackagesOverride);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            // Auto-discovery is best effort. A packages folder we are not allowed to read, a transient
            // IO error, or an unusable NUGET_PACKAGES value must not take down the whole run.
            EqtTrace.Verbose("CollectArgumentExecutor.TryGetCodeCoverageAdapterPath: Could not inspect the NuGet global packages folder: {0}", ex);
        }

        return path is not null;
    }

    private static string? FindCodeCoverageAdapterPath(string? nugetPackagesOverride)
    {
        var nugetPackagesPath = nugetPackagesOverride ?? GetNuGetGlobalPackagesPath();
        if (nugetPackagesPath is null)
        {
            return null;
        }

        var ccPackagePath = Path.Combine(nugetPackagesPath, "microsoft.codecoverage");
        if (!Directory.Exists(ccPackagePath))
        {
            return null;
        }

        string? bestPath = null;
        Version? bestVersion = null;
        string? bestPreRelease = null;
        string bestDirectoryName = string.Empty;

        foreach (var versionDir in Directory.GetDirectories(ccPackagePath))
        {
            var buildDir = Path.Combine(versionDir, "build");
            if (!Directory.Exists(buildDir))
            {
                continue;
            }

            var directoryName = Path.GetFileName(versionDir);
            if (!TryParseNuGetVersion(directoryName, out var version, out var preRelease))
            {
                continue;
            }

            if (bestVersion is null
                || CompareNuGetVersions(version, preRelease, directoryName, bestVersion, bestPreRelease, bestDirectoryName) > 0)
            {
                bestVersion = version;
                bestPreRelease = preRelease;
                bestDirectoryName = directoryName;
                bestPath = buildDir;
            }
        }

        return bestPath;
    }

    private static string? GetNuGetGlobalPackagesPath()
    {
        var envPath = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        if (!envPath.IsNullOrEmpty())
        {
            return envPath;
        }

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return userProfile.IsNullOrEmpty() ? null : Path.Combine(userProfile, ".nuget", "packages");
    }

    /// <summary>
    /// Splits a NuGet package folder name into its numeric version and pre-release label,
    /// e.g. <c>18.5.0-preview-1</c> into <c>18.5.0</c> and <c>preview-1</c>. Build metadata
    /// (anything after <c>+</c>) is ignored, as it does not take part in version ordering.
    /// </summary>
    private static bool TryParseNuGetVersion(string versionName, [NotNullWhen(true)] out Version? version, out string? preReleaseLabel)
    {
        version = null;
        preReleaseLabel = null;

        var metadataIndex = versionName.IndexOf('+');
        var numericPart = metadataIndex >= 0 ? versionName.Substring(0, metadataIndex) : versionName;

        var dashIndex = numericPart.IndexOf('-');
        if (dashIndex >= 0)
        {
            preReleaseLabel = numericPart.Substring(dashIndex + 1);
            numericPart = numericPart.Substring(0, dashIndex);
        }

        return Version.TryParse(numericPart, out version);
    }

    /// <summary>
    /// Orders two package versions the way NuGet does: by numeric version first, then a pre-release
    /// sorts below the stable release with the same numeric version, then by pre-release label per
    /// SemVer 2.0. Equal candidates fall back to an ordinal comparison of the folder name so the
    /// winner never depends on the order <see cref="Directory.GetDirectories(string)"/> happens to
    /// return directories in.
    /// </summary>
    private static int CompareNuGetVersions(
        Version left, string? leftPreRelease, string leftName,
        Version right, string? rightPreRelease, string rightName)
    {
        var versionComparison = left.CompareTo(right);
        if (versionComparison != 0)
        {
            return versionComparison;
        }

        if (leftPreRelease is null || rightPreRelease is null)
        {
            return (leftPreRelease, rightPreRelease) switch
            {
                (null, not null) => 1,
                (not null, null) => -1,
                _ => string.CompareOrdinal(leftName, rightName),
            };
        }

        var preReleaseComparison = ComparePreReleaseLabels(leftPreRelease, rightPreRelease);
        return preReleaseComparison != 0
            ? preReleaseComparison
            : string.CompareOrdinal(leftName, rightName);
    }

    private static int ComparePreReleaseLabels(string left, string right)
    {
        var leftIdentifiers = left.Split('.');
        var rightIdentifiers = right.Split('.');

        for (int i = 0; i < Math.Min(leftIdentifiers.Length, rightIdentifiers.Length); i++)
        {
            var comparison = ComparePreReleaseIdentifiers(leftIdentifiers[i], rightIdentifiers[i]);
            if (comparison != 0)
            {
                return comparison;
            }
        }

        // When every shared identifier is equal, the label with more identifiers is the higher one.
        return leftIdentifiers.Length.CompareTo(rightIdentifiers.Length);
    }

    private static int ComparePreReleaseIdentifiers(string left, string right)
    {
        var leftIsNumeric = int.TryParse(left, NumberStyles.None, CultureInfo.InvariantCulture, out var leftNumber);
        var rightIsNumeric = int.TryParse(right, NumberStyles.None, CultureInfo.InvariantCulture, out var rightNumber);

        return (leftIsNumeric, rightIsNumeric) switch
        {
            (true, true) => leftNumber.CompareTo(rightNumber),
            // SemVer 2.0: numeric identifiers always sort below alphanumeric ones.
            (true, false) => -1,
            (false, true) => 1,
            _ => string.CompareOrdinal(left, right),
        };
    }

    internal static class MicrosoftCodeCoverageConstants
    {
        /// <summary>
        /// Microsoft Code Coverage data collector friendly name.
        /// </summary>
        public const string FriendlyName = "Code Coverage";
    }

    internal static class CoverletConstants
    {
        /// <summary>
        /// Coverlet in-proc data collector friendly name
        /// </summary>
        public const string CoverletDataCollectorFriendlyName = "XPlat Code Coverage";

        /// <summary>
        /// Coverlet in-proc data collector assembly qualified name
        /// </summary>
        public const string CoverletDataCollectorAssemblyQualifiedName = "Coverlet.Collector.DataCollection.CoverletInProcDataCollector, coverlet.collector, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

        /// <summary>
        /// Coverlet in-proc data collector code base
        /// </summary>
        public const string CoverletDataCollectorCodebase = "coverlet.collector.dll";
    }
}
