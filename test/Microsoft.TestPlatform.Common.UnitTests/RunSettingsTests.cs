// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.Common.UnitTests;

using System;
using System.Xml;

using Microsoft.TestPlatform.TestUtilities;
using Microsoft.VisualStudio.TestPlatform.Common;
using Microsoft.VisualStudio.TestPlatform.Common.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class RunSettingsTests
{
    [TestCleanup]
    public void TestCleanup()
    {
        TestPluginCacheHelper.ResetExtensionsCache();
        TestSessionMessageLogger.Instance = null;
    }

    #region LoadSettingsXML Tests

    [TestMethod]
    public void LoadSettingsXmlShouldThrowOnNullSettings()
    {
        var runSettings = new RunSettings();
        Assert.ThrowsException<ArgumentException>(() => runSettings.LoadSettingsXml(null));
    }

    [TestMethod]
    public void LoadSettingsXmlShouldThrowOnEmptySettings()
    {
        var runSettings = new RunSettings();
        Assert.ThrowsException<ArgumentException>(() => runSettings.LoadSettingsXml("  "));
    }

    [TestMethod]
    public void LoadSettingsXmlShoulLoadAndInitializeSettingsXml()
    {
        var runSettings = new RunSettings();
        var emptyRunSettings = RunSettingsTests.GetEmptyRunSettings();

        runSettings.LoadSettingsXml(emptyRunSettings);

        // Not doing this because when we load the xml and write to string it converts it to a utf-16 format.
        // So they do not exactly match.
        // Assert.AreEqual(emptyRunSettings, runSettings.SettingsXml);

        var expectedRunSettings = "<RunSettings>" + Environment.NewLine
                                                  + "</RunSettings>";
        StringAssert.Contains(runSettings.SettingsXml, expectedRunSettings);
    }

    [TestMethod]
    public void LoadSettingsXmlShouldThrowOnInvalidSettings()
    {
        var runSettings = new RunSettings();
        var invalidSettings = RunSettingsTests.GetInvalidRunSettings();

        Assert.ThrowsException<SettingsException>(
            () => runSettings.LoadSettingsXml(invalidSettings),
            "An error occurred while loading the run settings.");
    }

    #endregion

    #region InitializeSettingsProviders and GetSettings tests

    [TestMethod]
    public void InitializeSettingsProvidersShouldThrowOnNullSettings()
    {
        var runSettings = new RunSettings();
        Assert.ThrowsException<ArgumentNullException>(() => runSettings.InitializeSettingsProviders(null));
    }

    [TestMethod]
    public void InitializeSettingsProvidersShouldWorkForEmptyRunSettings()
    {
        var runSettings = new RunSettings();

        runSettings.InitializeSettingsProviders(RunSettingsTests.GetEmptyRunSettings());

        Assert.IsNull(runSettings.GetSettings("RunSettings"));
    }

    [TestMethod]
    public void InitializeSettingsProvidersShouldThrowIfNodeInRunSettingsDoesNotHaveAProvider()
    {
        TestPluginCacheHelper.SetupMockExtensions(typeof(RunSettingsTests));

        var runSettings = new RunSettings();
        runSettings.InitializeSettingsProviders(RunSettingsTests.GetRunSettingsWithUndefinedSettingsNodes());

        Action action =
            () => runSettings.GetSettings("OrphanNode");

        Assert.ThrowsException<SettingsException>(
            action,
            "Settings Provider named '{0}' was not found.  The settings can not be loaded.",
            "OrphanNode");
    }

    [TestMethod]
    public void InitializeSettingsProvidersShouldThrowIfSettingsProviderLoadThrows()
    {
        TestPluginCacheHelper.SetupMockExtensions(typeof(RunSettingsTests));

        var runSettings = new RunSettings();
        runSettings.InitializeSettingsProviders(RunSettingsTests.GetRunSettingsWithBadSettingsNodes());

        Action action =
            () => runSettings.GetSettings("BadSettings");

        Assert.ThrowsException<SettingsException>(
            action,
            "An error occurred while initializing the settings provider named '{0}'",
            "BadSettings");
    }

    [TestMethod]
    public void InitializeSettingsProvidersShouldThrowIfInvalidRunSettingsIsPassed()
    {
        var runSettings = new RunSettings();
        Assert.ThrowsException<SettingsException>(
            () => runSettings.InitializeSettingsProviders(RunSettingsTests.GetInvalidRunSettings()),
            "An error occurred while loading the run settings.");
    }

    [TestMethod]
    public void InitializeSettingsProvidersMultipleTimesShouldThrowInvalidOperationException()
    {
        var runSettings = new RunSettings();
        runSettings.InitializeSettingsProviders(RunSettingsTests.GetEmptyRunSettings());
        Assert.ThrowsException<InvalidOperationException>(
            () => runSettings.InitializeSettingsProviders(RunSettingsTests.GetEmptyRunSettings()),
            "The Run Settings have already been loaded.");
    }

    [TestMethod]
    public void InitializeSettingsProvidersShouldLoadSettingsIntoASettingsProvider()
    {
        TestPluginCacheHelper.SetupMockExtensions(typeof(RunSettingsTests));

        var runSettings = new RunSettings();
        runSettings.InitializeSettingsProviders(RunSettingsTests.GetRunSettingsWithRunConfigurationNode());

        var settingsProvider = runSettings.GetSettings("RunConfiguration");

        Assert.IsNotNull(settingsProvider);
        Assert.IsTrue(settingsProvider is RunConfigurationSettingsProvider);

        // Also validate that the settings provider gets the right subtree.
        Assert.AreEqual(
            "<RunConfiguration><Architecture>x86</Architecture></RunConfiguration>",
            (settingsProvider as RunConfigurationSettingsProvider).SettingsTree);
    }

    [TestMethod]
    public void InitializeSettingsProvidersShouldLoadSettingsIntoMultipleSettingsProviders()
    {
        TestPluginCacheHelper.SetupMockExtensions(typeof(RunSettingsTests));

        var runSettings = new RunSettings();
        runSettings.InitializeSettingsProviders(RunSettingsTests.GetRunSettingsWithRunConfigurationAndMsTestNode());

        var rcSettingsProvider = runSettings.GetSettings("RunConfiguration");
        var mstestSettingsProvider = runSettings.GetSettings("MSTest");

        Assert.IsNotNull(rcSettingsProvider);
        Assert.IsTrue(rcSettingsProvider is RunConfigurationSettingsProvider);
        Assert.AreEqual(
            "<RunConfiguration><Architecture>x86</Architecture></RunConfiguration>",
            (rcSettingsProvider as RunConfigurationSettingsProvider).SettingsTree);

        Assert.IsNotNull(mstestSettingsProvider);
        Assert.IsTrue(mstestSettingsProvider is MsTestSettingsProvider);
        Assert.AreEqual(
            "<MSTest><NoAppDomain>true</NoAppDomain></MSTest>",
            (mstestSettingsProvider as MsTestSettingsProvider).SettingsTree);
    }

    [TestMethod]
    public void InitializeSettingsProvidersShouldWarnOfDuplicateSettings()
    {
        string receivedWarningMessage = null;

        TestPluginCacheHelper.SetupMockExtensions(typeof(RunSettingsTests));
        TestSessionMessageLogger.Instance.TestRunMessage += (object sender, TestRunMessageEventArgs e) => receivedWarningMessage = e.Message;

        var runSettings = new RunSettings();
        runSettings.InitializeSettingsProviders(RunSettingsTests.GetRunSettingsWithDuplicateSettingsNodes());

        Assert.IsNotNull(receivedWarningMessage);
        Assert.AreEqual(
            "Duplicate run settings section named 'RunConfiguration' found.  Ignoring the duplicate settings.",
            receivedWarningMessage);
    }

    #endregion

    #region GetSettings tests

    [TestMethod]
    public void GetSettingsShouldThrowIfSettingsNameIsNull()
    {
        var runSettings = new RunSettings();

        Assert.ThrowsException<ArgumentException>(() => runSettings.GetSettings(null));
    }

    [TestMethod]
    public void GetSettingsShouldThrowIfSettingsNameIsEmpty()
    {
        var runSettings = new RunSettings();

        Assert.ThrowsException<ArgumentException>(() => runSettings.GetSettings("  "));
    }

    // The remaining GetSettings tests are covered in the InitializeSettingsProviders tests above.
    #endregion

    #region Private methods

    private static string GetEmptyRunSettings()
    {
        return @"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
</RunSettings>";
    }

    private static string GetRunSettingsWithUndefinedSettingsNodes()
    {
        return @"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
<OrphanNode>
<o> </o>
</OrphanNode>
</RunSettings>";
    }

    private static string GetRunSettingsWithBadSettingsNodes()
    {
        return @"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
<BadSettings>
<o> </o>
</BadSettings>
</RunSettings>";
    }

    private static string GetRunSettingsWithRunConfigurationNode()
    {
        return @"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
<RunConfiguration>
<Architecture>x86</Architecture>
</RunConfiguration>
</RunSettings>";
    }

    private static string GetRunSettingsWithRunConfigurationAndMsTestNode()
    {
        return @"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
<RunConfiguration>
<Architecture>x86</Architecture>
</RunConfiguration>
<MSTest>
<NoAppDomain>true</NoAppDomain>
</MSTest>
</RunSettings>";
    }

    private static string GetRunSettingsWithDuplicateSettingsNodes()
    {
        return @"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
<RunConfiguration>
</RunConfiguration>
<RunConfiguration>
</RunConfiguration>
</RunSettings>";
    }

    private static string GetInvalidRunSettings()
    {
        return @"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
</RunSettingsInvalid>";
    }

    #endregion

    #region Testable Implementations

    [SettingsName("RunConfiguration")]
    private class RunConfigurationSettingsProvider : ISettingsProvider
    {
        public string SettingsTree { get; set; }

        public void Load(XmlReader reader)
        {
            reader.Read();
            SettingsTree = reader.ReadOuterXml();
        }
    }

    [SettingsName("MSTest")]
    private class MsTestSettingsProvider : ISettingsProvider
    {
        public string SettingsTree { get; set; }

        public void Load(XmlReader reader)
        {
            reader.Read();
            SettingsTree = reader.ReadOuterXml();
        }
    }

    [SettingsName("BadSettings")]
    private class BadSettingsProvider : ISettingsProvider
    {
        public void Load(XmlReader reader)
        {
            throw new Exception();
        }
    }

    #endregion
}