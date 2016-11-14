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
            this.testPluginInformation = new TestSettingsProviderPluginInformation(typeof(TestPluginInformationTests));
            Assert.AreEqual(typeof(TestPluginInformationTests).AssemblyQualifiedName, this.testPluginInformation.AssemblyQualifiedName);
        }

        [TestMethod]
        public void IdentifierDataShouldReturnSettingsName()
        {
            this.testPluginInformation = new TestSettingsProviderPluginInformation(typeof(DummySettingProviderWithSettingsName));
            Assert.AreEqual(DefaultSettingsName, this.testPluginInformation.IdentifierData);
        }

        [TestMethod]
        public void MetadataShouldReturnSettingsProviderName()
        {
            this.testPluginInformation = new TestSettingsProviderPluginInformation(typeof(DummySettingProviderWithSettingsName));
            CollectionAssert.AreEqual(new object[] { DefaultSettingsName }, this.testPluginInformation.Metadata.ToArray());
        }

        [TestMethod]
        public void SettingsNameShouldReturnEmptyIfASettingsProviderDoesNotHaveOne()
        {
            this.testPluginInformation = new TestSettingsProviderPluginInformation(typeof(DummySettingProviderWithoutSettingsName));
            Assert.IsNotNull(this.testPluginInformation.SettingsName);
            Assert.AreEqual(string.Empty, this.testPluginInformation.SettingsName);
        }

        [TestMethod]
        public void SettingsNameShouldReturnExtensionUriOfAnExtension()
        {
            this.testPluginInformation = new TestSettingsProviderPluginInformation(typeof(DummySettingProviderWithSettingsName));
            Assert.AreEqual(DefaultSettingsName, this.testPluginInformation.SettingsName);
        }

        #region implementation

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
