// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TraceDataCollector.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using Coverage;
    using Coverage.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using TestPlatform.ObjectModel.DataCollection;
    using TraceCollector;
    using TraceCollector.Interfaces;
    using IDataCollectionSink = TraceCollector.IDataCollectionSink;

    [TestClass]
    public class DynamicCoverageDataCollectorTests
    {
        private const string DefaultConfig =
            "<Configuration><Framework>.NETCoreApp,Version=v2.0</Framework><TargetPlatform>x64</TargetPlatform></Configuration>";

        private TestableDynamicCoverageDataCollector collector;
        private Mock<IVanguardLocationProvider> vanguardLocationProviderMock;
        private Mock<IDynamicCoverageDataCollectorImpl> implMock;
        private Mock<IDataCollectionEvents> eventsMock;
        private Mock<TraceCollector.IDataCollectionSink> sinkMock;
        private Mock<IDataCollectionLogger> loggerMock;
        private Mock<IDataCollectionAgentContext> agentContextMock;
        private Mock<IEnvironment> environmentMock;

        public DynamicCoverageDataCollectorTests()
        {
            this.vanguardLocationProviderMock = new Mock<IVanguardLocationProvider>();
            this.implMock = new Mock<IDynamicCoverageDataCollectorImpl>();
            this.eventsMock = new Mock<IDataCollectionEvents>();
            this.sinkMock = new Mock<TraceCollector.IDataCollectionSink>();
            this.loggerMock = new Mock<IDataCollectionLogger>();
            this.agentContextMock = new Mock<IDataCollectionAgentContext>();
            this.environmentMock = new Mock<IEnvironment>();
            this.collector = new TestableDynamicCoverageDataCollector(this.vanguardLocationProviderMock.Object, this.implMock.Object, this.environmentMock.Object);
            this.vanguardLocationProviderMock.Setup(u => u.GetVanguardDirectory()).Returns(Directory.GetCurrentDirectory);
            this.environmentMock.Setup(e => e.OperatingSystem).Returns(PlatformOperatingSystem.Windows);
            var configElement = DynamicCoverageDataCollectorImplTests.CreateXmlElement(DynamicCoverageDataCollectorTests.DefaultConfig);
            this.collector.Initialize(configElement, this.eventsMock.Object, this.sinkMock.Object, this.loggerMock.Object, this.agentContextMock.Object);
        }

        [TestMethod]
        public void InitializeShouldNotThrowOnNullConfig()
        {
            XmlElement actualConfig = null;
            this.implMock.Setup(i => i.Initialize(
                    It.IsAny<XmlElement>(),
                    It.IsAny<TraceCollector.IDataCollectionSink>(),
                    It.IsAny<IDataCollectionLogger>()))
                .Callback<XmlElement, IDataCollectionSink, IDataCollectionLogger>((config, sink, logger) =>
                {
                    actualConfig = config;
                });

            this.collector.Initialize(null, this.eventsMock.Object, this.sinkMock.Object, this.loggerMock.Object, this.agentContextMock.Object);

            Assert.IsNull(actualConfig);
        }

        [TestMethod]
        public void InitializeShouldLogWarningIfCurrentOperatingSystemIsUnix()
        {
            this.environmentMock.Setup(e => e.OperatingSystem).Returns(PlatformOperatingSystem.Unix);
            this.collector = new TestableDynamicCoverageDataCollector(this.vanguardLocationProviderMock.Object, null, this.environmentMock.Object);

           this.collector.Initialize(
               null,
               this.eventsMock.Object,
               this.sinkMock.Object,
               this.loggerMock.Object,
               this.agentContextMock.Object);

            var expectedExMsg =
                "No code coverage data available. Code coverage is currently supported only on Windows.";

            this.loggerMock.Verify(l => l.LogWarning(It.IsAny<DataCollectionContext>(), expectedExMsg));
        }

        [TestMethod]
        public void InitializeShouldNotRegisterForSessionEvents()
        {
            this.implMock = new Mock<IDynamicCoverageDataCollectorImpl>();
            this.environmentMock.Setup(e => e.OperatingSystem).Returns(PlatformOperatingSystem.Unix);
            this.collector = new TestableDynamicCoverageDataCollector(this.vanguardLocationProviderMock.Object, null, this.environmentMock.Object);

            this.collector.Initialize(
                null,
                this.eventsMock.Object,
                this.sinkMock.Object,
                this.loggerMock.Object,
                this.agentContextMock.Object);

            this.eventsMock.Raise(e => e.SessionStart += null, new SessionStartEventArgs());
            this.eventsMock.Raise(e => e.SessionEnd += null, new SessionEndEventArgs());

            this.implMock.Verify(i => i.SessionStart(It.IsAny<object>(), It.IsAny<SessionStartEventArgs>()), Times.Never);
            this.implMock.Verify(i => i.SessionEnd(It.IsAny<object>(), It.IsAny<SessionEndEventArgs>()), Times.Never);
        }

        [TestMethod]
        public void InitializeShouldRegisterForSessionStartEvent()
        {
            this.eventsMock.Raise(e => e.SessionStart += null, new SessionStartEventArgs());

            this.implMock.Verify(i => i.SessionStart(It.IsAny<object>(), It.IsAny<SessionStartEventArgs>()));
        }

        [TestMethod]
        public void InitializeShouldRegisterForSessionEndEvent()
        {
            this.eventsMock.Raise(e => e.SessionEnd += null, new SessionEndEventArgs());

            this.implMock.Verify(i => i.SessionEnd(It.IsAny<object>(), It.IsAny<SessionEndEventArgs>()));
        }

        [TestMethod]
        public void InitializeShouldNotLogMessageOnException()
        {
            var exceptionReason = "Failed to create directory";
            this.implMock.Setup(i => i.Initialize(
                    It.IsAny<XmlElement>(),
                    It.IsAny<TraceCollector.IDataCollectionSink>(),
                    It.IsAny<IDataCollectionLogger>()))
                .Throws(new Exception(exceptionReason));

            var actualErrorMessage = string.Empty;
            this.loggerMock.Setup(l => l.LogError(It.IsAny<DataCollectionContext>(), It.IsAny<string>()))
                .Callback<DataCollectionContext, string>((c, m) => { actualErrorMessage = m; });
            Assert.ThrowsException<Exception>(() => this.collector.Initialize(null, this.eventsMock.Object, this.sinkMock.Object, this.loggerMock.Object, this.agentContextMock.Object));

            this.loggerMock.Verify(l => l.LogError(It.IsAny<DataCollectionContext>(), It.IsAny<string>()), Times.Once());

            var expectedMessagePrefix = "Failed to initialize code coverage datacollector with error:";
            StringAssert.StartsWith(actualErrorMessage, expectedMessagePrefix);
            StringAssert.Contains(actualErrorMessage, exceptionReason);
        }

        [TestMethod]
        public void GetEnvironmentVariablesShouldReturnRightEnvVaribles()
        {
            var currentDir = Directory.GetCurrentDirectory();
            var x64profilePath = Path.Combine(currentDir, @"amd64\covrun64.dll");
            var x86profilePath = Path.Combine(currentDir, "covrun32.dll");
            var expectedEnvVariables = new Dictionary<string, string>
            {
                { "CORECLR_ENABLE_PROFILING", "1" },
                { "CORECLR_PROFILER_PATH_32", x86profilePath },
                { "CORECLR_PROFILER_PATH_64", x64profilePath },
                { "CORECLR_PROFILER", "{E5F256DC-7959-4DD6-8E4F-C11150AB28E0}" },
                { "CODE_COVERAGE_SESSION_NAME", "MTM_123" },
                { "COR_PROFILER_PATH_32", x86profilePath },
                { "COR_PROFILER_PATH_64", x64profilePath },
                { "COR_ENABLE_PROFILING", "1" },
                { "COR_PROFILER", "{E5F256DC-7959-4DD6-8E4F-C11150AB28E0}" },
            };

            this.implMock.Setup(i => i.GetSessionName()).Returns("MTM_123");

            var envVars = this.collector.GetEnvironmentVariables();

            foreach (var pair in envVars)
            {
                Console.WriteLine(pair.Key + " ==> " + pair.Value);
            }

            Assert.AreEqual(expectedEnvVariables.Count, envVars.Count());

            foreach (var pair in envVars)
            {
                Assert.IsTrue(expectedEnvVariables.ContainsKey(pair.Key) && expectedEnvVariables[pair.Key].Equals(pair.Value), $"unexpected env variable {pair}");
            }
        }

        [TestMethod]
        public void GetEnvironmentVariablesShouldReturnNoEnvVaribles()
        {
            this.implMock = new Mock<IDynamicCoverageDataCollectorImpl>();
            this.environmentMock.Setup(e => e.OperatingSystem).Returns(PlatformOperatingSystem.Unix);
            this.collector = new TestableDynamicCoverageDataCollector(this.vanguardLocationProviderMock.Object, null, this.environmentMock.Object);

            this.collector.Initialize(
                null,
                this.eventsMock.Object,
                this.sinkMock.Object,
                this.loggerMock.Object,
                this.agentContextMock.Object);
            var envVars = this.collector.GetEnvironmentVariables();

            Assert.IsFalse(envVars.Any(), "No environment variables set on unix.");
        }

        [TestMethod]
        public void DisposeShouldDisposeImpl()
        {
            this.collector.Dispose();
            this.implMock.Verify(i => i.Dispose());
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

        private class TestableDynamicCoverageDataCollector : DynamicCoverageDataCollector
        {
            public TestableDynamicCoverageDataCollector(
                IVanguardLocationProvider vanguardLocationProvider,
                IDynamicCoverageDataCollectorImpl impl,
                IEnvironment environment)
            : base(vanguardLocationProvider, impl, environment)
            {
            }

            public new IEnumerable<KeyValuePair<string, string>> GetEnvironmentVariables()
            {
                return base.GetEnvironmentVariables();
            }
        }
    }
}
