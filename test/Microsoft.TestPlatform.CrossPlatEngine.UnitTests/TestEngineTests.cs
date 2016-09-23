// Copyright (c) Microsoft. All rights reserved.

namespace TestPlatform.CrossPlatEngine.UnitTests
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Hosting;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class TestEngineTests
    {
        private readonly ITestEngine testEngine;

        private Mock<ITestHostManager> mockTestHostManager;

        public TestEngineTests()
        {
            this.testEngine = new TestEngine();
            this.mockTestHostManager = new Mock<ITestHostManager>();
        }

        [TestMethod]
        public void GetDiscoveryManagerShouldReturnANonNullInstance()
        {
            Assert.IsNotNull(this.testEngine.GetDiscoveryManager(this.mockTestHostManager.Object));
        }

        [TestMethod]
        public void GetDiscoveryManagerShouldReturnCachedInstance()
        {
            var discoveryManager = this.testEngine.GetDiscoveryManager(this.mockTestHostManager.Object);

            Assert.AreSame(discoveryManager, this.testEngine.GetDiscoveryManager(this.mockTestHostManager.Object));
        }

        [TestMethod]
        public void GetExecutionManagerShouldReturnANonNullInstance()
        {
            var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll" }, 100);

            Assert.IsNotNull(this.testEngine.GetExecutionManager(this.mockTestHostManager.Object, testRunCriteria));
        }

        [TestMethod]
        public void GetExecutionManagerShouldReturnCachedInstance()
        {
            var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll" }, 100);
            var executionManager = this.testEngine.GetExecutionManager(this.mockTestHostManager.Object, testRunCriteria);

            Assert.AreSame(executionManager, this.testEngine.GetExecutionManager(this.mockTestHostManager.Object, testRunCriteria));
        }

        [TestMethod]
        public void GetExecutionManagerShouldReturnDefaultExecutionManagerIfParallelDisabled()
        {
            string settingXml = @"<RunSettings><RunConfiguration></RunConfiguration ></RunSettings>";
            var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll" }, 100, false, settingXml);

            Assert.IsNotNull(this.testEngine.GetExecutionManager(this.mockTestHostManager.Object, testRunCriteria));
            Assert.IsInstanceOfType(this.testEngine.GetExecutionManager(this.mockTestHostManager.Object, testRunCriteria), typeof(ProxyExecutionManager));
        }

        [TestMethod]
        public void GetExecutionManagerWithSingleSourceShouldReturnDefaultExecutionManagerEvenIfParallelEnabled()
        {
            string settingXml = @"<RunSettings><RunConfiguration><MaxCpuCount>2</MaxCpuCount></RunConfiguration ></RunSettings>";
            var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll" }, 100, false, settingXml);

            Assert.IsNotNull(this.testEngine.GetExecutionManager(this.mockTestHostManager.Object, testRunCriteria));
            Assert.IsInstanceOfType(this.testEngine.GetExecutionManager(this.mockTestHostManager.Object, testRunCriteria), typeof(ProxyExecutionManager));
        }

        [TestMethod]
        public void GetExecutionManagerShouldReturnParallelExecutionManagerIfParallelEnabled()
        {
            string settingXml = @"<RunSettings><RunConfiguration><MaxCpuCount>2</MaxCpuCount></RunConfiguration></RunSettings>";
            var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll", "2.dll" }, 100, false, settingXml);

            Assert.IsNotNull(this.testEngine.GetExecutionManager(this.mockTestHostManager.Object, testRunCriteria));
            Assert.IsInstanceOfType(this.testEngine.GetExecutionManager(this.mockTestHostManager.Object, testRunCriteria), typeof(ParallelProxyExecutionManager));
        }

        [TestMethod]
        public void GetExcecutionManagerShouldReturnExectuionManagerWithDataCollectionIfDataCollectionIsEnabled()
        {
            var settingXml = @"<RunSettings><DataCollectionRunSettings><DataCollectors><DataCollector friendlyName=""Code Coverage"" uri=""datacollector://Microsoft/CodeCoverage/2.0"" assemblyQualifiedName=""Microsoft.VisualStudio.Coverage.DynamicCoverageDataCollector, Microsoft.VisualStudio.TraceCollector, Version=11.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a""></DataCollector></DataCollectors></DataCollectionRunSettings></RunSettings>";
            var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll" }, 100, false, settingXml);
            var result = this.testEngine.GetExecutionManager(this.mockTestHostManager.Object, testRunCriteria);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(ProxyExecutionManagerWithDataCollection));
        }

        [TestMethod]
        public void GetExtensionManagerShouldReturnANonNullInstance()
        {
            Assert.IsNotNull(this.testEngine.GetExtensionManager());
        }

        [TestMethod]
        public void GetDefaultTestHostManagerReturnsANonNullInstance()
        {
            Assert.IsNotNull(this.testEngine.GetDefaultTestHostManager(Architecture.X86, Framework.DefaultFramework));
        }

        [TestMethod]
        public void GetDefaultTestHostManagerReturnsANewInstanceEverytime()
        {
            var instance1 = this.testEngine.GetDefaultTestHostManager(Architecture.X86, Framework.DefaultFramework);
            var instance2 = this.testEngine.GetDefaultTestHostManager(Architecture.X86, Framework.DefaultFramework);

            Assert.AreNotEqual(instance1, instance2);
        }

        [TestMethod]
        public void GetDefaultTestHostManagerReturnsDotnetCoreHostManagerIfFrameworkIsNetCore()
        {
            var testHostManager = this.testEngine.GetDefaultTestHostManager(
                Architecture.X64,
                Framework.FromString(".NETCoreApp,Version=v1.0"));

            Assert.AreEqual(typeof(DotnetTestHostManager), testHostManager.GetType());
        }
    }
}
