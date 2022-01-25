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
            testPluginInformation = new TestLoggerPluginInformation(typeof(DummyTestExtensionWithNoFriendlyName));
            Assert.AreEqual(typeof(DummyTestExtensionWithNoFriendlyName).AssemblyQualifiedName, testPluginInformation.AssemblyQualifiedName);
        }

        [TestMethod]
        public void IdentifierDataShouldReturnExtensionUri()
        {
            testPluginInformation = new TestLoggerPluginInformation(typeof(DummyTestExtensionWithFriendlyName));
            Assert.AreEqual(DefaultExtensionURI, testPluginInformation.IdentifierData);
        }

        [TestMethod]
        public void FriendlyNameShouldReturnEmptyIfALoggerDoesNotHaveOne()
        {
            testPluginInformation = new TestLoggerPluginInformation(typeof(DummyTestExtensionWithNoFriendlyName));
            Assert.IsNotNull(testPluginInformation.FriendlyName);
            Assert.AreEqual(string.Empty, testPluginInformation.FriendlyName);
        }

        [TestMethod]
        public void FriendlyNameShouldReturnFriendlyNameOfALogger()
        {
            testPluginInformation = new TestLoggerPluginInformation(typeof(DummyTestExtensionWithFriendlyName));
            Assert.AreEqual(DefaultFriendlyName, testPluginInformation.FriendlyName);
        }

        [TestMethod]
        public void MetadataShouldReturnExtensionUriAndFriendlyName()
        {
            testPluginInformation = new TestLoggerPluginInformation(typeof(DummyTestExtensionWithFriendlyName));

            CollectionAssert.AreEqual(new object[] { DefaultExtensionURI, DefaultFriendlyName }, testPluginInformation.Metadata.ToArray());
        }

        #region Implementation

        private class DummyTestExtensionWithNoFriendlyName
        {
        }

        [FriendlyName(DefaultFriendlyName)]
        [ExtensionUri(DefaultExtensionURI)]
        private class DummyTestExtensionWithFriendlyName
        {
        }

        #endregion
    }
}
