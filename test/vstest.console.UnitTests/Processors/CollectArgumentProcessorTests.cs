// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.IO;

using Microsoft.VisualStudio.TestPlatform.CommandLine;
using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
using Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests;
using Microsoft.VisualStudio.TestPlatform.Common;
using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using static Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.CollectArgumentExecutor;

namespace vstest.console.UnitTests.Processors;

[TestClass]
public class CollectArgumentProcessorTests
{
    private readonly TestableRunSettingsProvider _settingsProvider;
    private readonly CollectArgumentExecutor _executor;

    private readonly string _defaultRunSettings = string.Join(Environment.NewLine,
        "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
        "<RunSettings>",
        " <RunConfiguration>",
        " <TestAdaptersPaths>c:\\AdapterFolderPath</TestAdaptersPaths>",
        " </RunConfiguration>",
        " <DataCollectionRunSettings>",
        "    <DataCollectors >{0}</DataCollectors>",
        "  </DataCollectionRunSettings>",
        "</RunSettings>");

    public CollectArgumentProcessorTests()
    {
        _settingsProvider = new TestableRunSettingsProvider();
        _executor = new CollectArgumentExecutor(_settingsProvider, new Mock<IFileHelper>().Object);
        EnabledDataCollectors.Clear();
    }

    [TestMethod]
    public void GetMetadataShouldReturnCollectArgumentProcessorCapabilities()
    {
        var processor = new CollectArgumentProcessor(new TestableRunSettingsProvider());
        Assert.IsTrue(processor.Metadata.Value is CollectArgumentProcessorCapabilities);
    }

    [TestMethod]
    public void GetExecuterShouldReturnCollectArgumentProcessorCapabilities()
    {
        var processor = new CollectArgumentProcessor(new TestableRunSettingsProvider());
        Assert.IsTrue(processor.Executor!.Value is CollectArgumentExecutor);
    }

    #region CollectArgumentProcessorCapabilities tests

    [TestMethod]
    public void CapabilitiesShouldReturnAppropriateProperties()
    {
        var capabilities = new CollectArgumentProcessorCapabilities();

        Assert.AreEqual("/Collect", capabilities.CommandName);
        var expected =
            $"--Collect|/Collect:<DataCollector FriendlyName>{Environment.NewLine}      Enables data collector for the test run. More info here : https://aka.ms/vstest-collect";
        Assert.AreEqual(expected.NormalizeLineEndings().ShowWhiteSpace(),
            capabilities.HelpContentResourceName.NormalizeLineEndings().ShowWhiteSpace());

        Assert.AreEqual(HelpContentPriority.CollectArgumentProcessorHelpPriority, capabilities.HelpPriority);
        Assert.IsFalse(capabilities.IsAction);
        Assert.AreEqual(ArgumentProcessorPriority.AutoUpdateRunSettings, capabilities.Priority);

        Assert.IsTrue(capabilities.AllowMultiple);
        Assert.IsFalse(capabilities.AlwaysExecute);
        Assert.IsFalse(capabilities.IsSpecialCommand);
    }

    #endregion

    #region CollectArgumentExecutor tests

    [TestMethod]
    public void InitializeShouldThrowIfArgumentIsNull()
    {
        Assert.ThrowsExactly<CommandLineException>(() => _executor.Initialize(null));
    }

    [TestMethod]
    public void InitializeShouldNotThrowIfArgumentIsEmpty()
    {
        Assert.ThrowsExactly<CommandLineException>(() => _executor.Initialize(string.Empty));
    }

    [TestMethod]
    public void InitializeShouldNotThrowIfArgumentIsWhiteSpace()
    {
        Assert.ThrowsExactly<CommandLineException>(() => _executor.Initialize(" "));
    }

    [TestMethod]
    public void InitializeShouldThrowExceptionWhenTestSettingsIsEnabled()
    {
        string runsettingsString = @"<RunSettings>
                                        <MSTest>
                                            <SettingsFile>C:\temp.testsettings</SettingsFile>
                                            <ForcedLegacyMode>true</ForcedLegacyMode>
                                        </MSTest>
                                    </RunSettings>";
        var runsettings = new RunSettings();
        runsettings.LoadSettingsXml(runsettingsString);
        _settingsProvider.SetActiveRunSettings(runsettings);

        var ex = Assert.ThrowsExactly<SettingsException>(() => _executor.Initialize("MyDataCollector"));
        Assert.AreEqual(
            "--Collect|/Collect:\"MyDataCollector\" is not supported if test run is configured using testsettings.",
            ex.Message);
    }

    [TestMethod]
    public void InitializeShouldCreateEntryForDataCollectorInRunSettingsIfNotAlreadyPresent()
    {
        var runsettingsString = string.Format(CultureInfo.CurrentCulture, _defaultRunSettings, "");
        var runsettings = new RunSettings();
        runsettings.LoadSettingsXml(runsettingsString);
        _settingsProvider.SetActiveRunSettings(runsettings);

        _executor.Initialize("MyDataCollector");

        Assert.IsNotNull(_settingsProvider.ActiveRunSettings);
        Assert.AreEqual(string.Join(Environment.NewLine,
            "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
            "<RunSettings>",
            "  <RunConfiguration>",
            "    <TestAdaptersPaths>c:\\AdapterFolderPath</TestAdaptersPaths>",
            "  </RunConfiguration>",
            "  <DataCollectionRunSettings>",
            "    <DataCollectors>",
            "      <DataCollector friendlyName=\"MyDataCollector\" enabled=\"True\" />",
            "    </DataCollectors>",
            "  </DataCollectionRunSettings>",
            "</RunSettings>"), _settingsProvider.ActiveRunSettings!.SettingsXml);
    }

    [TestMethod]
    public void InitializeShouldEnableDataCollectorIfDisabledInRunSettings()
    {
        var runsettingsString = string.Format(CultureInfo.CurrentCulture, _defaultRunSettings,
            "<DataCollector friendlyName=\"MyDataCollector\" enabled=\"False\" />");
        var runsettings = new RunSettings();
        runsettings.LoadSettingsXml(runsettingsString);
        _settingsProvider.SetActiveRunSettings(runsettings);

        _executor.Initialize("MyDataCollector");

        Assert.AreEqual(string.Join(Environment.NewLine,
            "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
            "<RunSettings>",
            "  <RunConfiguration>",
            "    <TestAdaptersPaths>c:\\AdapterFolderPath</TestAdaptersPaths>",
            "  </RunConfiguration>",
            "  <DataCollectionRunSettings>",
            "    <DataCollectors>",
            "      <DataCollector friendlyName=\"MyDataCollector\" enabled=\"True\" />",
            "    </DataCollectors>",
            "  </DataCollectionRunSettings>",
            "</RunSettings>"), _settingsProvider.ActiveRunSettings!.SettingsXml);
    }

    [TestMethod]
    public void InitializeShouldNotDisableOtherDataCollectorsIfEnabled()
    {
        var runsettingsString = string.Format(CultureInfo.CurrentCulture, _defaultRunSettings,
            "<DataCollector friendlyName=\"MyDataCollector\" enabled=\"False\" /><DataCollector friendlyName=\"MyDataCollector1\" enabled=\"True\" />");
        var runsettings = new RunSettings();
        runsettings.LoadSettingsXml(runsettingsString);
        _settingsProvider.SetActiveRunSettings(runsettings);

        _executor.Initialize("MyDataCollector");
        _executor.Initialize("MyDataCollector2");

        Assert.AreEqual(string.Join(Environment.NewLine,
            "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
            "<RunSettings>",
            "  <RunConfiguration>",
            "    <TestAdaptersPaths>c:\\AdapterFolderPath</TestAdaptersPaths>",
            "  </RunConfiguration>",
            "  <DataCollectionRunSettings>",
            "    <DataCollectors>",
            "      <DataCollector friendlyName=\"MyDataCollector\" enabled=\"True\" />",
            "      <DataCollector friendlyName=\"MyDataCollector1\" enabled=\"True\" />",
            "      <DataCollector friendlyName=\"MyDataCollector2\" enabled=\"True\" />",
            "    </DataCollectors>",
            "  </DataCollectionRunSettings>",
            "</RunSettings>"), _settingsProvider.ActiveRunSettings!.SettingsXml);
    }

    [TestMethod]
    public void InitializeShouldNotEnableOtherDataCollectorsIfDisabled()
    {
        var runsettingsString = string.Format(CultureInfo.CurrentCulture, _defaultRunSettings,
            "<DataCollector friendlyName=\"MyDataCollector\" enabled=\"False\" /><DataCollector friendlyName=\"MyDataCollector1\" enabled=\"False\" />");
        var runsettings = new RunSettings();
        runsettings.LoadSettingsXml(runsettingsString);
        _settingsProvider.SetActiveRunSettings(runsettings);

        _executor.Initialize("MyDataCollector");
        _executor.Initialize("MyDataCollector2");

        Assert.AreEqual(string.Join(Environment.NewLine,
            "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
            "<RunSettings>",
            "  <RunConfiguration>",
            "    <TestAdaptersPaths>c:\\AdapterFolderPath</TestAdaptersPaths>",
            "  </RunConfiguration>",
            "  <DataCollectionRunSettings>",
            "    <DataCollectors>",
            "      <DataCollector friendlyName=\"MyDataCollector\" enabled=\"True\" />",
            "      <DataCollector friendlyName=\"MyDataCollector1\" enabled=\"False\" />",
            "      <DataCollector friendlyName=\"MyDataCollector2\" enabled=\"True\" />",
            "    </DataCollectors>",
            "  </DataCollectionRunSettings>",
            "</RunSettings>"), _settingsProvider.ActiveRunSettings!.SettingsXml);
    }

    [TestMethod]
    public void InitializeShouldEnableMultipleCollectorsWhenCalledMoreThanOnce()
    {
        var runsettingsString = string.Format(CultureInfo.CurrentCulture, _defaultRunSettings, string.Empty);
        var runsettings = new RunSettings();
        runsettings.LoadSettingsXml(runsettingsString);
        _settingsProvider.SetActiveRunSettings(runsettings);
        _executor.Initialize("MyDataCollector");
        _executor.Initialize("MyDataCollector1");

        Assert.AreEqual(string.Join(Environment.NewLine,
            "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
            "<RunSettings>",
            "  <RunConfiguration>",
            "    <TestAdaptersPaths>c:\\AdapterFolderPath</TestAdaptersPaths>",
            "  </RunConfiguration>",
            "  <DataCollectionRunSettings>",
            "    <DataCollectors>",
            "      <DataCollector friendlyName=\"MyDataCollector\" enabled=\"True\" />",
            "      <DataCollector friendlyName=\"MyDataCollector1\" enabled=\"True\" />",
            "    </DataCollectors>",
            "  </DataCollectionRunSettings>",
            "</RunSettings>"), _settingsProvider.ActiveRunSettings!.SettingsXml);
    }

    [TestMethod]
    public void InitializeShouldAddOutProcAndInprocCollectorWhenXPlatCodeCoverageIsEnabled()
    {
        var runsettingsString = string.Format(CultureInfo.CurrentCulture, _defaultRunSettings, string.Empty);
        var runsettings = new RunSettings();
        runsettings.LoadSettingsXml(runsettingsString);
        _settingsProvider.SetActiveRunSettings(runsettings);
        _executor.Initialize("XPlat Code Coverage");

        Assert.AreEqual(string.Join(Environment.NewLine,
            "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
            "<RunSettings>",
            "  <RunConfiguration>",
            "    <TestAdaptersPaths>c:\\AdapterFolderPath</TestAdaptersPaths>",
            "  </RunConfiguration>",
            "  <DataCollectionRunSettings>",
            "    <DataCollectors>",
            "      <DataCollector friendlyName=\"XPlat Code Coverage\" enabled=\"True\" />",
            "    </DataCollectors>",
            "  </DataCollectionRunSettings>",
            "  <InProcDataCollectionRunSettings>",
            "    <InProcDataCollectors>",
            "      <InProcDataCollector assemblyQualifiedName=\"Coverlet.Collector.DataCollection.CoverletInProcDataCollector, coverlet.collector, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null\" friendlyName=\"XPlat Code Coverage\" enabled=\"True\" codebase=\"coverlet.collector.dll\" />",
            "    </InProcDataCollectors>",
            "  </InProcDataCollectionRunSettings>",
            "</RunSettings>"), _settingsProvider.ActiveRunSettings!.SettingsXml);
    }

    [TestMethod]
    public void UpdateXPlatCodeCoverageCodebaseWithFullPathFromTestAdaptersPaths_Found()
    {
        var runsettingsString = string.Format(CultureInfo.CurrentCulture, _defaultRunSettings, string.Empty);
        var runsettings = new RunSettings();
        runsettings.LoadSettingsXml(runsettingsString);
        _settingsProvider.SetActiveRunSettings(runsettings);
        Mock<IFileHelper> fileHelper = new();
        fileHelper.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
        CollectArgumentExecutor executor = new(_settingsProvider, fileHelper.Object);
        executor.Initialize("XPlat Code Coverage");
        executor.Execute();

        Assert.AreEqual(string.Join(Environment.NewLine,
            "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
            "<RunSettings>",
            "  <RunConfiguration>",
            "    <TestAdaptersPaths>c:\\AdapterFolderPath</TestAdaptersPaths>",
            "  </RunConfiguration>",
            "  <DataCollectionRunSettings>",
            "    <DataCollectors>",
            "      <DataCollector friendlyName=\"XPlat Code Coverage\" enabled=\"True\" />",
            "    </DataCollectors>",
            "  </DataCollectionRunSettings>",
            "  <InProcDataCollectionRunSettings>",
            "    <InProcDataCollectors>",
            $"      <InProcDataCollector assemblyQualifiedName=\"Coverlet.Collector.DataCollection.CoverletInProcDataCollector, coverlet.collector, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null\" friendlyName=\"XPlat Code Coverage\" enabled=\"True\" codebase=\"c:\\AdapterFolderPath{Path.DirectorySeparatorChar}coverlet.collector.dll\" />",
            "    </InProcDataCollectors>",
            "  </InProcDataCollectionRunSettings>",
            "</RunSettings>").ShowWhiteSpace(), _settingsProvider.ActiveRunSettings!.SettingsXml!.ShowWhiteSpace());
    }

    [TestMethod]
    public void UpdageXPlatCodeCoverageCodebaseWithFullPathFromTestAdaptersPaths_NotFound()
    {
        var runsettingsString = string.Format(CultureInfo.CurrentCulture, _defaultRunSettings, string.Empty);
        var runsettings = new RunSettings();
        runsettings.LoadSettingsXml(runsettingsString);
        _settingsProvider.SetActiveRunSettings(runsettings);
        Mock<IFileHelper> fileHelper = new();
        fileHelper.Setup(f => f.Exists(It.IsAny<string>())).Returns(false);
        CollectArgumentExecutor executor = new(_settingsProvider, fileHelper.Object);
        executor.Initialize("XPlat Code Coverage");
        executor.Execute();

        Assert.AreEqual(string.Join(Environment.NewLine,
            "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
            "<RunSettings>",
            "  <RunConfiguration>",
            "    <TestAdaptersPaths>c:\\AdapterFolderPath</TestAdaptersPaths>",
            "  </RunConfiguration>",
            "  <DataCollectionRunSettings>",
            "    <DataCollectors>",
            "      <DataCollector friendlyName=\"XPlat Code Coverage\" enabled=\"True\" />",
            "    </DataCollectors>",
            "  </DataCollectionRunSettings>",
            "  <InProcDataCollectionRunSettings>",
            "    <InProcDataCollectors>",
            "      <InProcDataCollector assemblyQualifiedName=\"Coverlet.Collector.DataCollection.CoverletInProcDataCollector, coverlet.collector, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null\" friendlyName=\"XPlat Code Coverage\" enabled=\"True\" codebase=\"coverlet.collector.dll\" />",
            "    </InProcDataCollectors>",
            "  </InProcDataCollectionRunSettings>",
            "</RunSettings>"), _settingsProvider.ActiveRunSettings!.SettingsXml);
    }

    [TestMethod]
    public void InitializeXPlatCodeCoverageShouldNotChangeExistingDataCollectors()
    {
        var runsettingsString = string.Join(Environment.NewLine,
            "<?xml version =\"1.0\" encoding=\"utf-16\"?>",
            "<RunSettings>",
            "  <DataCollectionRunSettings>",
            "    <DataCollectors>",
            "      <DataCollector friendlyName=\"MyDataCollector\" enabled=\"True\" />",
            "      <DataCollector friendlyName=\"MyDataCollector1\" enabled=\"True\" />",
            "      <DataCollector friendlyName=\"MyDataCollector2\" enabled=\"True\" />",
            "    </DataCollectors>",
            "  </DataCollectionRunSettings>",
            "</RunSettings>");
        runsettingsString = string.Format(CultureInfo.CurrentCulture, runsettingsString, string.Empty);
        var runsettings = new RunSettings();
        runsettings.LoadSettingsXml(runsettingsString);
        _settingsProvider.SetActiveRunSettings(runsettings);
        _executor.Initialize("XPlat Code Coverage");

        Assert.AreEqual(string.Join(Environment.NewLine,
            "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
            "<RunSettings>",
            "  <DataCollectionRunSettings>",
            "    <DataCollectors>",
            "      <DataCollector friendlyName=\"MyDataCollector\" enabled=\"True\" />",
            "      <DataCollector friendlyName=\"MyDataCollector1\" enabled=\"True\" />",
            "      <DataCollector friendlyName=\"MyDataCollector2\" enabled=\"True\" />",
            "      <DataCollector friendlyName=\"XPlat Code Coverage\" enabled=\"True\" />",
            "    </DataCollectors>",
            "  </DataCollectionRunSettings>",
            "  <InProcDataCollectionRunSettings>",
            "    <InProcDataCollectors>",
            $"      <InProcDataCollector assemblyQualifiedName=\"{CoverletConstants.CoverletDataCollectorAssemblyQualifiedName}\" friendlyName=\"{CoverletConstants.CoverletDataCollectorFriendlyName}\" enabled=\"True\" codebase=\"{CoverletConstants.CoverletDataCollectorCodebase}\" />",
            "    </InProcDataCollectors>",
            "  </InProcDataCollectionRunSettings>",
            "</RunSettings>"), _settingsProvider.ActiveRunSettings!.SettingsXml);
    }

    [TestMethod]
    public void InitializeXPlatCodeCoverageShouldNotChangeExistingXPlatDataCollectorSetting()
    {
        var runsettingsString = string.Join(Environment.NewLine,
            "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
            "<RunSettings>",
            "  <DataCollectionRunSettings>",
            "    <DataCollectors>",
            "      <DataCollector friendlyName=\"MyDataCollector\" enabled=\"True\" />",
            "      <DataCollector friendlyName=\"MyDataCollector1\" enabled=\"True\" />",
            "      <DataCollector friendlyName=\"MyDataCollector2\" enabled=\"True\" />",
            "      <DataCollector friendlyName=\"XPlat Code Coverage\" enabled=\"True\" />",
            "    </DataCollectors>",
            "  </DataCollectionRunSettings>",
            "  </RunSettings>");
        runsettingsString = string.Format(CultureInfo.CurrentCulture, runsettingsString, string.Empty);
        var runsettings = new RunSettings();
        runsettings.LoadSettingsXml(runsettingsString);
        _settingsProvider.SetActiveRunSettings(runsettings);
        _executor.Initialize("XPlat Code Coverage");

        Assert.AreEqual(string.Join(Environment.NewLine,
            "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
            "<RunSettings>",
            "  <DataCollectionRunSettings>",
            "    <DataCollectors>",
            "      <DataCollector friendlyName=\"MyDataCollector\" enabled=\"True\" />",
            "      <DataCollector friendlyName=\"MyDataCollector1\" enabled=\"True\" />",
            "      <DataCollector friendlyName=\"MyDataCollector2\" enabled=\"True\" />",
            "      <DataCollector friendlyName=\"XPlat Code Coverage\" enabled=\"True\" />",
            "    </DataCollectors>",
            "  </DataCollectionRunSettings>",
            "  <InProcDataCollectionRunSettings>",
            "    <InProcDataCollectors>",
            $"      <InProcDataCollector assemblyQualifiedName=\"{CoverletConstants.CoverletDataCollectorAssemblyQualifiedName}\" friendlyName=\"XPlat Code Coverage\" enabled=\"True\" codebase=\"{CoverletConstants.CoverletDataCollectorCodebase}\" />",
            "    </InProcDataCollectors>",
            "  </InProcDataCollectionRunSettings>",
            "</RunSettings>"), _settingsProvider.ActiveRunSettings!.SettingsXml);
    }

    [TestMethod]
    public void InitializeXPlatCodeCoverageShouldNotChangeExistingXPlatInProcDataCollectorSetting()
    {
        var runsettingsString = string.Join(Environment.NewLine,
            "<?xml version =\"1.0\" encoding=\"utf-16\"?>",
            "<RunSettings>",
            "  <DataCollectionRunSettings>",
            "    <DataCollectors>",
            "      <DataCollector friendlyName=\"MyDataCollector\" enabled=\"True\" />",
            "      <DataCollector friendlyName=\"MyDataCollector1\" enabled=\"True\" />",
            "      <DataCollector friendlyName=\"MyDataCollector2\" enabled=\"True\" />",
            "      <DataCollector friendlyName=\"XPlat Code Coverage\" enabled=\"True\" />",
            "    </DataCollectors>",
            "  </DataCollectionRunSettings>",
            "  <InProcDataCollectionRunSettings>",
            "    <InProcDataCollectors>",
            "      <InProcDataCollector assemblyQualifiedName=\"Microsoft.TestPlatform.Extensions.CoverletCoverageDataCollector.CoverletCoverageDataCollector, Microsoft.TestPlatform.Extensions.CoverletCoverageDataCollector, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\" friendlyName=\"XPlat Code Coverage\" enabled=\"False\" codebase=\"inprocdatacollector.dll\" />",
            "    </InProcDataCollectors>",
            "  </InProcDataCollectionRunSettings>",
            "</RunSettings>");
        runsettingsString = string.Format(CultureInfo.CurrentCulture, runsettingsString, string.Empty);
        var runsettings = new RunSettings();
        runsettings.LoadSettingsXml(runsettingsString);
        _settingsProvider.SetActiveRunSettings(runsettings);
        _executor.Initialize("XPlat Code Coverage");

        Assert.AreEqual(string.Join(Environment.NewLine,
            "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
            "<RunSettings>",
            "  <DataCollectionRunSettings>",
            "    <DataCollectors>",
            "      <DataCollector friendlyName=\"MyDataCollector\" enabled=\"True\" />",
            "      <DataCollector friendlyName=\"MyDataCollector1\" enabled=\"True\" />",
            "      <DataCollector friendlyName=\"MyDataCollector2\" enabled=\"True\" />",
            "      <DataCollector friendlyName=\"XPlat Code Coverage\" enabled=\"True\" />",
            "    </DataCollectors>",
            "  </DataCollectionRunSettings>",
            "  <InProcDataCollectionRunSettings>",
            "    <InProcDataCollectors>",
            "      <InProcDataCollector assemblyQualifiedName=\"Microsoft.TestPlatform.Extensions.CoverletCoverageDataCollector.CoverletCoverageDataCollector, Microsoft.TestPlatform.Extensions.CoverletCoverageDataCollector, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\" friendlyName=\"XPlat Code Coverage\" enabled=\"True\" codebase=\"inprocdatacollector.dll\" />",
            "    </InProcDataCollectors>",
            "  </InProcDataCollectionRunSettings>",
            "</RunSettings>"), _settingsProvider.ActiveRunSettings!.SettingsXml);
    }

    [TestMethod]
    public void InitializeXPlatCodeCoverageShouldAddXPlatOutProcProcDataCollectorSetting()
    {
        var runsettingsString = string.Join(Environment.NewLine,
            "<?xml version =\"1.0\" encoding=\"utf-16\"?>",
            "<RunSettings>",
            "  <DataCollectionRunSettings>",
            "    <DataCollectors>",
            "      <DataCollector friendlyName=\"MyDataCollector\" enabled=\"True\" />",
            "      <DataCollector friendlyName=\"MyDataCollector1\" enabled=\"True\" />",
            "      <DataCollector friendlyName=\"MyDataCollector2\" enabled=\"True\" />",
            "    </DataCollectors>",
            "  </DataCollectionRunSettings>",
            "  <InProcDataCollectionRunSettings>",
            "    <InProcDataCollectors>",
            $"      <InProcDataCollector assemblyQualifiedName=\"{CoverletConstants.CoverletDataCollectorAssemblyQualifiedName}\" friendlyName=\"{CoverletConstants.CoverletDataCollectorFriendlyName}\" enabled=\"False\" codebase=\"inprocdatacollector.dll\" />",
            "    </InProcDataCollectors>",
            "  </InProcDataCollectionRunSettings>",
            "</RunSettings>");
        runsettingsString = string.Format(CultureInfo.CurrentCulture, runsettingsString, string.Empty);
        var runsettings = new RunSettings();
        runsettings.LoadSettingsXml(runsettingsString);
        _settingsProvider.SetActiveRunSettings(runsettings);
        _executor.Initialize("XPlat Code Coverage");

        Assert.AreEqual(string.Join(Environment.NewLine,
            "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
            "<RunSettings>",
            "  <DataCollectionRunSettings>",
            "    <DataCollectors>",
            "      <DataCollector friendlyName=\"MyDataCollector\" enabled=\"True\" />",
            "      <DataCollector friendlyName=\"MyDataCollector1\" enabled=\"True\" />",
            "      <DataCollector friendlyName=\"MyDataCollector2\" enabled=\"True\" />",
            $"      <DataCollector friendlyName=\"{CoverletConstants.CoverletDataCollectorFriendlyName}\" enabled=\"True\" />",
            "    </DataCollectors>",
            "  </DataCollectionRunSettings>",
            "  <InProcDataCollectionRunSettings>",
            "    <InProcDataCollectors>",
            $"      <InProcDataCollector assemblyQualifiedName=\"{CoverletConstants.CoverletDataCollectorAssemblyQualifiedName}\" friendlyName=\"{CoverletConstants.CoverletDataCollectorFriendlyName}\" enabled=\"True\" codebase=\"inprocdatacollector.dll\" />",
            "    </InProcDataCollectors>",
            "  </InProcDataCollectionRunSettings>",
            "</RunSettings>"), _settingsProvider.ActiveRunSettings!.SettingsXml);
    }

    [TestMethod]
    public void InitializeXPlatCodeCoverageShouldAddXPlatInProcProcDataCollectoPropertiesIfNotPresent()
    {
        var runsettingsString = $"<?xml version =\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n <DataCollectionRunSettings>\r\n    <DataCollectors>\r\n      <DataCollector friendlyName=\"MyDataCollector\" enabled=\"True\" />\r\n      <DataCollector friendlyName=\"MyDataCollector1\" enabled=\"True\" />\r\n      <DataCollector friendlyName=\"MyDataCollector2\" enabled=\"True\" />\r\n    </DataCollectors>\r\n  </DataCollectionRunSettings>\r\n  <InProcDataCollectionRunSettings>\r\n    <InProcDataCollectors>\r\n      <InProcDataCollector assemblyQualifiedName=\"{CoverletConstants.CoverletDataCollectorAssemblyQualifiedName}\" friendlyName=\"{CoverletConstants.CoverletDataCollectorFriendlyName}\" enabled=\"False\" />\r\n    </InProcDataCollectors>\r\n  </InProcDataCollectionRunSettings>\r\n</RunSettings>";
        runsettingsString = string.Format(CultureInfo.CurrentCulture, runsettingsString, string.Empty);
        var runsettings = new RunSettings();
        runsettings.LoadSettingsXml(runsettingsString);
        _settingsProvider.SetActiveRunSettings(runsettings);
        Mock<IFileHelper> fileHelper = new();
        fileHelper.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
        CollectArgumentExecutor executor = new(_settingsProvider, fileHelper.Object);
        executor.Initialize("XPlat Code Coverage");

        Assert.AreEqual(string.Join(Environment.NewLine,
            "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
            "<RunSettings>",
            "  <DataCollectionRunSettings>",
            "    <DataCollectors>",
            "      <DataCollector friendlyName=\"MyDataCollector\" enabled=\"True\" />",
            "      <DataCollector friendlyName=\"MyDataCollector1\" enabled=\"True\" />",
            "      <DataCollector friendlyName=\"MyDataCollector2\" enabled=\"True\" />",
            $"      <DataCollector friendlyName=\"{CoverletConstants.CoverletDataCollectorFriendlyName}\" enabled=\"True\" />",
            "    </DataCollectors>",
            "  </DataCollectionRunSettings>",
            "  <InProcDataCollectionRunSettings>",
            "    <InProcDataCollectors>",
            $"      <InProcDataCollector assemblyQualifiedName=\"{CoverletConstants.CoverletDataCollectorAssemblyQualifiedName}\" friendlyName=\"{CoverletConstants.CoverletDataCollectorFriendlyName}\" enabled=\"True\" codebase=\"{CoverletConstants.CoverletDataCollectorCodebase}\" />",
            "    </InProcDataCollectors>",
            "  </InProcDataCollectionRunSettings>",
            "</RunSettings>"), _settingsProvider.ActiveRunSettings!.SettingsXml);
    }

    [TestMethod]
    public void InitializeXPlatCodeCoverageShouldAddXPlatInProcProcDataCollectoPropertiesIfNotPresent_NoTestAdaptersPaths()
    {
        var runsettingsString = $"<?xml version =\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <RunConfiguration>\r\n </RunConfiguration>\r\n <DataCollectionRunSettings>\r\n    <DataCollectors>\r\n      <DataCollector friendlyName=\"MyDataCollector\" enabled=\"True\" />\r\n      <DataCollector friendlyName=\"MyDataCollector1\" enabled=\"True\" />\r\n      <DataCollector friendlyName=\"MyDataCollector2\" enabled=\"True\" />\r\n    </DataCollectors>\r\n  </DataCollectionRunSettings>\r\n  <InProcDataCollectionRunSettings>\r\n    <InProcDataCollectors>\r\n      <InProcDataCollector assemblyQualifiedName=\"{CoverletConstants.CoverletDataCollectorAssemblyQualifiedName}\" friendlyName=\"{CoverletConstants.CoverletDataCollectorFriendlyName}\" enabled=\"False\" />\r\n    </InProcDataCollectors>\r\n  </InProcDataCollectionRunSettings>\r\n</RunSettings>";
        runsettingsString = string.Format(CultureInfo.CurrentCulture, runsettingsString, string.Empty);
        var runsettings = new RunSettings();
        runsettings.LoadSettingsXml(runsettingsString);
        _settingsProvider.SetActiveRunSettings(runsettings);
        Mock<IFileHelper> fileHelper = new();
        // Suppose file exists to be sure that we won't find adapter path on runsettings config
        fileHelper.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
        CollectArgumentExecutor executor = new(_settingsProvider, fileHelper.Object);
        executor.Initialize("XPlat Code Coverage");

        Assert.AreEqual(string.Join(Environment.NewLine,
            "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
            "<RunSettings>",
            "  <RunConfiguration>",
            "  </RunConfiguration>",
            "  <DataCollectionRunSettings>",
            "    <DataCollectors>",
            "      <DataCollector friendlyName=\"MyDataCollector\" enabled=\"True\" />",
            "      <DataCollector friendlyName=\"MyDataCollector1\" enabled=\"True\" />",
            "      <DataCollector friendlyName=\"MyDataCollector2\" enabled=\"True\" />",
            $"      <DataCollector friendlyName=\"{CoverletConstants.CoverletDataCollectorFriendlyName}\" enabled=\"True\" />",
            "    </DataCollectors>",
            "  </DataCollectionRunSettings>",
            "  <InProcDataCollectionRunSettings>",
            "    <InProcDataCollectors>",
            $"      <InProcDataCollector assemblyQualifiedName=\"{CoverletConstants.CoverletDataCollectorAssemblyQualifiedName}\" friendlyName=\"{CoverletConstants.CoverletDataCollectorFriendlyName}\" enabled=\"True\" codebase=\"{CoverletConstants.CoverletDataCollectorCodebase}\" />",
            "    </InProcDataCollectors>",
            "  </InProcDataCollectionRunSettings>",
            "</RunSettings>"), _settingsProvider.ActiveRunSettings!.SettingsXml);
    }

    [TestMethod]
    public void InitializeShouldThrowExceptionWhenInvalidCollectorNameProvided()
    {
        var runsettingsString = string.Format(CultureInfo.CurrentCulture, _defaultRunSettings, "");
        var runsettings = new RunSettings();
        runsettings.LoadSettingsXml(runsettingsString);
        _settingsProvider.SetActiveRunSettings(runsettings);

        Assert.ThrowsExactly<CommandLineException>(() => _executor.Initialize("MyDataCollector=SomeSetting"));
    }

    [TestMethod]
    public void InitializeShouldThrowExceptionWhenInvalidConfigurationsProvided()
    {
        var runsettingsString = string.Format(CultureInfo.CurrentCulture, _defaultRunSettings, "");
        var runsettings = new RunSettings();
        runsettings.LoadSettingsXml(runsettingsString);
        _settingsProvider.SetActiveRunSettings(runsettings);

        Assert.ThrowsExactly<CommandLineException>(() => _executor.Initialize("MyDataCollector;SomeSetting"));
    }

    [TestMethod]
    public void InitializeShouldCreateConfigurationsForNewDataCollectorInRunSettings()
    {
        var runsettingsString = string.Format(CultureInfo.CurrentCulture, _defaultRunSettings, "");
        var runsettings = new RunSettings();
        runsettings.LoadSettingsXml(runsettingsString);
        _settingsProvider.SetActiveRunSettings(runsettings);

        _executor.Initialize("MyDataCollector;SomeSetting=SomeValue;AnotherSetting=AnotherValue");

        Assert.IsNotNull(_settingsProvider.ActiveRunSettings);
        Assert.AreEqual(string.Join(Environment.NewLine,
            "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
            "<RunSettings>",
            "  <RunConfiguration>",
            "    <TestAdaptersPaths>c:\\AdapterFolderPath</TestAdaptersPaths>",
            "  </RunConfiguration>",
            "  <DataCollectionRunSettings>",
            "    <DataCollectors>",
            "      <DataCollector friendlyName=\"MyDataCollector\" enabled=\"True\">",
            "        <Configuration>",
            "          <SomeSetting>SomeValue</SomeSetting>",
            "          <AnotherSetting>AnotherValue</AnotherSetting>",
            "        </Configuration>",
            "      </DataCollector>",
            "    </DataCollectors>",
            "  </DataCollectionRunSettings>",
            "</RunSettings>"), _settingsProvider.ActiveRunSettings!.SettingsXml);
    }

    [TestMethod]
    public void InitializeShouldCreateConfigurationsForExistingDataCollectorInRunSettings()
    {
        var runsettingsString = string.Format(CultureInfo.CurrentCulture, _defaultRunSettings,
            "<DataCollector friendlyName=\"MyDataCollector\" enabled=\"False\">" +
            "  <Configuration>" +
            "    <SomeSetting>SomeValue</SomeSetting>" +
            "  </Configuration>" +
            "</DataCollector>");
        var runsettings = new RunSettings();
        runsettings.LoadSettingsXml(runsettingsString);
        _settingsProvider.SetActiveRunSettings(runsettings);

        _executor.Initialize("MyDataCollector;AnotherSetting=AnotherValue");

        Assert.AreEqual(string.Join(Environment.NewLine,
            "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
            "<RunSettings>",
            "  <RunConfiguration>",
            "    <TestAdaptersPaths>c:\\AdapterFolderPath</TestAdaptersPaths>",
            "  </RunConfiguration>",
            "  <DataCollectionRunSettings>",
            "    <DataCollectors>",
            "      <DataCollector friendlyName=\"MyDataCollector\" enabled=\"True\">",
            "        <Configuration>",
            "          <SomeSetting>SomeValue</SomeSetting>",
            "          <AnotherSetting>AnotherValue</AnotherSetting>",
            "        </Configuration>",
            "      </DataCollector>",
            "    </DataCollectors>",
            "  </DataCollectionRunSettings>",
            "</RunSettings>"), _settingsProvider.ActiveRunSettings!.SettingsXml);
    }

    [TestMethod]
    public void InitializeShouldUpdateConfigurationsForExistingDataCollectorInRunSettings()
    {
        var runsettingsString = string.Format(CultureInfo.CurrentCulture, _defaultRunSettings,
            "<DataCollector friendlyName=\"MyDataCollector\" enabled=\"False\">" +
            "  <Configuration>" +
            "    <SomeSetting>SomeValue</SomeSetting>" +
            "  </Configuration>" +
            "</DataCollector>");
        var runsettings = new RunSettings();
        runsettings.LoadSettingsXml(runsettingsString);
        _settingsProvider.SetActiveRunSettings(runsettings);

        _executor.Initialize("MyDataCollector;SomeSetting=AnotherValue");

        Assert.AreEqual(string.Join(Environment.NewLine,
            "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
            "<RunSettings>",
            "  <RunConfiguration>",
            "    <TestAdaptersPaths>c:\\AdapterFolderPath</TestAdaptersPaths>",
            "  </RunConfiguration>",
            "  <DataCollectionRunSettings>",
            "    <DataCollectors>",
            "      <DataCollector friendlyName=\"MyDataCollector\" enabled=\"True\">",
            "        <Configuration>",
            "          <SomeSetting>AnotherValue</SomeSetting>",
            "        </Configuration>",
            "      </DataCollector>",
            "    </DataCollectors>",
            "  </DataCollectionRunSettings>",
            "</RunSettings>"), _settingsProvider.ActiveRunSettings!.SettingsXml);
    }
    #endregion

    #region TryGetCodeCoverageAdapterPath tests

    [TestMethod]
    public void TryGetCodeCoverageAdapterPath_ReturnsFalse_WhenCcPackageDirectoryDoesNotExist()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            bool result = CollectArgumentExecutor.TryGetCodeCoverageAdapterPath(out var path, nugetPackagesOverride: tempDir);

            Assert.IsFalse(result);
            Assert.IsNull(path);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [TestMethod]
    public void TryGetCodeCoverageAdapterPath_ReturnsFalse_WhenNoBuildDirectoryExists()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            Directory.CreateDirectory(Path.Combine(tempDir, "microsoft.codecoverage", "18.0.0"));

            bool result = CollectArgumentExecutor.TryGetCodeCoverageAdapterPath(out var path, nugetPackagesOverride: tempDir);

            Assert.IsFalse(result);
            Assert.IsNull(path);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [TestMethod]
    public void TryGetCodeCoverageAdapterPath_ReturnsLatestVersion_WhenMultipleVersionsInstalled()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            Directory.CreateDirectory(Path.Combine(tempDir, "microsoft.codecoverage", "17.12.0", "build"));
            Directory.CreateDirectory(Path.Combine(tempDir, "microsoft.codecoverage", "18.5.0", "build"));
            Directory.CreateDirectory(Path.Combine(tempDir, "microsoft.codecoverage", "18.0.0", "build"));

            bool result = CollectArgumentExecutor.TryGetCodeCoverageAdapterPath(out var path, nugetPackagesOverride: tempDir);

            Assert.IsTrue(result);
            Assert.IsTrue(path!.EndsWith(Path.Combine("18.5.0", "build"), StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [TestMethod]
    public void TryGetCodeCoverageAdapterPath_PicksHigherNumericVersion_EvenWhenItIsPreRelease()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            Directory.CreateDirectory(Path.Combine(tempDir, "microsoft.codecoverage", "18.5.0", "build"));
            Directory.CreateDirectory(Path.Combine(tempDir, "microsoft.codecoverage", "18.6.0-preview-1", "build"));

            bool result = CollectArgumentExecutor.TryGetCodeCoverageAdapterPath(out var path, nugetPackagesOverride: tempDir);

            Assert.IsTrue(result);
            // 18.6.0-preview-1 has a higher numeric version (18.6 > 18.5), so it wins even though it's a pre-release.
            Assert.IsTrue(path!.EndsWith(Path.Combine("18.6.0-preview-1", "build"), StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [TestMethod]
    public void TryGetCodeCoverageAdapterPath_StableBeatsPreReleaseWhenSameNumerics()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            Directory.CreateDirectory(Path.Combine(tempDir, "microsoft.codecoverage", "18.5.0-preview-1", "build"));
            Directory.CreateDirectory(Path.Combine(tempDir, "microsoft.codecoverage", "18.5.0", "build"));

            bool result = CollectArgumentExecutor.TryGetCodeCoverageAdapterPath(out var path, nugetPackagesOverride: tempDir);

            Assert.IsTrue(result);
            // Stable release wins over a pre-release with the same numeric version (NuGet SemVer: pre-release < release).
            Assert.IsTrue(path!.EndsWith(Path.Combine("18.5.0", "build"), StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [TestMethod]
    public void TryGetCodeCoverageAdapterPath_PicksHigherPreRelease_WhenOnlyPreReleasesWithSameNumericsAreInstalled()
    {
        // Two pre-releases of the same numeric version are a tie on numerics alone, so the winner used
        // to depend on the order the file system returned the directories in. Create them in both orders
        // and require the same answer.
        foreach (var creationOrder in new[] { new[] { "18.6.0-preview-1", "18.6.0-preview-2" }, new[] { "18.6.0-preview-2", "18.6.0-preview-1" } })
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                foreach (var version in creationOrder)
                {
                    Directory.CreateDirectory(Path.Combine(tempDir, "microsoft.codecoverage", version, "build"));
                }

                bool result = CollectArgumentExecutor.TryGetCodeCoverageAdapterPath(out var path, nugetPackagesOverride: tempDir);

                Assert.IsTrue(result);
                Assert.IsTrue(path!.EndsWith(Path.Combine("18.6.0-preview-2", "build"), StringComparison.OrdinalIgnoreCase),
                    $"Expected 18.6.0-preview-2 to win regardless of creation order, but got '{path}'.");
            }
            finally
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [TestMethod]
    public void TryGetCodeCoverageAdapterPath_ComparesDottedPreReleaseIdentifiersNumerically()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            Directory.CreateDirectory(Path.Combine(tempDir, "microsoft.codecoverage", "18.6.0-rc.2", "build"));
            Directory.CreateDirectory(Path.Combine(tempDir, "microsoft.codecoverage", "18.6.0-rc.10", "build"));

            bool result = CollectArgumentExecutor.TryGetCodeCoverageAdapterPath(out var path, nugetPackagesOverride: tempDir);

            Assert.IsTrue(result);
            // SemVer 2.0 compares dot-separated numeric identifiers as numbers, so rc.10 > rc.2.
            Assert.IsTrue(path!.EndsWith(Path.Combine("18.6.0-rc.10", "build"), StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [TestMethod]
    public void TryGetCodeCoverageAdapterPath_ComparesAllFourVersionComponents()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            Directory.CreateDirectory(Path.Combine(tempDir, "microsoft.codecoverage", "18.5.0.1", "build"));
            Directory.CreateDirectory(Path.Combine(tempDir, "microsoft.codecoverage", "18.5.0.2", "build"));

            bool result = CollectArgumentExecutor.TryGetCodeCoverageAdapterPath(out var path, nugetPackagesOverride: tempDir);

            Assert.IsTrue(result);
            Assert.IsTrue(path!.EndsWith(Path.Combine("18.5.0.2", "build"), StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [TestMethod]
    public void TryGetCodeCoverageAdapterPath_DoesNotThrow_WhenVersionDirectoryHasTwoComponents()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            Directory.CreateDirectory(Path.Combine(tempDir, "microsoft.codecoverage", "18.5", "build"));
            Directory.CreateDirectory(Path.Combine(tempDir, "microsoft.codecoverage", "18.5.0", "build"));

            bool result = CollectArgumentExecutor.TryGetCodeCoverageAdapterPath(out var path, nugetPackagesOverride: tempDir);

            Assert.IsTrue(result);
            Assert.IsTrue(path!.EndsWith(Path.Combine("18.5.0", "build"), StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [TestMethod]
    public void TryGetCodeCoverageAdapterPath_IgnoresBuildMetadata()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            Directory.CreateDirectory(Path.Combine(tempDir, "microsoft.codecoverage", "18.6.0-preview-1", "build"));
            Directory.CreateDirectory(Path.Combine(tempDir, "microsoft.codecoverage", "18.6.0+abc123", "build"));

            bool result = CollectArgumentExecutor.TryGetCodeCoverageAdapterPath(out var path, nugetPackagesOverride: tempDir);

            Assert.IsTrue(result);
            // Build metadata takes no part in ordering, so "18.6.0+abc123" is the stable 18.6.0 and beats the pre-release.
            Assert.IsTrue(path!.EndsWith(Path.Combine("18.6.0+abc123", "build"), StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [TestMethod]
    public void TryAddCodeCoverageAdapterPath_AddsPathToRunSettings_WhenCcPackageFound()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            var buildDir = Path.Combine(tempDir, "microsoft.codecoverage", "18.5.0", "build");
            Directory.CreateDirectory(buildDir);

            var settingsProvider = new TestableRunSettingsProvider();
            settingsProvider.AddDefaultRunSettings();

            CollectArgumentExecutor.TryAddCodeCoverageAdapterPath(settingsProvider, nugetPackagesOverride: tempDir);

            var xml = settingsProvider.ActiveRunSettings?.SettingsXml ?? string.Empty;
            Assert.IsTrue(xml.Contains(buildDir, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [TestMethod]
    public void TryAddCodeCoverageAdapterPath_SkipsInjection_WhenAdapterPathAlreadyPresent()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            var buildDir = Path.Combine(tempDir, "microsoft.codecoverage", "18.5.0", "build");
            Directory.CreateDirectory(buildDir);

            var settingsProvider = new TestableRunSettingsProvider();
            settingsProvider.AddDefaultRunSettings();

            CollectArgumentExecutor.TryAddCodeCoverageAdapterPath(settingsProvider, nugetPackagesOverride: tempDir);
            CollectArgumentExecutor.TryAddCodeCoverageAdapterPath(settingsProvider, nugetPackagesOverride: tempDir);

            var xml = settingsProvider.ActiveRunSettings?.SettingsXml ?? string.Empty;
            Assert.AreEqual(1, CountOccurrences(xml, buildDir));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [TestMethod]
    public void TryAddCodeCoverageAdapterPath_SkipsInjection_WhenExplicitAdapterPathAlreadySet()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            var buildDir = Path.Combine(tempDir, "microsoft.codecoverage", "18.5.0", "build");
            Directory.CreateDirectory(buildDir);
            var userPath = Path.Combine(tempDir, "my-custom-adapters");
            Directory.CreateDirectory(userPath);

            var settingsProvider = new TestableRunSettingsProvider();
            settingsProvider.AddDefaultRunSettings();
            settingsProvider.UpdateRunSettingsNode(TestAdapterPathArgumentExecutor.RunSettingsPath, userPath);

            CollectArgumentExecutor.TryAddCodeCoverageAdapterPath(settingsProvider, nugetPackagesOverride: tempDir);

            var xml = settingsProvider.ActiveRunSettings?.SettingsXml ?? string.Empty;
            Assert.IsFalse(xml.Contains(buildDir, StringComparison.OrdinalIgnoreCase),
                "Expected auto-injection to be skipped when the user has already configured adapter paths.");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static int CountOccurrences(string text, string pattern)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            count++;
            index += pattern.Length;
        }

        return count;
    }

    #endregion
}
