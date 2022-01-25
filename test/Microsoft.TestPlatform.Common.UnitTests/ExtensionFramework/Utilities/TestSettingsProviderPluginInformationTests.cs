// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.Common.UnitTests.ExtensionFramework.Utilities
{
    using System;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TestSettingsProviderPluginInformationTests
    {
        private TestSettingsProviderPluginInformation testPluginInformation;

        private const string DefaultSettingsName = "mstestsettings";

        [TestMethod]
        public void AssemblyQualifiedNameShouldReturnTestExtensionTypesName()
        {
            testPluginInformation = new TestSettingsProviderPluginInformation(typeof(TestPluginInformationTests));
            Assert.AreEqual(typeof(TestPluginInformationTests).AssemblyQualifiedName, testPluginInformation.AssemblyQualifiedName);
        }

        [TestMethod]
        public void IdentifierDataShouldReturnSettingsName()
        {
            testPluginInformation = new TestSettingsProviderPluginInformation(typeof(DummySettingProviderWithSettingsName));
            Assert.AreEqual(DefaultSettingsName, testPluginInformation.IdentifierData);
        }

        [TestMethod]
        public void MetadataShouldReturnSettingsProviderName()
        {
            testPluginInformation = new TestSettingsProviderPluginInformation(typeof(DummySettingProviderWithSettingsName));
            CollectionAssert.AreEqual(new object[] { DefaultSettingsName }, testPluginInformation.Metadata.ToArray());
        }

        [TestMethod]
        public void SettingsNameShouldReturnEmptyIfASettingsProviderDoesNotHaveOne()
        {
            testPluginInformation = new TestSettingsProviderPluginInformation(typeof(DummySettingProviderWithoutSettingsName));
            Assert.IsNotNull(testPluginInformation.SettingsName);
            Assert.AreEqual(string.Empty, testPluginInformation.SettingsName);
        }

        [TestMethod]
        public void SettingsNameShouldReturnExtensionUriOfAnExtension()
        {
            testPluginInformation = new TestSettingsProviderPluginInformation(typeof(DummySettingProviderWithSettingsName));
            Assert.AreEqual(DefaultSettingsName, testPluginInformation.SettingsName);
        }

        #region Implementation

        private class DummySettingProviderWithoutSettingsName
        {
        }

        [SettingsName(DefaultSettingsName)]
        private class DummySettingProviderWithSettingsName
        {
        }

        #endregion
    }
}
