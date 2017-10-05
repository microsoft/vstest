// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Telemetry
{
    /// <summary>
    /// The Telemetry data constants.
    /// </summary>
    public static class TelemetryDataConstants
    {
        // ******************** Execution ***********************
        public static string ParallelEnabledDuringExecution = "VS.TestRun.ParallelEnabled";

        // Total number of tests ran under one test request
        public static string TotalTestsRun = "VS.TestRun.TotalTestsRun";

        // Total time taken to complete one test run request
        public static string TimeTakenInSecForRun = "VS.TestRun.TimeTakenInSec";

        public static string DataCollectorsEnabled = "VS.TestRun.DataCollectorsEnabled";

        public static string RunState = "VS.TestRun.RunState";

        public static string NumberOfSourcesSentForRun = "VS.TestRun.NumberOfSources";

        public static string TargetDevice = "VS.TestRun.TargetDevice";

        public static string TargetFramework = "VS.TestRun.TargetFramework";

        public static string TargetPlatform = "VS.TestRun.TargetPlatform";

        public static string MaxCPUcount = "VS.TestRun.MaxCPUcount";

        public static string TestPlatformVersion = "VS.TestRun.TestPlatformVersion";

        public static string TargetOS = "VS.TestRun.TargetOS";

        public static string LoggerUsed = "VS.TestRun.LoggersUsed";

        public static string CommandLineSwitches = "VS.TestRun.CommandLineSwitches";

        // Adapter name will get appended. eg:- VS.TestRun.TotalTestsRanByAdapter.executor//cppunittestexecutor/v1
        // In case of parallel it will be sum of all tests ran by an adapter in different execution process
        public static string TotalTestsRanByAdapter = "VS.TestRun.TotalTestsRanByAdapter";

        // Adapter name will get appended. eg:- VS.TestRun.TimeTakenToRunTestsByAnAdapter.executor//cppunittestexecutor/v1
        // In case of parallel it will be sum of all time taken by an adapter to run tests in different execution process
        public static string TimeTakenToRunTestsByAnAdapter = "VS.TestRun.TimeTakenToRunTestsByAnAdapter";

        // Total number of adapter discovered on the machine.
        public static string NumberOfAdapterDiscoveredDuringExecution = "VS.TestRun.AdaptersDiscoveredCount";

        public static string NumberOfAdapterUsedToRunTests = "VS.TestRun.AdaptersUsedCount";

        // It will be the sum of the times taken by all adapter to run tests.
        // In case of parallel it can be more than total time taken to complete run request.
        public static string TimeTakenByAllAdaptersInSec = "VS.TestRun.TimeTakenByAllAdaptersInSec";

        // *********************Discovery****************************
        public static string TotalTestsDiscovered = "VS.TestDiscovery.TotalTestsDiscovered";

        public static string ParallelEnabledDuringDiscovery = "VS.TestDiscovery.ParallelEnabled";

        // All the times are in sec
        public static string TimeTakenInSecForDiscovery = "VS.TestDiscovery.TotalTimeTakenInSec";

        public static string TimeTakenToLoadAdaptersInSec = "VS.TestDiscovery.TimeTakenToLoadAdaptersInSec";

        // It will be the sum of the times taken by all adapter to discover tests.
        public static string TimeTakenInSecByAllAdapters = "VS.TestDiscovery.TimeTakenInSecByAllAdapters";

        // Adapter name will get appended. eg:- VS.TestDiscovery.TimeTaken.executor//cppunittestexecutor/v1
        public static string TimeTakenToDiscoverTestsByAnAdapter = "VS.TestDiscovery.TimeTaken";

        // Adapter name will get appended. eg:- VS.TestDiscovery.TotalTests.executor//cppunittestexecutor/v1
        public static string TotalTestsByAdapter = "VS.TestDiscovery.TotalTests";

        public static string DiscoveryState = "VS.TestDiscovery.DiscoveryState";

        public static string NumberOfSourcesSentForDiscovery = "VS.TestDiscovery.NumberOfSources";

        public static string NumberOfAdapterDiscoveredDuringDiscovery = "VS.TestDiscovery.AdaptersDiscoveredCount";

        public static string NumberOfAdapterUsedToDiscoverTests = "VS.TestDiscovery.AdaptersUsedCount";

        // **************Events Name **********************************
        public static string TestDiscoveryCompleteEvent = "vs/testplatform/testdiscoverysession";

        public static string TestExecutionCompleteEvent = "vs/testplatform/testrunsession";
    }
}
