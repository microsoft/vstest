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
    public class TestLoggerPluginInformationTests
    {
        private TestLoggerPluginInformation testPluginInformation;

        internal const string DefaultExtensionURI = "executor://unittest";

        internal const string DefaultFriendlyName = "excel";

        [TestMethod]
        public void AssemblyQualifiedNameShouldReturnTestExtensionTypesName()
        {
            this.testPluginInformation = new TestLoggerPluginInformation(typeof(DummyTestExtensionWithNoFriendlyName));
            Assert.AreEqual(typeof(DummyTestExtensionWithNoFriendlyName).AssemblyQualifiedName, this.testPluginInformation.AssemblyQualifiedName);
        }

        [TestMethod]
        public void IdentifierDataShouldReturnExtensionUri()
        {
            this.testPluginInformation = new TestLoggerPluginInformation(typeof(DummyTestExtensionWithFriendlyName));
            Assert.AreEqual(DefaultExtensionURI, this.testPluginInformation.IdentifierData);
        }

        [TestMethod]
        public void FriendlyNameShouldReturnEmptyIfALoggerDoesNotHaveOne()
        {
            this.testPluginInformation = new TestLoggerPluginInformation(typeof(DummyTestExtensionWithNoFriendlyName));
            Assert.IsNotNull(this.testPluginInformation.FriendlyName);
            Assert.AreEqual(string.Empty, this.testPluginInformation.FriendlyName);
        }

        [TestMethod]
        public void FriendlyNameShouldReturnFriendlyNameOfALogger()
        {
            this.testPluginInformation = new TestLoggerPluginInformation(typeof(DummyTestExtensionWithFriendlyName));
            Assert.AreEqual(DefaultFriendlyName, this.testPluginInformation.FriendlyName);
        }

        [TestMethod]
        public void MetadataShouldReturnExtensionUriAndFriendlyName()
        {
            this.testPluginInformation = new TestLoggerPluginInformation(typeof(DummyTestExtensionWithFriendlyName));

            CollectionAssert.AreEqual(new object[] { DefaultExtensionURI, DefaultFriendlyName }, this.testPluginInformation.Metadata.ToArray());
        }

        #region Implementation

        private class DummyTestExtensionWithNoFriendlyName
        {
        }

        [FriendlyName(TestLoggerPluginInformationTests.DefaultFriendlyName)]
        [ExtensionUri(TestLoggerPluginInformationTests.DefaultExtensionURI)]
        private class DummyTestExtensionWithFriendlyName
        {
        }

        #endregion
    }
}
