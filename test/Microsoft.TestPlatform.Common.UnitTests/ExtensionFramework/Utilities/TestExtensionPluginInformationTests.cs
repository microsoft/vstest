// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.Common.UnitTests.ExtensionFramework.Utilities
{
    using System;
    using System.Globalization;
    using System.Linq;
    using Microsoft.VisualStudio.TestPlatform.Common.Exceptions;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using CommonResources = Microsoft.VisualStudio.TestPlatform.Common.Resources.Resources;

    [TestClass]
    public class TestExtensionPluginInformationTests
    {
        private TestableTestExtensionPluginInformation testPluginInformation;

        internal const string DefaultExtensionURI = "executor://unittest";

        [TestMethod]
        public void AssemblyQualifiedNameShouldReturnTestExtensionTypesName()
        {
            this.testPluginInformation = new TestableTestExtensionPluginInformation(typeof(DummyTestExtensionWithNoExtensionUri));
            Assert.AreEqual(typeof(DummyTestExtensionWithNoExtensionUri).AssemblyQualifiedName, this.testPluginInformation.AssemblyQualifiedName);
        }

        [TestMethod]
        public void IdentifierDataShouldReturnExtensionUri()
        {
            this.testPluginInformation = new TestableTestExtensionPluginInformation(typeof(DummyTestExtensionWithExtensionUri));
            Assert.AreEqual(DefaultExtensionURI, this.testPluginInformation.IdentifierData);
        }

        [TestMethod]
        public void ExtensionUriShouldReturnEmptyIfAnExtensionDoesNotHaveOne()
        {
            this.testPluginInformation = new TestableTestExtensionPluginInformation(typeof(DummyTestExtensionWithNoExtensionUri));
            Assert.IsNotNull(this.testPluginInformation.ExtensionUri);
            Assert.AreEqual(string.Empty, this.testPluginInformation.ExtensionUri);
        }

        [TestMethod]
        public void ExtensionUriShouldReturnExtensionUriOfAnExtension()
        {
            this.testPluginInformation = new TestableTestExtensionPluginInformation(typeof(DummyTestExtensionWithExtensionUri));
            Assert.AreEqual(DefaultExtensionURI, this.testPluginInformation.ExtensionUri);
        }

        [TestMethod]
        public void MetadataShouldReturnExtensionUri()
        {
            this.testPluginInformation = new TestableTestExtensionPluginInformation(typeof(DummyTestExtensionWithExtensionUri));
            
            CollectionAssert.AreEqual(new object[] { DefaultExtensionURI }, this.testPluginInformation.Metadata.ToArray());
        }

        [TestMethod]
        public void TestLoggerPluginInformationShouldThrowExceptionWithNoExtensionUri()
        {
            Type type = typeof(DummyTestExtensionWithNoExtensionUri);
            var message = string.Format(CultureInfo.CurrentUICulture, CommonResources.UnknownUniqueIdentifier, type.Module);
            InvalidLoggerException ex = Assert.ThrowsException<InvalidLoggerException>(() => new TestableTestExtensionPluginInformation(typeof(DummyTestExtensionWithNoExtensionUri)));
            Assert.AreEqual(message, ex.Message);
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

        [ExtensionUri(TestExtensionPluginInformationTests.DefaultExtensionURI)]
        private class DummyTestExtensionWithExtensionUri
        {
        }

        #endregion
    }
}
