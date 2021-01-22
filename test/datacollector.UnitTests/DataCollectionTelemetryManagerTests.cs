using Microsoft.VisualStudio.TestPlatform.Common.DataCollector;
using Microsoft.VisualStudio.TestPlatform.Common.DataCollector.Interfaces;
using Microsoft.VisualStudio.TestPlatform.Common.DataCollector.UnitTests;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.TestPlatform.DataCollector.UnitTests
{
    [TestClass]
    public class DataCollectionTelemetryManagerTests
    {
        private readonly Mock<IRequestData> mockRequestData;
        private readonly Mock<IMetricsCollection> mockMetricsCollection;
        private readonly DataCollectionTelemetryManager telemetryManager;
        private readonly DataCollectorInformation dataCollectorInformation;

        public DataCollectionTelemetryManagerTests()
        {
            this.mockRequestData = new Mock<IRequestData>();
            this.mockMetricsCollection = new Mock<IMetricsCollection>();
            this.mockRequestData.Setup(m => m.MetricsCollection).Returns(this.mockMetricsCollection.Object);

            var dataCollectorMock = new Mock<CodeCoverageDataCollector>();
            var evnVariablesMock = dataCollectorMock.As<ITestExecutionEnvironmentSpecifier>();
            evnVariablesMock.Setup(a => a.GetTestExecutionEnvironmentVariables()).Returns(new KeyValuePair<string, string>[] 
            {
                new KeyValuePair<string, string>("MicrosoftInstrumentationEngine_ConfigPath32_VanguardInstrumentationProfiler", "path1"),
                new KeyValuePair<string, string>("MicrosoftInstrumentationEngine_ConfigPath64_VanguardInstrumentationProfiler", "path2")
            });

            this.dataCollectorInformation = new DataCollectorInformation(
                dataCollectorMock.Object,
                null,
                new DataCollectorConfig(typeof(CustomDataCollector)),
                null,
                new Mock<IDataCollectionAttachmentManager>().Object,
                new TestPlatformDataCollectionEvents(),
                new Mock<IMessageSink>().Object,
                string.Empty);

            this.telemetryManager = new DataCollectionTelemetryManager(this.mockRequestData.Object);
        }

        [TestMethod]
        public void OnEnvironmentVariableAdded_ShouldDoNothing_IfNotProfilerVariable()
        {
            // act
            this.telemetryManager.OnEnvironmentVariableAdded(this.dataCollectorInformation, "key", "value");

            // assert
            this.mockMetricsCollection.Verify(c => c.Add(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        }

        [TestMethod]
        public void OnEnvironmentVariableConflict_ShouldDoNothing_IfNotProfilerVariable()
        {
            // act
            this.telemetryManager.OnEnvironmentVariableConflict(this.dataCollectorInformation, "key", "value");

            // assert
            this.mockMetricsCollection.Verify(c => c.Add(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        }

        [TestMethod]
        [DataRow("{E5F256DC-7959-4DD6-8E4F-C11150AB28E0}", "vanguard")]
        [DataRow("{324F817A-7420-4E6D-B3C1-143FBED6D855}", "clrie")]
        [DataRow("{9317ae81-bcd8-47b7-aaa1-a28062e41c71}", "intellitrace")]
        [DataRow("{aaaaaaaa-bcd8-47b7-aaa1-a28062e41c71}", "unknown")]
        [DataRow("{E5F256DC-7959-4DD6-8E4F-c11150AB28E0}", "vanguard")]
        [DataRow("{324f817a-7420-4e6d-b3c1-143fbEd6d855}", "clrie")]
        [DataRow("{9317AE81-bcd8-47b7-AAA1-A28062E41C71}", "intellitrace")]
        public void OnEnvironmentVariableAdded_ShouldCollectTelemetry_IfCorProfilerVariable(string profilerGuid, string profilerName)
        {
            // act
            this.telemetryManager.OnEnvironmentVariableAdded(this.dataCollectorInformation, "COR_PROFILER", profilerGuid);

            // assert
            this.mockMetricsCollection.Verify(c => c.Add($"VS.TestPlatform.DataCollector.{dataCollectorInformation.DataCollectorConfig.TypeUri}.CorProfiler", profilerName), Times.Once);
        }

        [TestMethod]
        [DataRow("{E5F256DC-7959-4DD6-8E4F-C11150AB28E0}", "vanguard")]
        [DataRow("{324F817A-7420-4E6D-B3C1-143FBED6D855}", "clrie")]
        [DataRow("{9317ae81-bcd8-47b7-aaa1-a28062e41c71}", "intellitrace")]
        [DataRow("{aaaaaaaa-bcd8-47b7-aaa1-a28062e41c71}", "unknown")]
        [DataRow("{E5F256DC-7959-4DD6-8E4F-c11150AB28E0}", "vanguard")]
        [DataRow("{324f817a-7420-4e6d-b3c1-143fbEd6d855}", "clrie")]
        [DataRow("{9317AE81-bcd8-47b7-AAA1-A28062E41C71}", "intellitrace")]
        public void OnEnvironmentVariableAdded_ShouldCollectTelemetry_IfCoreClrProfilerVariable(string profilerGuid, string profilerName)
        {
            // act
            this.telemetryManager.OnEnvironmentVariableAdded(this.dataCollectorInformation, "CORECLR_PROFILER", profilerGuid);

            // assert
            this.mockMetricsCollection.Verify(c => c.Add($"VS.TestPlatform.DataCollector.{dataCollectorInformation.DataCollectorConfig.TypeUri}.CoreClrProfiler", profilerName), Times.Once);
        }

        [TestMethod]
        [DataRow("{E5F256DC-7959-4DD6-8E4F-C11150AB28E0}")]
        [DataRow("{324F817A-7420-4E6D-B3C1-143FBED6D855}")]
        [DataRow("{9317ae81-bcd8-47b7-aaa1-a28062e41c71}")]
        [DataRow("{aaaaaaaa-bcd8-47b7-aaa1-a28062e41c71}")]
        [DataRow("{E5F256DC-7959-4DD6-8E4F-c11150AB28E0}")]
        [DataRow("{324f817a-7420-4e6d-b3c1-143fbEd6d855}")]
        [DataRow("{9317AE81-bcd8-47b7-AAA1-A28062E41C71}")]
        public void OnEnvironmentVariableConflict_ShouldCollectOverwrittenTelemetry_IfCorProfilerVariable(string profilerGuid)
        {
            // act
            this.telemetryManager.OnEnvironmentVariableConflict(this.dataCollectorInformation, "COR_PROFILER", profilerGuid);

            // assert
            this.mockMetricsCollection.Verify(c => c.Add($"VS.TestPlatform.DataCollector.{dataCollectorInformation.DataCollectorConfig.TypeUri}.CorProfiler", "overwritten"), Times.Once);
        }

        [TestMethod]
        [DataRow("{E5F256DC-7959-4DD6-8E4F-C11150AB28E0}")]
        [DataRow("{324F817A-7420-4E6D-B3C1-143FBED6D855}")]
        [DataRow("{9317ae81-bcd8-47b7-aaa1-a28062e41c71}")]
        [DataRow("{aaaaaaaa-bcd8-47b7-aaa1-a28062e41c71}")]
        [DataRow("{E5F256DC-7959-4DD6-8E4F-c11150AB28E0}")]
        [DataRow("{324f817a-7420-4e6d-b3c1-143fbEd6d855}")]
        [DataRow("{9317AE81-bcd8-47b7-AAA1-A28062E41C71}")]
        public void OnEnvironmentVariableConflict_ShouldCollectOverwrittenTelemetry_IfCoreClrProfilerVariable(string profilerGuid)
        {
            // act
            this.telemetryManager.OnEnvironmentVariableConflict(this.dataCollectorInformation, "CORECLR_PROFILER", profilerGuid);

            // assert
            this.mockMetricsCollection.Verify(c => c.Add($"VS.TestPlatform.DataCollector.{dataCollectorInformation.DataCollectorConfig.TypeUri}.CoreClrProfiler", "overwritten"), Times.Once);
        }

        [TestMethod]
        public void OnEnvironmentVariableConflict_ShouldCollectClrIeTelemetry_IfCorProfilerVariableAndCollectorSpecifiesClrIeProfile()
        {
            // arrange
            this.dataCollectorInformation.SetTestExecutionEnvironmentVariables();

            // act
            this.telemetryManager.OnEnvironmentVariableConflict(this.dataCollectorInformation, "COR_PROFILER", "{324F817A-7420-4E6D-B3C1-143FBED6D855}");

            // assert
            this.mockMetricsCollection.Verify(c => c.Add($"VS.TestPlatform.DataCollector.{dataCollectorInformation.DataCollectorConfig.TypeUri}.CorProfiler", "clrie"), Times.Once);
        }

        [TestMethod]
        public void OnEnvironmentVariableConflict_ShouldCollectClrIeTelemetry_IfCoreClrProfilerVariableAndCollectorSpecifiesClrIeProfile()
        {
            // arrange
            this.dataCollectorInformation.SetTestExecutionEnvironmentVariables();

            // act
            this.telemetryManager.OnEnvironmentVariableConflict(this.dataCollectorInformation, "CORECLR_PROFILER", "{324F817A-7420-4E6D-B3C1-143FBED6D855}");

            // assert
            this.mockMetricsCollection.Verify(c => c.Add($"VS.TestPlatform.DataCollector.{dataCollectorInformation.DataCollectorConfig.TypeUri}.CoreClrProfiler", "clrie"), Times.Once);
        }
    }
}
