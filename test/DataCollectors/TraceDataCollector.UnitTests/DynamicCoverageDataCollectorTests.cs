// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TraceCollector.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using Collector;
    using Coverage;
    using Coverage.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using TestPlatform.ObjectModel.DataCollection;
    using IDataCollectionSink = TraceCollector.IDataCollectionSink;

    [TestClass]
    public class DynamicCoverageDataCollectorTests
    {
        private const string DefaultConfigFormat =
            "<Configuration><Framework>{0}</Framework><TargetPlatform>{1}</TargetPlatform></Configuration>";
        private TestableDynamicCoverageDataCollector collector;
        private Mock<ICollectorUtility> collectorUtilityMock;
        private Mock<IDynamicCoverageDataCollectorImpl> implMock;
        private Mock<IDataCollectionEvents> eventsMock;
        private Mock<IDataCollectionSink> sinkMock;
        private Mock<IDataCollectionLogger> loggerMock;
        private Mock<IDataCollectionAgentContext> agentContextMock;

        public DynamicCoverageDataCollectorTests()
        {
            this.collectorUtilityMock = new Mock<ICollectorUtility>();
            this.implMock = new Mock<IDynamicCoverageDataCollectorImpl>();
            this.eventsMock = new Mock<IDataCollectionEvents>();
            this.sinkMock = new Mock<IDataCollectionSink>();
            this.loggerMock = new Mock<IDataCollectionLogger>();
            this.agentContextMock = new Mock<IDataCollectionAgentContext>();
            this.collector = new TestableDynamicCoverageDataCollector(this.collectorUtilityMock.Object, this.implMock.Object);
            this.collectorUtilityMock.Setup(u => u.GetVanguardDirectory()).Returns(Directory.GetCurrentDirectory);
            var configElement = this.SetupForGetEnvironmentVariables(".NETCoreApp,Version=v2.0", "x64");
            this.collector.Initialize(configElement, this.eventsMock.Object, this.sinkMock.Object, this.loggerMock.Object, this.agentContextMock.Object);
        }

        [TestMethod]
        public void InitilizeShouldNotThrowOnNullConfig()
        {
            XmlElement actualConfig = null;
            this.implMock.Setup(i => i.Initialize(It.IsAny<XmlElement>(), It.IsAny<IDataCollectionSink>(),
                    It.IsAny<IDataCollectionLogger>()))
                .Callback<XmlElement, IDataCollectionSink, IDataCollectionLogger>((config, sink, logger) =>
                {
                    actualConfig = config;
                });

            this.collector.Initialize(null,this.eventsMock.Object, this.sinkMock.Object, this.loggerMock.Object, this.agentContextMock.Object);

            Assert.AreEqual(null, actualConfig);
        }

        [TestMethod]
        public void InitilizeShouldRegisterForSessionStartEvent()
        {

            this.eventsMock.Raise(e => e.SessionStart += null, new SessionStartEventArgs());

            this.implMock.Verify(i => i.SessionStart(It.IsAny<object>(), It.IsAny<SessionStartEventArgs>()));
        }

        [TestMethod]
        public void InitilizeShouldRegisterForSessionEndEvent()
        {
            this.eventsMock.Raise(e => e.SessionEnd += null, new SessionEndEventArgs());

            this.implMock.Verify(i => i.SessionEnd(It.IsAny<object>(), It.IsAny<SessionEndEventArgs>()));
        }

        [DataRow(".NETCoreApp,Version=v2.0", "x86")]
        [DataRow(".NETCoreApp,Version=v2.0", "X64")]
        [DataRow(".NETFramework,Version=v4.6", "X86")]
        [DataRow(".NETFramework,Version=v4.6", "x64")]
        [TestMethod]
        public void GetEnvironmentVariablesShouldReturnRightEnvVaribles(string framework, string targetPlatform)
        {
            var currentDir = Directory.GetCurrentDirectory();
            var profileRelativePath = targetPlatform.Equals("x86", StringComparison.OrdinalIgnoreCase)? "covrun32.dll": @"amd64\covrun64.dll";
            var profilePath = Path.Combine(currentDir, profileRelativePath);
            var expectedEnvVariables = new Dictionary<string, string>()
            {
                { "CORECLR_ENABLE_PROFILING", "1"},
                { "CORECLR_PROFILER_PATH", profilePath},
                { "CORECLR_PROFILER", "{E5F256DC-7959-4DD6-8E4F-C11150AB28E0}"},
                { "CODE_COVERAGE_SESSION_NAME", "MTM_123"},
                { "COR_PROFILER_PATH", profilePath},
                { "COR_ENABLE_PROFILING", "1"},
                { "COR_PROFILER", "{E5F256DC-7959-4DD6-8E4F-C11150AB28E0}"},
            };

            var configElement = SetupForGetEnvironmentVariables(framework, targetPlatform);
            this.collector.Initialize(configElement, this.eventsMock.Object, this.sinkMock.Object, this.loggerMock.Object, this.agentContextMock.Object);
            var envVars = this.collector.GetEnvironmentVariables();

            Assert.AreEqual(expectedEnvVariables.Count, envVars.Count());
            foreach (var pair in envVars)
            {
                Assert.IsTrue(expectedEnvVariables.ContainsKey(pair.Key) && expectedEnvVariables[pair.Key].Equals(pair.Value), $"unexpected env variable {pair}");
            }
        }

        [DataRow(".NETCoreApp,Version=v2.0", "Native")]
        [DataRow(".NETFramework,Version=v4.6", "ARM")]
        [DataRow(".NETFramework,Version=v4.6", null)]
        [DataRow(".NETFramework,Version=v4.6", " ")]
        [TestMethod]
        public void GetEnvironmentVariablesShouldThrowForNotSupportedConfig(string framework, string targetPlatform)
        {
            var configElement = SetupForGetEnvironmentVariables(framework, targetPlatform);

            this.collector.Initialize(configElement, this.eventsMock.Object, this.sinkMock.Object, this.loggerMock.Object, this.agentContextMock.Object);
            var exception = Assert.ThrowsException<VanguardException>( () => this.collector.GetEnvironmentVariables());

            Assert.AreEqual($"Code Coverage not available for TargetPlatform: \"{targetPlatform}\"", exception.Message);
        }

        [TestMethod]
        public void DisposeShouldDisposeImpl()
        {
            this.collector.Dispose();
            this.implMock.Verify( i => i.Dispose());
        }

        [TestMethod]
        public void DisposeShouldUnsubcribeEvents()
        {
            this.collector.Dispose();
            this.eventsMock.Raise(e => e.SessionStart += null, new SessionStartEventArgs());
            this.eventsMock.Raise(e => e.SessionEnd += null, new SessionEndEventArgs());

            this.implMock.Verify(i => i.SessionStart(It.IsAny<object>(), It.IsAny<SessionStartEventArgs>()), Times.Never);
            this.implMock.Verify(i => i.SessionEnd(It.IsAny<object>(), It.IsAny<SessionEndEventArgs>()), Times.Never);
        }

        private XmlElement SetupForGetEnvironmentVariables(string framework, string targetPlatform)
        {
            string config = string.Format(DynamicCoverageDataCollectorTests.DefaultConfigFormat, framework, targetPlatform);
            var configElement = DynamicCoverageDataCollectorImplTests.CreateXmlElement(config);

            this.collectorUtilityMock.Setup(u =>
                u.RemoveChildNodeAndReturnValue(ref configElement, "Framework", out framework));

            this.collectorUtilityMock.Setup(u =>
                u.RemoveChildNodeAndReturnValue(ref configElement, "TargetPlatform", out targetPlatform));
            this.SetupGetMachineType(targetPlatform);

            this.implMock.Setup(i => i.GetSessionName()).Returns("MTM_123");
            return configElement;
        }

        private void SetupGetMachineType(string targetPlatform)
        {
            this.collectorUtilityMock.Setup(u => u.GetMachineType(It.IsAny<string>()))
                .Returns(() =>
                {
                    if (targetPlatform.Equals("x86", StringComparison.OrdinalIgnoreCase))
                    {
                        return CollectorUtility.MachineType.I386;
                    }
                    else if (targetPlatform.Equals("x64", StringComparison.OrdinalIgnoreCase))
                    {
                        return CollectorUtility.MachineType.X64;
                    }
                    else
                    {
                        return CollectorUtility.MachineType.Native;
                    }
                });
        }

        private class TestableDynamicCoverageDataCollector : DynamicCoverageDataCollector
        {
            public TestableDynamicCoverageDataCollector(ICollectorUtility collectorUtility, IDynamicCoverageDataCollectorImpl impl)
            :base(collectorUtility, impl)
            {
            }

            public new IEnumerable<KeyValuePair<string, string>> GetEnvironmentVariables()
            {
                return base.GetEnvironmentVariables();
            }
        }
    }
}
