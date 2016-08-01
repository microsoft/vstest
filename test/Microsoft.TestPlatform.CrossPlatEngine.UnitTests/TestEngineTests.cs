// Copyright (c) Microsoft. All rights reserved.

namespace TestPlatform.CrossPlatEngine.UnitTests
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TestEngineTests
    {
        private ITestEngine testEngine;

        public TestEngineTests()
        {
            this.testEngine = new TestEngine();
        }

        [TestMethod]
        public void GetDiscoveryManagerShouldReturnANonNullInstance()
        {
            Assert.IsNotNull(this.testEngine.GetDiscoveryManager());
        }


        [TestMethod]
        public void GetExecutionManagerShouldReturnANonNullInstance()
        {
            var testRunCriteria = new TestRunCriteria(new List<string>() { "1.dll" }, 100);
            Assert.IsNotNull(this.testEngine.GetExecutionManager(testRunCriteria));
        }

        [TestMethod]
        public void GetExecutionManagerShouldReturnDefaultExecutionManagerIfParallelDisabled()
        {
            string settingXml = @"<RunSettings><RunConfiguration></RunConfiguration ></RunSettings>";
            var testRunCriteria = new TestRunCriteria(new List<string>() { "1.dll" }, 100, false, settingXml);
            Assert.IsNotNull(this.testEngine.GetExecutionManager(testRunCriteria));
            Assert.IsInstanceOfType(this.testEngine.GetExecutionManager(testRunCriteria), typeof(ProxyExecutionManager));
        }

        [TestMethod]
        public void GetExecutionManagerWithSingleSourceShouldReturnDefaultExecutionManagerEvenIfParallelEnabled()
        {
            string settingXml = @"<RunSettings><RunConfiguration><MaxCpuCount>2</MaxCpuCount></RunConfiguration ></RunSettings>";
            var testRunCriteria = new TestRunCriteria(new List<string>() { "1.dll" }, 100, false, settingXml);
            Assert.IsNotNull(this.testEngine.GetExecutionManager(testRunCriteria));
            Assert.IsInstanceOfType(this.testEngine.GetExecutionManager(testRunCriteria), typeof(ProxyExecutionManager));
        }

        [TestMethod]
        public void GetExecutionManagerShouldReturnParallelExecutionManagerIfParallelEnabled()
        {
            string settingXml = @"<RunSettings><RunConfiguration><MaxCpuCount>2</MaxCpuCount></RunConfiguration></RunSettings>";
            var testRunCriteria = new TestRunCriteria(new List<string>() { "1.dll", "2.dll" }, 100, false, settingXml);
            Assert.IsNotNull(this.testEngine.GetExecutionManager(testRunCriteria));
            Assert.IsInstanceOfType(this.testEngine.GetExecutionManager(testRunCriteria), typeof(ParallelProxyExecutionManager));
        }

        [TestMethod]
        public void GetExcecutionManagerShouldReturnExectuionManagerWithDataCollectionIfDataCollectionIsEnabled()
        {
            var settingXml = @"<RunSettings><DataCollectionRunSettings><DataCollectors><DataCollector friendlyName=""Code Coverage"" uri=""datacollector://Microsoft/CodeCoverage/2.0"" assemblyQualifiedName=""Microsoft.VisualStudio.Coverage.DynamicCoverageDataCollector, Microsoft.VisualStudio.TraceCollector, Version=11.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a""></DataCollector></DataCollectors></DataCollectionRunSettings></RunSettings>";
            var testRunCriteria = new TestRunCriteria(new List<string>() { "1.dll" }, 100, false, settingXml);
            var result = this.testEngine.GetExecutionManager(testRunCriteria);
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
            Assert.IsNotNull(this.testEngine.GetDefaultTestHostManager(Architecture.X86));
        }

        [TestMethod]
        public void GetDefaultTestHostManagerReturnsANewInstanceEverytime()
        {
            var instance1 = this.testEngine.GetDefaultTestHostManager(Architecture.X86);
            var instance2 = this.testEngine.GetDefaultTestHostManager(Architecture.X86);
            Assert.AreNotEqual(instance1, instance2);
        }
    }
}
