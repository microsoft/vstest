// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.Common.UnitTests.Utilities
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class FakesUtilitiesTests
    {
        
        [TestMethod]
        public void FakesSettingsShouldBeNotGeneratedIfTargetFrameWorkIsNetCore()
        {
            string runSettingsXml = @"<RunSettings><RunConfiguration><TargetFrameworkVersion>.netstandard,Version=5.0</TargetFrameworkVersion></RunConfiguration ></RunSettings>";
            var generatedRunSettings = FakesUtilities.GenerateFakesSettingsForRunConfiguration(new string[] { }, runSettingsXml);
            Assert.AreEqual(generatedRunSettings, runSettingsXml);
        }

        [TestMethod]
        public void FakesSettingsShouldThrowExceptionIfSourcesArePassedAsNull()
        {
            string runSettingsXml = @"<RunSettings><RunConfiguration><TargetFrameworkVersion>.netstandard,Version=5.0</TargetFrameworkVersion></RunConfiguration ></RunSettings>";
            Assert.ThrowsException<ArgumentNullException>(() => FakesUtilities.GenerateFakesSettingsForRunConfiguration(null, runSettingsXml));
        }

        [TestMethod]
        public void FakesSettingsShouldThrowExceptionIfRunSettingsIsPassedAsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() => FakesUtilities.GenerateFakesSettingsForRunConfiguration(new string[] { }, null));
        }

        [TestMethod]
        public void FakesSettingsShouldBeNotGeneratedIfFakeConfiguratorAssemblyIsNotPresent()
        {
            string runSettingsXml = @"<RunSettings><RunConfiguration></RunConfiguration ></RunSettings>";
            var generatedRunSettings = FakesUtilities.GenerateFakesSettingsForRunConfiguration(new string[] {@"C:\temp\UT.dll" }, runSettingsXml);
            Assert.AreEqual(generatedRunSettings, runSettingsXml);
        }
    }
}
