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
    public class TestExtensionPluginInformationTests
    {
        private TestableTestExtensionPluginInformation testPluginInformation;

        internal const string DefaultExtensionURI = "executor://unittest";

        [TestMethod]
        public void AssemblyQualifiedNameShouldReturnTestExtensionTypesName()
        {
            testPluginInformation = new TestableTestExtensionPluginInformation(typeof(DummyTestExtensionWithNoExtensionUri));
            Assert.AreEqual(typeof(DummyTestExtensionWithNoExtensionUri).AssemblyQualifiedName, testPluginInformation.AssemblyQualifiedName);
        }

        [TestMethod]
        public void IdentifierDataShouldReturnExtensionUri()
        {
            testPluginInformation = new TestableTestExtensionPluginInformation(typeof(DummyTestExtensionWithExtensionUri));
            Assert.AreEqual(DefaultExtensionURI, testPluginInformation.IdentifierData);
        }

        [TestMethod]
        public void ExtensionUriShouldReturnEmptyIfAnExtensionDoesNotHaveOne()
        {
            testPluginInformation = new TestableTestExtensionPluginInformation(typeof(DummyTestExtensionWithNoExtensionUri));
            Assert.IsNotNull(testPluginInformation.ExtensionUri);
            Assert.AreEqual(string.Empty, testPluginInformation.ExtensionUri);
        }

        [TestMethod]
        public void ExtensionUriShouldReturnExtensionUriOfAnExtension()
        {
            testPluginInformation = new TestableTestExtensionPluginInformation(typeof(DummyTestExtensionWithExtensionUri));
            Assert.AreEqual(DefaultExtensionURI, testPluginInformation.ExtensionUri);
        }

        [TestMethod]
        public void MetadataShouldReturnExtensionUri()
        {
            testPluginInformation = new TestableTestExtensionPluginInformation(typeof(DummyTestExtensionWithExtensionUri));

            CollectionAssert.AreEqual(new object[] { DefaultExtensionURI }, testPluginInformation.Metadata.ToArray());
        }

        #region Implementation

        private class TestableTestExtensionPluginInformation : TestExtensionPluginInformation
        {
            public TestableTestExtensionPluginInformation(Type testExtensionType) : base(testExtensionType)
            {
            }
        }

        private class DummyTestExtensionWithNoExtensionUri
        {
        }

        [ExtensionUri(DefaultExtensionURI)]
        private class DummyTestExtensionWithExtensionUri
        {
        }

        #endregion
    }
}
