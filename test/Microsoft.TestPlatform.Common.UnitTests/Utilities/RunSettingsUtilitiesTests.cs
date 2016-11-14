// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.Common.UnitTests.Utilities
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
    using System.Xml;
    using ExtensionFramework;
    [TestClass]
    public class RunSettingsUtilitiesTests
    {
        [TestMethod]
        public void CreateRunSettingsShouldReturnNullIfSettingsXmlIsNullorEmpty()
        {
            Assert.IsNull(RunSettingsUtilities.CreateAndInitializeRunSettings(null));
        }

        [TestMethod]
        public void CreateRunSettingsShouldThrowExceptionWhenInvalidXmlStringIsPassed()
        {
            Assert.ThrowsException<SettingsException>(() =>
            {
                RunSettingsUtilities.CreateAndInitializeRunSettings("abc");
            }
            );
        }

        [TestMethod]
        public void CreateRunSettingsShouldReturnValidRunSettings()
        {
            TestPluginCacheTests.SetupMockExtensions();
            string runsettings = @"<RunSettings><RunConfiguration><ResultsDirectory>.\TestResults</ResultsDirectory></RunConfiguration ><DummyMSTest><FORCEDLEGACYMODE>true</FORCEDLEGACYMODE></DummyMSTest></RunSettings>";
            var result= RunSettingsUtilities.CreateAndInitializeRunSettings(runsettings);
            Assert.AreEqual(DummyMsTestSetingsProvider.StringToVerify, "<DummyMSTest><FORCEDLEGACYMODE>true</FORCEDLEGACYMODE></DummyMSTest>");
            TestPluginCacheTests.ResetExtensionsCache();
        }

        [TestMethod]
        public void GetMaxCpuCountWithNullSettingXmlShouldReturnDefaultCpuCount()
        {
            string settingXml = null;
            int expectedResult = Constants.DefaultCpuCount;

            int result = RunSettingsUtilities.GetMaxCpuCount(settingXml);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void GetMaxCpuCountWithEmptySettingXmlShouldReturnDefaultCpuCount()
        {
            string settingXml = "";
            int expectedResult = Constants.DefaultCpuCount;

            int result = RunSettingsUtilities.GetMaxCpuCount(settingXml);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void GetMaxCpuCountWithSettingXmlNotHavingCpuCountShouldReturnDefaultCpuCount()
        {
            string settingXml = @"<RunSettings><RunConfiguration></RunConfiguration ></RunSettings>";
            int expectedResult = Constants.DefaultCpuCount;

            int result = RunSettingsUtilities.GetMaxCpuCount(settingXml);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void GetMaxCpuCountWithSettingXmlCpuCountShouldReturnCorrectCpuCount()
        {
            string settingXml = @"<RunSettings><RunConfiguration><MaxCpuCount>5</MaxCpuCount></RunConfiguration ></RunSettings>";
            int expectedResult = 5;

            int result = RunSettingsUtilities.GetMaxCpuCount(settingXml);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void GetMaxCpuCountWithInvalidCpuCountShouldReturnDefaultCpuCount()
        {
            string settingXml = @"<RunSettings><RunConfiguration><MaxCpuCount>-10</MaxCpuCount></RunConfiguration ></RunSettings>";
            int expectedResult = Constants.DefaultCpuCount;

            int result = RunSettingsUtilities.GetMaxCpuCount(settingXml);

            Assert.AreEqual(expectedResult, result);
        }
    }

    [SettingsName("DummyMSTest")]
    public class DummyMsTestSetingsProvider : ISettingsProvider
    {
        public void Load(XmlReader reader)
        {
            ValidateArg.NotNull<XmlReader>(reader, "reader");
            reader.Read();
            StringToVerify = reader.ReadOuterXml();
        }

        public static string StringToVerify = string.Empty;
    }

}
