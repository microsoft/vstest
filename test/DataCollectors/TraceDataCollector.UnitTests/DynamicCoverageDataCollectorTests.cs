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

        private const string ConfigWithClrIeEnabledForNetCore =
            @"<Configuration>
                <Framework>.NETCoreApp,Version=v2.0</Framework>
                <TargetPlatform>x64</TargetPlatform>
                <CLRIEInstrumentationNetCore>true</CLRIEInstrumentationNetCore>
                <CLRIEInstrumentationNetFramework>false</CLRIEInstrumentationNetFramework>
            </Configuration>";

        private const string ConfigWithClrIeEnabled =
            @"<Configuration>
                <Framework>.NETCoreApp,Version=v2.0</Framework>
                <TargetPlatform>x64</TargetPlatform>
                <CLRIEInstrumentationNetCore>true</CLRIEInstrumentationNetCore>
                <CLRIEInstrumentationNetFramework>true</CLRIEInstrumentationNetFramework>
            </Configuration>";

        private TestableDynamicCoverageDataCollector collector;
        private Mock<IProfilersLocationProvider> vanguardLocationProviderMock;
        private Mock<IDynamicCoverageDataCollectorImpl> implMock;
        private Mock<IDataCollectionEvents> eventsMock;
        private Mock<TraceCollector.IDataCollectionSink> sinkMock;
        private Mock<IDataCollectionLogger> loggerMock;
        private Mock<IDataCollectionAgentContext> agentContextMock;
        private Mock<IEnvironment> environmentMock;

        public DynamicCoverageDataCollectorTests()
        {
            this.vanguardLocationProviderMock = new Mock<IProfilersLocationProvider>();
            this.implMock = new Mock<IDynamicCoverageDataCollectorImpl>();
            this.eventsMock = new Mock<IDataCollectionEvents>();
            this.sinkMock = new Mock<TraceCollector.IDataCollectionSink>();
            this.loggerMock = new Mock<IDataCollectionLogger>();
            this.agentContextMock = new Mock<IDataCollectionAgentContext>();
            this.environmentMock = new Mock<IEnvironment>();
            this.collector = new TestableDynamicCoverageDataCollector(this.vanguardLocationProviderMock.Object, this.implMock.Object, this.environmentMock.Object);

            this.vanguardLocationProviderMock.Setup(u => u.GetVanguardProfilerX86Path()).Returns(@"covrun86");
            this.vanguardLocationProviderMock.Setup(u => u.GetVanguardProfilerX64Path()).Returns(@"covrun64");
            this.vanguardLocationProviderMock.Setup(u => u.GetVanguardProfilerConfigX86Path()).Returns(@"config86");
            this.vanguardLocationProviderMock.Setup(u => u.GetVanguardProfilerConfigX64Path()).Returns(@"config64");
            this.vanguardLocationProviderMock.Setup(u => u.GetClrInstrumentationEngineX86Path()).Returns(@"clrie86");
            this.vanguardLocationProviderMock.Setup(u => u.GetClrInstrumentationEngineX64Path()).Returns(@"clrie64");

            this.environmentMock.Setup(e => e.OperatingSystem).Returns(PlatformOperatingSystem.Windows);
            var configElement = DynamicCoverageDataCollectorImplTests.CreateXmlElement(DynamicCoverageDataCollectorTests.DefaultConfig);
            this.collector.Initialize(configElement, this.eventsMock.Object, this.sinkMock.Object, this.loggerMock.Object, this.agentContextMock.Object);
        }

        [TestCleanup]
        public void CleanEnvVariables()
        {
            Environment.SetEnvironmentVariable("VANGUARD_CLR_IE_INSTRUMENTATION_NETCORE", null);
            Environment.SetEnvironmentVariable("VANGUARD_CLR_IE_INSTRUMENTATION_NETFRAMEWORK", null);
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
            var expectedEnvVariables = new Dictionary<string, string>
            {
                { "CORECLR_ENABLE_PROFILING", "1" },
                { "CORECLR_PROFILER_PATH_32", "covrun86" },
                { "CORECLR_PROFILER_PATH_64", "covrun64" },
                { "CORECLR_PROFILER", "{E5F256DC-7959-4DD6-8E4F-C11150AB28E0}" },
                { "CODE_COVERAGE_SESSION_NAME", "MTM_123" },
                { "COR_PROFILER_PATH_32", "covrun86" },
                { "COR_PROFILER_PATH_64", "covrun64" },
                { "COR_ENABLE_PROFILING", "1" },
                { "COR_PROFILER", "{E5F256DC-7959-4DD6-8E4F-C11150AB28E0}" },
                { "MicrosoftInstrumentationEngine_ConfigPath32_VanguardInstrumentationProfiler", "config86" },
                { "MicrosoftInstrumentationEngine_ConfigPath64_VanguardInstrumentationProfiler", "config64" }
            };

            this.implMock.Setup(i => i.GetSessionName()).Returns("MTM_123");

            var envVars = this.collector.GetEnvironmentVariables();

            VerifyEnvironmentVariables(expectedEnvVariables, envVars);
        }

        [TestMethod]
        public void GetEnvironmentVariablesShouldReturnRightEnvVariblesClrInstrumentationEngineEnabled()
        {
            var configElement = DynamicCoverageDataCollectorImplTests.CreateXmlElement(DynamicCoverageDataCollectorTests.ConfigWithClrIeEnabled);
            this.collector.Initialize(configElement, this.eventsMock.Object, this.sinkMock.Object, this.loggerMock.Object, this.agentContextMock.Object);

            var expectedEnvVariables = new Dictionary<string, string>
            {
                { "CORECLR_ENABLE_PROFILING", "1" },
                { "CORECLR_PROFILER_PATH_32", "clrie86" },
                { "CORECLR_PROFILER_PATH_64", "clrie64" },
                { "CORECLR_PROFILER", "{324F817A-7420-4E6D-B3C1-143FBED6D855}" },
                { "CODE_COVERAGE_SESSION_NAME", "MTM_123" },
                { "COR_PROFILER_PATH_32", "clrie86" },
                { "COR_PROFILER_PATH_64", "clrie64" },
                { "COR_ENABLE_PROFILING", "1" },
                { "COR_PROFILER", "{324F817A-7420-4E6D-B3C1-143FBED6D855}" },
                { "MicrosoftInstrumentationEngine_ConfigPath32_VanguardInstrumentationProfiler", "config86" },
                { "MicrosoftInstrumentationEngine_ConfigPath64_VanguardInstrumentationProfiler", "config64" },
                { "MicrosoftInstrumentationEngine_LogLevel", "Errors" },
                { "MicrosoftInstrumentationEngine_LogLevel_{2A1F2A34-8192-44AC-A9D8-4FCC03DCBAA8}", "Errors" },
                { "MicrosoftInstrumentationEngine_DisableCodeSignatureValidation", "1" },
                { "MicrosoftInstrumentationEngine_FileLogPath", @"GENERATED" },
            };

            this.implMock.Setup(i => i.GetSessionName()).Returns("MTM_123");

            var envVars = this.collector.GetEnvironmentVariables();

            VerifyEnvironmentVariables(expectedEnvVariables, envVars);
        }

        [TestMethod]
        public void GetEnvironmentVariablesShouldReturnRightEnvVariblesClrInstrumentationEngineEnabledThroughEnvVars()
        {
            Environment.SetEnvironmentVariable("VANGUARD_CLR_IE_INSTRUMENTATION_NETCORE", "1");
            Environment.SetEnvironmentVariable("VANGUARD_CLR_IE_INSTRUMENTATION_NETFRAMEWORK", "1");

            var configElement = DynamicCoverageDataCollectorImplTests.CreateXmlElement(DynamicCoverageDataCollectorTests.DefaultConfig);
            this.collector.Initialize(configElement, this.eventsMock.Object, this.sinkMock.Object, this.loggerMock.Object, this.agentContextMock.Object);

            var expectedEnvVariables = new Dictionary<string, string>
            {
                { "CORECLR_ENABLE_PROFILING", "1" },
                { "CORECLR_PROFILER_PATH_32", "clrie86" },
                { "CORECLR_PROFILER_PATH_64", "clrie64" },
                { "CORECLR_PROFILER", "{324F817A-7420-4E6D-B3C1-143FBED6D855}" },
                { "CODE_COVERAGE_SESSION_NAME", "MTM_123" },
                { "COR_PROFILER_PATH_32", "clrie86" },
                { "COR_PROFILER_PATH_64", "clrie64" },
                { "COR_ENABLE_PROFILING", "1" },
                { "COR_PROFILER", "{324F817A-7420-4E6D-B3C1-143FBED6D855}" },
                { "MicrosoftInstrumentationEngine_ConfigPath32_VanguardInstrumentationProfiler", "config86" },
                { "MicrosoftInstrumentationEngine_ConfigPath64_VanguardInstrumentationProfiler", "config64" },
                { "MicrosoftInstrumentationEngine_LogLevel", "Errors" },
                { "MicrosoftInstrumentationEngine_LogLevel_{2A1F2A34-8192-44AC-A9D8-4FCC03DCBAA8}", "Errors" },
                { "MicrosoftInstrumentationEngine_DisableCodeSignatureValidation", "1" },
                { "MicrosoftInstrumentationEngine_FileLogPath", @"GENERATED" },
            };

            this.implMock.Setup(i => i.GetSessionName()).Returns("MTM_123");

            var envVars = this.collector.GetEnvironmentVariables();

            VerifyEnvironmentVariables(expectedEnvVariables, envVars);
        }

        [TestMethod]
        public void GetEnvironmentVariablesShouldReturnRightEnvVariblesClrInstrumentationEngineEnabledForNetCore()
        {
            var configElement = DynamicCoverageDataCollectorImplTests.CreateXmlElement(DynamicCoverageDataCollectorTests.ConfigWithClrIeEnabledForNetCore);
            this.collector.Initialize(configElement, this.eventsMock.Object, this.sinkMock.Object, this.loggerMock.Object, this.agentContextMock.Object);

            var expectedEnvVariables = new Dictionary<string, string>
            {
                { "CORECLR_ENABLE_PROFILING", "1" },
                { "CORECLR_PROFILER_PATH_32", "clrie86" },
                { "CORECLR_PROFILER_PATH_64", "clrie64" },
                { "CORECLR_PROFILER", "{324F817A-7420-4E6D-B3C1-143FBED6D855}" },
                { "CODE_COVERAGE_SESSION_NAME", "MTM_123" },
                { "COR_PROFILER_PATH_32", "covrun86" },
                { "COR_PROFILER_PATH_64", "covrun64" },
                { "COR_ENABLE_PROFILING", "1" },
                { "COR_PROFILER", "{E5F256DC-7959-4DD6-8E4F-C11150AB28E0}" },
                { "MicrosoftInstrumentationEngine_ConfigPath32_VanguardInstrumentationProfiler", "config86" },
                { "MicrosoftInstrumentationEngine_ConfigPath64_VanguardInstrumentationProfiler", "config64" },
                { "MicrosoftInstrumentationEngine_LogLevel", "Errors" },
                { "MicrosoftInstrumentationEngine_LogLevel_{2A1F2A34-8192-44AC-A9D8-4FCC03DCBAA8}", "Errors" },
                { "MicrosoftInstrumentationEngine_DisableCodeSignatureValidation", "1" },
                { "MicrosoftInstrumentationEngine_FileLogPath", @"GENERATED" },
            };

            this.implMock.Setup(i => i.GetSessionName()).Returns("MTM_123");

            var envVars = this.collector.GetEnvironmentVariables();

            VerifyEnvironmentVariables(expectedEnvVariables, envVars);
        }

        [TestMethod]
        public void GetEnvironmentVariablesShouldReturnRightEnvVariblesClrInstrumentationEngineEnabledForNetCoreThroughEnvVar()
        {
            Environment.SetEnvironmentVariable("VANGUARD_CLR_IE_INSTRUMENTATION_NETCORE", "1");
            Environment.SetEnvironmentVariable("VANGUARD_CLR_IE_INSTRUMENTATION_NETFRAMEWORK", "0");

            var configElement = DynamicCoverageDataCollectorImplTests.CreateXmlElement(DynamicCoverageDataCollectorTests.DefaultConfig);
            this.collector.Initialize(configElement, this.eventsMock.Object, this.sinkMock.Object, this.loggerMock.Object, this.agentContextMock.Object);

            var expectedEnvVariables = new Dictionary<string, string>
            {
                { "CORECLR_ENABLE_PROFILING", "1" },
                { "CORECLR_PROFILER_PATH_32", "clrie86" },
                { "CORECLR_PROFILER_PATH_64", "clrie64" },
                { "CORECLR_PROFILER", "{324F817A-7420-4E6D-B3C1-143FBED6D855}" },
                { "CODE_COVERAGE_SESSION_NAME", "MTM_123" },
                { "COR_PROFILER_PATH_32", "covrun86" },
                { "COR_PROFILER_PATH_64", "covrun64" },
                { "COR_ENABLE_PROFILING", "1" },
                { "COR_PROFILER", "{E5F256DC-7959-4DD6-8E4F-C11150AB28E0}" },
                { "MicrosoftInstrumentationEngine_ConfigPath32_VanguardInstrumentationProfiler", "config86" },
                { "MicrosoftInstrumentationEngine_ConfigPath64_VanguardInstrumentationProfiler", "config64" },
                { "MicrosoftInstrumentationEngine_LogLevel", "Errors" },
                { "MicrosoftInstrumentationEngine_LogLevel_{2A1F2A34-8192-44AC-A9D8-4FCC03DCBAA8}", "Errors" },
                { "MicrosoftInstrumentationEngine_DisableCodeSignatureValidation", "1" },
                { "MicrosoftInstrumentationEngine_FileLogPath", @"GENERATED" },
            };

            this.implMock.Setup(i => i.GetSessionName()).Returns("MTM_123");

            var envVars = this.collector.GetEnvironmentVariables();

            VerifyEnvironmentVariables(expectedEnvVariables, envVars);
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

        private static void VerifyEnvironmentVariables(Dictionary<string, string> expectedEnvVariables, IEnumerable<KeyValuePair<string, string>> envVars)
        {
            foreach (var pair in envVars)
            {
                Console.WriteLine(pair.Key + " ==> " + pair.Value);
            }

            Assert.AreEqual(expectedEnvVariables.Count, envVars.Count());

            foreach (var pair in envVars)
            {
                Assert.IsTrue(expectedEnvVariables.ContainsKey(pair.Key), $"unexpected env variable {pair}");
                Assert.IsTrue(pair.Key == "MicrosoftInstrumentationEngine_FileLogPath" || expectedEnvVariables[pair.Key].Equals(pair.Value), $"unexpected env variable {pair}");
            }
        }

        private class TestableDynamicCoverageDataCollector : DynamicCoverageDataCollector
        {
            public TestableDynamicCoverageDataCollector(
                IProfilersLocationProvider vanguardLocationProvider,
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
