using Microsoft.VisualStudio.TestPlatform.Common.DataCollector;
using Microsoft.VisualStudio.TestPlatform.Common.DataCollector.Interfaces;
using Microsoft.VisualStudio.TestPlatform.Common.DataCollector.UnitTests;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
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
        public void RecordEnvironmentVariableAddition_ShouldDoNothing_IfNotProfilerVariable()
        {
            // act
            this.telemetryManager.RecordEnvironmentVariableAddition(this.dataCollectorInformation, "key", "value");

            // assert
            this.mockMetricsCollection.Verify(c => c.Add(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        }

        [TestMethod]
        public void RecordEnvironmentVariableConflict_ShouldDoNothing_IfNotProfilerVariable_ValuesSame()
        {
            // act
            this.telemetryManager.RecordEnvironmentVariableConflict(this.dataCollectorInformation, "key", "value", "value");

            // assert
            this.mockMetricsCollection.Verify(c => c.Add(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        }

        [TestMethod]
        public void RecordEnvironmentVariableConflict_ShouldDoNothing_IfNotProfilerVariable_ValuesDifferent()
        {
            // act
            this.telemetryManager.RecordEnvironmentVariableConflict(this.dataCollectorInformation, "key", "value", "othervalue");

            // assert
            this.mockMetricsCollection.Verify(c => c.Add(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        }

        [TestMethod]
        [DataRow("{E5F256DC-7959-4DD6-8E4F-C11150AB28E0}", "e5f256dc-7959-4dd6-8e4f-c11150ab28e0")]
        [DataRow("{324F817A-7420-4E6D-B3C1-143FBED6D855}", "324f817a-7420-4e6d-b3c1-143fbed6d855")]
        [DataRow("{9317ae81-bcd8-47b7-aaa1-a28062e41c71}", "9317ae81-bcd8-47b7-aaa1-a28062e41c71")]
        [DataRow("{aaaaaaaa-bcd8-47b7-aaa1-a28062e41c71}", "aaaaaaaa-bcd8-47b7-aaa1-a28062e41c71")]
        [DataRow("{E5F256DC-7959-4DD6-8E4F-c11150AB28E0}", "e5f256dc-7959-4dd6-8e4f-c11150ab28e0")]
        [DataRow("{324f817a-7420-4e6d-b3c1-143fbEd6d855}", "324f817a-7420-4e6d-b3c1-143fbed6d855")]
        [DataRow("{9317AE81-bcd8-47b7-AAA1-A28062E41C71}", "9317ae81-bcd8-47b7-aaa1-a28062e41c71")]
        [DataRow("AAAAAAAAAAAAA", "00000000-0000-0000-0000-000000000000")]
        public void RecordEnvironmentVariableAddition_ShouldCollectTelemetry_IfCorProfilerVariable(string profilerGuid, string profilerName)
        {
            // act
            this.telemetryManager.RecordEnvironmentVariableAddition(this.dataCollectorInformation, "COR_PROFILER", profilerGuid);

            // assert
            this.mockMetricsCollection.Verify(c => c.Add($"VS.TestPlatform.DataCollector.CorProfiler.{dataCollectorInformation.DataCollectorConfig.TypeUri}", profilerName), Times.Once);
        }

        [TestMethod]
        [DataRow("{E5F256DC-7959-4DD6-8E4F-C11150AB28E0}", "e5f256dc-7959-4dd6-8e4f-c11150ab28e0")]
        [DataRow("{324F817A-7420-4E6D-B3C1-143FBED6D855}", "324f817a-7420-4e6d-b3c1-143fbed6d855")]
        [DataRow("{9317ae81-bcd8-47b7-aaa1-a28062e41c71}", "9317ae81-bcd8-47b7-aaa1-a28062e41c71")]
        [DataRow("{aaaaaaaa-bcd8-47b7-aaa1-a28062e41c71}", "aaaaaaaa-bcd8-47b7-aaa1-a28062e41c71")]
        [DataRow("{E5F256DC-7959-4DD6-8E4F-c11150AB28E0}", "e5f256dc-7959-4dd6-8e4f-c11150ab28e0")]
        [DataRow("{324f817a-7420-4e6d-b3c1-143fbEd6d855}", "324f817a-7420-4e6d-b3c1-143fbed6d855")]
        [DataRow("{9317AE81-bcd8-47b7-AAA1-A28062E41C71}", "9317ae81-bcd8-47b7-aaa1-a28062e41c71")]
        [DataRow("AAAAAAAAAAAAA", "00000000-0000-0000-0000-000000000000")]
        public void RecordEnvironmentVariableAddition_ShouldCollectTelemetry_IfCoreClrProfilerVariable(string profilerGuid, string profilerName)
        {
            // act
            this.telemetryManager.RecordEnvironmentVariableAddition(this.dataCollectorInformation, "CORECLR_PROFILER", profilerGuid);

            // assert
            this.mockMetricsCollection.Verify(c => c.Add($"VS.TestPlatform.DataCollector.CoreClrProfiler.{dataCollectorInformation.DataCollectorConfig.TypeUri}", profilerName), Times.Once);
        }

        [TestMethod]
        [DataRow("{0f8fad5b-d9cb-469f-a165-70867728950e}", "{E5F256DC-7959-4DD6-8E4F-C11150AB28E0}", "e5f256dc-7959-4dd6-8e4f-c11150ab28e0")]
        [DataRow("{0f8fad5b-d9cb-469f-a165-70867728950e}", "{324F817A-7420-4E6D-B3C1-143FBED6D855}", "324f817a-7420-4e6d-b3c1-143fbed6d855")]
        [DataRow("{0f8fad5b-d9cb-469f-a165-70867728950e}", "{9317ae81-bcd8-47b7-aaa1-a28062e41c71}", "9317ae81-bcd8-47b7-aaa1-a28062e41c71")]
        [DataRow("{0f8fad5b-d9cb-469f-a165-70867728950e}", "{aaaaaaaa-bcd8-47b7-aaa1-a28062e41c71}", "aaaaaaaa-bcd8-47b7-aaa1-a28062e41c71")]
        [DataRow("{324f817a-7420-4e6d-b3c1-143fbEd6d855}", "{E5F256DC-7959-4DD6-8E4F-c11150AB28E0}", "e5f256dc-7959-4dd6-8e4f-c11150ab28e0")]
        [DataRow("{0f8fad5b-d9cb-469f-a165-70867728950e}", "{324f817a-7420-4e6d-b3c1-143fbEd6d855}", "324f817a-7420-4e6d-b3c1-143fbed6d855")]
        [DataRow("{324f817a-7420-4e6d-b3c1-143fbEd6d855}", "{9317AE81-bcd8-47b7-AAA1-A28062E41C71}", "9317ae81-bcd8-47b7-aaa1-a28062e41c71")]
        [DataRow("{324f817a-7420-4e6d-b3c1-143fbEd6d855}", "AAAAAAAAAAAAA", "00000000-0000-0000-0000-000000000000")]
        public void RecordEnvironmentVariableConflict_ShouldCollectOverwrittenTelemetry_IfCorProfilerVariable(string existingProfilerGuid, string profilerGuid, string expectedOverwrittenProfiler)
        {
            // act
            this.telemetryManager.RecordEnvironmentVariableConflict(this.dataCollectorInformation, "COR_PROFILER", profilerGuid, existingProfilerGuid);

            // assert
            this.mockMetricsCollection.Verify(c => c.Add($"VS.TestPlatform.DataCollector.CorProfiler.{dataCollectorInformation.DataCollectorConfig.TypeUri}", $"{Guid.Parse(existingProfilerGuid)}(overwritten:{expectedOverwrittenProfiler})"), Times.Once);
        }

        [TestMethod]
        [DataRow("{0f8fad5b-d9cb-469f-a165-70867728950e}", "{E5F256DC-7959-4DD6-8E4F-C11150AB28E0}", "e5f256dc-7959-4dd6-8e4f-c11150ab28e0")]
        [DataRow("{0f8fad5b-d9cb-469f-a165-70867728950e}", "{324F817A-7420-4E6D-B3C1-143FBED6D855}", "324f817a-7420-4e6d-b3c1-143fbed6d855")]
        [DataRow("{0f8fad5b-d9cb-469f-a165-70867728950e}", "{9317ae81-bcd8-47b7-aaa1-a28062e41c71}", "9317ae81-bcd8-47b7-aaa1-a28062e41c71")]
        [DataRow("{0f8fad5b-d9cb-469f-a165-70867728950e}", "{aaaaaaaa-bcd8-47b7-aaa1-a28062e41c71}", "aaaaaaaa-bcd8-47b7-aaa1-a28062e41c71")]
        [DataRow("{324f817a-7420-4e6d-b3c1-143fbEd6d855}", "{E5F256DC-7959-4DD6-8E4F-c11150AB28E0}", "e5f256dc-7959-4dd6-8e4f-c11150ab28e0")]
        [DataRow("{0f8fad5b-d9cb-469f-a165-70867728950e}", "{324f817a-7420-4e6d-b3c1-143fbEd6d855}", "324f817a-7420-4e6d-b3c1-143fbed6d855")]
        [DataRow("{324f817a-7420-4e6d-b3c1-143fbEd6d855}", "{9317AE81-bcd8-47b7-AAA1-A28062E41C71}", "9317ae81-bcd8-47b7-aaa1-a28062e41c71")]
        [DataRow("{324f817a-7420-4e6d-b3c1-143fbEd6d855}", "AAAAAAAAAAAAA", "00000000-0000-0000-0000-000000000000")]
        public void RecordEnvironmentVariableConflict_ShouldCollectOverwrittenTelemetry_IfCoreClrProfilerVariable(string existingProfilerGuid, string profilerGuid, string expectedOverwrittenProfiler)
        {
            // act
            this.telemetryManager.RecordEnvironmentVariableConflict(this.dataCollectorInformation, "CORECLR_PROFILER", profilerGuid, existingProfilerGuid);

            // assert
            this.mockMetricsCollection.Verify(c => c.Add($"VS.TestPlatform.DataCollector.CoreClrProfiler.{dataCollectorInformation.DataCollectorConfig.TypeUri}", $"{Guid.Parse(existingProfilerGuid)}(overwritten:{expectedOverwrittenProfiler})"), Times.Once);
        }

        [TestMethod]
        [DataRow("{E5F256DC-7959-4DD6-8E4F-C11150AB28E0}")]
        [DataRow("{324F817A-7420-4E6D-B3C1-143FBED6D855}")]
        [DataRow("{9317ae81-bcd8-47b7-aaa1-a28062e41c71}")]
        [DataRow("{aaaaaaaa-bcd8-47b7-aaa1-a28062e41c71}")]
        [DataRow("{E5F256DC-7959-4DD6-8E4F-c11150AB28E0}")]
        [DataRow("{324f817a-7420-4e6d-b3c1-143fbEd6d855}")]
        [DataRow("{9317AE81-bcd8-47b7-AAA1-A28062E41C71}")]
        [DataRow("AAAAAAAAAAAAA")]
        public void RecordEnvironmentVariableConflict_ShouldCollectClrIeTelemetry_IfCorProfilerVariableAndCollectorSpecifiesClrIeProfile(string profilerGuid)
        {
            // arrange
            this.dataCollectorInformation.SetTestExecutionEnvironmentVariables();

            // act
            this.telemetryManager.RecordEnvironmentVariableConflict(this.dataCollectorInformation, "COR_PROFILER", profilerGuid, "{324F817A-7420-4E6D-B3C1-143FBED6D855}");

            // assert
            this.mockMetricsCollection.Verify(c => c.Add($"VS.TestPlatform.DataCollector.CorProfiler.{dataCollectorInformation.DataCollectorConfig.TypeUri}", "324f817a-7420-4e6d-b3c1-143fbed6d855"), Times.Once);
        }

        [TestMethod]
        [DataRow("{E5F256DC-7959-4DD6-8E4F-C11150AB28E0}")]
        [DataRow("{324F817A-7420-4E6D-B3C1-143FBED6D855}")]
        [DataRow("{9317ae81-bcd8-47b7-aaa1-a28062e41c71}")]
        [DataRow("{aaaaaaaa-bcd8-47b7-aaa1-a28062e41c71}")]
        [DataRow("{E5F256DC-7959-4DD6-8E4F-c11150AB28E0}")]
        [DataRow("{324f817a-7420-4e6d-b3c1-143fbEd6d855}")]
        [DataRow("{9317AE81-bcd8-47b7-AAA1-A28062E41C71}")]
        [DataRow("AAAAAAAAAAAAA")]
        public void RecordEnvironmentVariableConflict_ShouldCollectClrIeTelemetry_IfCoreClrProfilerVariableAndCollectorSpecifiesClrIeProfile(string profilerGuid)
        {
            // arrange
            this.dataCollectorInformation.SetTestExecutionEnvironmentVariables();

            // act
            this.telemetryManager.RecordEnvironmentVariableConflict(this.dataCollectorInformation, "CORECLR_PROFILER", profilerGuid, "{324F817A-7420-4E6D-B3C1-143FBED6D855}");

            // assert
            this.mockMetricsCollection.Verify(c => c.Add($"VS.TestPlatform.DataCollector.CoreClrProfiler.{dataCollectorInformation.DataCollectorConfig.TypeUri}", "324f817a-7420-4e6d-b3c1-143fbed6d855"), Times.Once);
        }

        [TestMethod]
        [DataRow("{E5F256DC-7959-4DD6-8E4F-C11150AB28E0}", "e5f256dc-7959-4dd6-8e4f-c11150ab28e0")]
        [DataRow("{324F817A-7420-4E6D-B3C1-143FBED6D855}", "324f817a-7420-4e6d-b3c1-143fbed6d855")]
        [DataRow("{9317ae81-bcd8-47b7-aaa1-a28062e41c71}", "9317ae81-bcd8-47b7-aaa1-a28062e41c71")]
        [DataRow("{aaaaaaaa-bcd8-47b7-aaa1-a28062e41c71}", "aaaaaaaa-bcd8-47b7-aaa1-a28062e41c71")]
        [DataRow("{E5F256DC-7959-4DD6-8E4F-c11150AB28E0}", "e5f256dc-7959-4dd6-8e4f-c11150ab28e0")]
        [DataRow("{324f817a-7420-4e6d-b3c1-143fbEd6d855}", "324f817a-7420-4e6d-b3c1-143fbed6d855")]
        [DataRow("{9317AE81-bcd8-47b7-AAA1-A28062E41C71}", "9317ae81-bcd8-47b7-aaa1-a28062e41c71")]
        [DataRow("AAAAAAAAAAAAA", "00000000-0000-0000-0000-000000000000")]
        public void RecordEnvironmentVariableConflict_ShouldCollectTelemetry_IfCorProfilerVariableAndBothValuesSame(string profilerGuid, string profilerName)
        {
            // act
            this.telemetryManager.RecordEnvironmentVariableConflict(this.dataCollectorInformation, "COR_PROFILER", profilerGuid, profilerGuid.ToLower());

            // assert
            this.mockMetricsCollection.Verify(c => c.Add($"VS.TestPlatform.DataCollector.CorProfiler.{dataCollectorInformation.DataCollectorConfig.TypeUri}", profilerName), Times.Once);
        }

        [TestMethod]
        [DataRow("{E5F256DC-7959-4DD6-8E4F-C11150AB28E0}", "e5f256dc-7959-4dd6-8e4f-c11150ab28e0")]
        [DataRow("{324F817A-7420-4E6D-B3C1-143FBED6D855}", "324f817a-7420-4e6d-b3c1-143fbed6d855")]
        [DataRow("{9317ae81-bcd8-47b7-aaa1-a28062e41c71}", "9317ae81-bcd8-47b7-aaa1-a28062e41c71")]
        [DataRow("{aaaaaaaa-bcd8-47b7-aaa1-a28062e41c71}", "aaaaaaaa-bcd8-47b7-aaa1-a28062e41c71")]
        [DataRow("{E5F256DC-7959-4DD6-8E4F-c11150AB28E0}", "e5f256dc-7959-4dd6-8e4f-c11150ab28e0")]
        [DataRow("{324f817a-7420-4e6d-b3c1-143fbEd6d855}", "324f817a-7420-4e6d-b3c1-143fbed6d855")]
        [DataRow("{9317AE81-bcd8-47b7-AAA1-A28062E41C71}", "9317ae81-bcd8-47b7-aaa1-a28062e41c71")]
        [DataRow("AAAAAAAAAAAAA", "00000000-0000-0000-0000-000000000000")]
        public void RecordEnvironmentVariableConflict_ShouldCollectTelemetry_IfCoreClrProfilerVariableAndBothValuesSame(string profilerGuid, string profilerName)
        {
            // act
            this.telemetryManager.RecordEnvironmentVariableConflict(this.dataCollectorInformation, "CORECLR_PROFILER", profilerGuid, profilerGuid.ToUpper());

            // assert
            this.mockMetricsCollection.Verify(c => c.Add($"VS.TestPlatform.DataCollector.CoreClrProfiler.{dataCollectorInformation.DataCollectorConfig.TypeUri}", profilerName), Times.Once);
        }
    }
}
