// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.DataCollection
{
    using System.IO;
    using System.Reflection;
    using Coverlet.Collector.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollector.InProcDataCollector;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.InProcDataCollector;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;
    using TestPlatform.Common.UnitTests.ExtensionFramework;

    [TestClass]
    public class InProcDataCollectorTests
    {
        private Mock<IAssemblyLoadContext> assemblyLoadContext;

        private IInProcDataCollector inProcDataCollector;

        public InProcDataCollectorTests()
        {
            this.assemblyLoadContext = new Mock<IAssemblyLoadContext>();
        }

        [TestMethod]
        public void InProcDataCollectorShouldNotThrowExceptionIfInvalidAssemblyIsProvided()
        {
            this.assemblyLoadContext.Setup(alc => alc.LoadAssemblyFromPath(It.IsAny<string>()))
                .Throws<FileNotFoundException>();

            this.inProcDataCollector = new InProcDataCollector(
                string.Empty,
                string.Empty,
                null,
                string.Empty,
                this.assemblyLoadContext.Object,
                TestPluginCache.Instance);

            Assert.IsNull(this.inProcDataCollector.AssemblyQualifiedName);
        }

        [TestMethod]
        public void InProcDataCollectorShouldNotThrowExceptionIfAssemblyDoesNotContainAnyInProcDataCollector()
        {
            this.assemblyLoadContext.Setup(alc => alc.LoadAssemblyFromPath(It.IsAny<string>()))
                .Returns(Assembly.GetEntryAssembly());

            this.inProcDataCollector = new InProcDataCollector(
                string.Empty,
                string.Empty,
                null,
                string.Empty,
                this.assemblyLoadContext.Object,
                TestPluginCache.Instance);

            Assert.IsNull(this.inProcDataCollector.AssemblyQualifiedName);
        }

        [TestMethod]
        public void InProcDataCollectorShouldInitializeIfAssemblyContainsAnyInProcDataCollector()
        {
            var typeInfo = typeof(TestableInProcDataCollector).GetTypeInfo();

            this.assemblyLoadContext.Setup(alc => alc.LoadAssemblyFromPath(It.IsAny<string>()))
                .Returns(typeInfo.Assembly);

            this.inProcDataCollector = new InProcDataCollector(
                string.Empty,
                typeInfo.AssemblyQualifiedName,
                typeInfo,
                string.Empty,
                this.assemblyLoadContext.Object,
                TestPluginCache.Instance);

            Assert.IsNotNull(this.inProcDataCollector.AssemblyQualifiedName);
            Assert.AreEqual(this.inProcDataCollector.AssemblyQualifiedName, typeInfo.AssemblyQualifiedName);
        }

        [TestMethod]
        public void InProcDataCollectorLoadCoverlet()
        {
            var typeInfo = typeof(CoverletInProcDataCollector).GetTypeInfo();

            Assert.AreEqual("9.9.9.9", typeInfo.Assembly.GetName().Version.ToString());

            this.assemblyLoadContext.Setup(alc => alc.LoadAssemblyFromPath(It.IsAny<string>()))
                .Returns(typeInfo.Assembly);

            // We need to mock TestPluginCache because we have to create assembly resolver instance
            // using SetupAssemblyResolver method, we don't use any other method of class(like DiscoverTestExtensions etc...)
            // that fire creation
            TestableTestPluginCache testablePlugin = new TestableTestPluginCache();
            testablePlugin.SetupAssemblyResolver(typeInfo.Assembly.Location);

            this.inProcDataCollector = new InProcDataCollector(
                typeInfo.Assembly.Location,
                "Coverlet.Collector.DataCollection.CoverletInProcDataCollector, coverlet.collector, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                typeof(InProcDataCollection).GetTypeInfo(),
                string.Empty,
                this.assemblyLoadContext.Object,
                testablePlugin);

            Assert.IsNotNull(this.inProcDataCollector.AssemblyQualifiedName);
            Assert.AreEqual(this.inProcDataCollector.AssemblyQualifiedName, typeInfo.AssemblyQualifiedName);
        }

        private class TestableInProcDataCollector : InProcDataCollection
        {
            public void Initialize(IDataCollectionSink dataCollectionSink)
            {
                throw new System.NotImplementedException();
            }

            public void TestSessionStart(TestSessionStartArgs testSessionStartArgs)
            {
                throw new System.NotImplementedException();
            }

            public void TestCaseStart(TestCaseStartArgs testCaseStartArgs)
            {
                throw new System.NotImplementedException();
            }

            public void TestCaseEnd(TestCaseEndArgs testCaseEndArgs)
            {
                throw new System.NotImplementedException();
            }

            public void TestSessionEnd(TestSessionEndArgs testSessionEndArgs)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
