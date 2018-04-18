// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.Common.UnitTests.ExtensionFramework.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class LazyExtensionTests
    {
        #region Value tests

        [TestMethod]
        public void ValueShouldCreateExtensionViaTheCallback()
        {
            var mockExtension = new Mock<ITestDiscoverer>();
            LazyExtension<ITestDiscoverer, ITestDiscovererCapabilities> extension =
                new LazyExtension<ITestDiscoverer, ITestDiscovererCapabilities>(
                    () => { return mockExtension.Object; },
                    new Mock<ITestDiscovererCapabilities>().Object);

            Assert.AreEqual(mockExtension.Object, extension.Value);
        }

        [TestMethod]
        public void ValueShouldCreateExtensionViaTestPluginManager()
        {
            var testDiscovererPluginInfo = new TestDiscovererPluginInformation(typeof(DummyExtension));
            LazyExtension<ITestDiscoverer, ITestDiscovererCapabilities> extension =
                new LazyExtension<ITestDiscoverer, ITestDiscovererCapabilities>(
                    testDiscovererPluginInfo,
                    new Mock<ITestDiscovererCapabilities>().Object);

            Assert.IsNotNull(extension.Value);
            Assert.AreEqual(typeof(DummyExtension), extension.Value.GetType());
        }

        [TestMethod]
        public void ValueShouldNotCreateExtensionIfAlreadyCreated()
        {
            var numberOfTimesExtensionCreated = 0;
            var mockExtension = new Mock<ITestDiscoverer>();
            LazyExtension<ITestDiscoverer, ITestDiscovererCapabilities> extension =
                new LazyExtension<ITestDiscoverer, ITestDiscovererCapabilities>(
                    () =>
                        {
                            numberOfTimesExtensionCreated++;
                            return mockExtension.Object;
                        },
                    new Mock<ITestDiscovererCapabilities>().Object);

            var temp = extension.Value;
            temp = extension.Value;

            Assert.AreEqual(1, numberOfTimesExtensionCreated);
        }

        #endregion

        #region metadata tests

        [TestMethod]
        public void MetadataShouldReturnMetadataSpecified()
        {
            var testDiscovererPluginInfo = new TestDiscovererPluginInformation(typeof(DummyExtension));
            var mockMetadata = new Mock<ITestDiscovererCapabilities>();
            LazyExtension<ITestDiscoverer, ITestDiscovererCapabilities> extension =
                new LazyExtension<ITestDiscoverer, ITestDiscovererCapabilities>(
                    testDiscovererPluginInfo,
                    mockMetadata.Object);
            
            Assert.AreEqual(mockMetadata.Object, extension.Metadata);
        }

        [TestMethod]
        public void MetadataShouldCreateMetadataFromMetadataType()
        {
            var testDiscovererPluginInfo = new TestDiscovererPluginInformation(typeof(DummyExtension));
            LazyExtension<ITestDiscoverer, ITestDiscovererCapabilities> extension =
                new LazyExtension<ITestDiscoverer, ITestDiscovererCapabilities>(
                    testDiscovererPluginInfo,
                    typeof(DummyDiscovererCapability));

            var metadata = extension.Metadata;
            Assert.IsNotNull(metadata);
            Assert.AreEqual(typeof(DummyDiscovererCapability), metadata.GetType());
            CollectionAssert.AreEqual(new List<string> { "csv" }, (metadata as ITestDiscovererCapabilities).FileExtension.ToArray());
            Assert.AreEqual("executor://unittestexecutor/", (metadata as ITestDiscovererCapabilities).DefaultExecutorUri.AbsoluteUri);
            Assert.AreEqual(AssemblyType.Native, (metadata as ITestDiscovererCapabilities).AssemblyType);
        }

        #endregion

        #region Implementation

        private class DummyDiscovererCapability : ITestDiscovererCapabilities
        {
            public IEnumerable<string> FileExtension
            {
                get;
                private set;
            }

            public Uri DefaultExecutorUri
            {
                get;
                private set;
            }

            public AssemblyType AssemblyType
            {
                get;
                private set;
            }

            public DummyDiscovererCapability(List<string> fileExtensions, string executorURI, AssemblyType assemblyType)
            {
                this.FileExtension = fileExtensions;
                this.DefaultExecutorUri = new Uri(executorURI);
                this.AssemblyType = assemblyType;
            }
        }


        [FileExtension("csv")]
        [DefaultExecutorUri("executor://unittestexecutor")]
        [Category("native")]
        private class DummyExtension : ITestDiscoverer
        {
            public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
            {
            }
        }

        #endregion
    }
}
