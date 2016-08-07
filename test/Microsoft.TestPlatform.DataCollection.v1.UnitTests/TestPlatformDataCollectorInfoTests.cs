// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.TestPlatform.DataCollection.V1.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.Common.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.DataCollection.V1;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using DataCollectionContext = Microsoft.VisualStudio.TestTools.Execution.DataCollectionContext;
    using DataCollectionEnvironmentContext = Microsoft.VisualStudio.TestTools.Execution.DataCollectionEnvironmentContext;
    using DataCollector = Microsoft.VisualStudio.TestTools.Execution.DataCollector;
    using DataCollectorInformation = Microsoft.VisualStudio.TestTools.Execution.DataCollectorInformation;
    using ITestExecutionEnvironmentSpecifier = Microsoft.VisualStudio.TestTools.Execution.ITestExecutionEnvironmentSpecifier;
    using SafeAbortableUserWorkItemFactory = Microsoft.VisualStudio.TestTools.Common.SafeAbortableUserWorkItemFactory;
    using SessionId = Microsoft.VisualStudio.TestTools.Common.SessionId;

    [TestClass]
    public class TestPlatformDataCollectorInfoTests
    {
        private MockDataCollector mockDataCollector;
        private TestPlatformDataCollectorInfo testPlatformDataCollectorInfo;
        private IMessageSink mockMessageSink;
        private DataCollectorInformation mockDataCollectorInformation;
        private XmlElement dummyXmlElement;

        [TestInitialize]
        public void Init()
        {
            this.mockDataCollector = new MockDataCollector();
            this.mockMessageSink = new DummyMessageSink();
            this.mockDataCollectorInformation = new DataCollectorInformation();
            this.dummyXmlElement = new XmlDocument().CreateElement("Root");
            this.testPlatformDataCollectorInfo = new TestPlatformDataCollectorInfo(this.mockDataCollector, this.dummyXmlElement, this.mockMessageSink, this.mockDataCollectorInformation, new SafeAbortableUserWorkItemFactory());
        }

        [TestCleanup]
        public void Cleanup()
        {
            MockDataCollector.IsInitializeInvoked = false;
            MockDataCollector.GetTestExecutionEnvironmentVariablesThrowException = false;
            MockDataCollector.EnvVarList = null;
            MockDataCollector.DisposeShouldThrowException = false;
            MockDataCollector.IsDisposeInvoked = false;
        }

        [TestMethod]
        public void InitializeDataCollectorShouldInitializeDataCollector()
        {
            var envContext = DataCollectionEnvironmentContext.CreateForLocalEnvironment(new DataCollectionContext(new SessionId(Guid.NewGuid())));
            this.testPlatformDataCollectorInfo.InitializeDataCollector(envContext);

            Assert.IsTrue(MockDataCollector.IsInitializeInvoked);
        }

        [TestMethod]
        public void InitializeDataCollectorShouldThrowExecptionIfDataCollectorInitThrowsException()
        {
            MockDataCollector.ThrowExceptionWhenInitialized = true;

            Assert.ThrowsException<Exception>(() =>
            {
                var envContext = DataCollectionEnvironmentContext.CreateForLocalEnvironment(new DataCollectionContext(new SessionId(Guid.NewGuid())));
                this.testPlatformDataCollectorInfo.InitializeDataCollector(envContext);
            });
        }

        [TestMethod]
        public void GetTestExecutionEnvironmentVariablesShouldGetEnvVariablesFromDataCollector()
        {
            var envVarList = new List<KeyValuePair<string, string>>();
            envVarList.Add(new KeyValuePair<string, string>("key", "value"));
            MockDataCollector.EnvVarList = envVarList;

            this.testPlatformDataCollectorInfo.GetTestExecutionEnvironmentVariables();

            Assert.IsTrue(MockDataCollector.IsGetTestExecutionEnvironmentVariablesInvoked);
            Assert.AreEqual(1, this.testPlatformDataCollectorInfo.TestExecutionEnvironmentVariables.Count());
            Assert.AreEqual("key", this.testPlatformDataCollectorInfo.TestExecutionEnvironmentVariables.First().Key);
            Assert.AreEqual("value", this.testPlatformDataCollectorInfo.TestExecutionEnvironmentVariables.First().Value);
        }

        [TestMethod]
        public void GetTestExecutionEnvironmentVariablesShouldThrowExceptionIfCallFails()
        {
            MockDataCollector.GetTestExecutionEnvironmentVariablesThrowException = true;
            Assert.ThrowsException<Exception>(() =>
            {
                this.testPlatformDataCollectorInfo.GetTestExecutionEnvironmentVariables();
            });
        }

        [TestMethod]
        public void DisposeDataCollectorShouldDisposeDataCollecdtor()
        {
            this.testPlatformDataCollectorInfo.DisposeDataCollector();

            Assert.IsTrue(MockDataCollector.IsDisposeInvoked);
        }

        [TestMethod]
        public void DisposeDataCollectorShouldEatExceptionIfDisposingDataCollectorFails()
        {
            MockDataCollector.DisposeShouldThrowException = true;

            this.testPlatformDataCollectorInfo.DisposeDataCollector();

            Assert.IsTrue(MockDataCollector.IsDisposeInvoked);
        }
    }
}
