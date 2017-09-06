// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Telemetry
{
    /// <summary>
    /// The unit test telemetry data constants.
    /// </summary>
    public static class UnitTestTelemetryDataConstants
    {
        // ******************** Execution ***********************
        public static string ParallelEnabled = "VS.UnitTest.TestRun.ParallelEnabled";

        // Total number of tests ran under one test request
        public static string TotalTestsRun = "VS.UnitTest.TestRun.TotalTestsRun";

        // Total time taken to complete one test run request
        public static string TimeTakenInSecForRun = "VS.UnitTest.TestRun.TimeTakenInSec";

        public static string PeakWorkingSetForRun = "VS.UnitTest.TestRun.PeakWorkingSet";

        public static string DataCollectorsEnabled = "VS.UnitTest.TestRun.DataCollectorsEnabled";

        public static string IsAppContainerMode = "VS.UnitTest.TestRun.IsAppContainerMode";

        public static string RunState = "VS.UnitTest.TestRun.RunState";

        public static string NumberOfSourcesSentForRun = "VS.UnitTest.TestRun.NumberOfSources";

        public static string TargetDevice = "VS.UnitTest.TestRun.TargetDevice";

        // Adapter name will get appended. eg:- VS.UnitTest.TestRun.TotalTestsRanByAdapter.executor//cppunittestexecutor/v1
        // In case of parallel it will be sum of all tests ran by an adapter in different execution process
        public static string TotalTestsRanByAdapter = "VS.UnitTest.TestRun.TotalTestsRanByAdapter";

        // Adapter name will get appended. eg:- VS.UnitTest.TestRun.TimeTakenToRunTestsByAnAdapter.executor//cppunittestexecutor/v1
        // In case of parallel it will be sum of all time taken by an adapter to run tests in different execution process
        public static string TimeTakenToRunTestsByAnAdapter = "VS.UnitTest.TestRun.TimeTakenToRunTestsByAnAdapter";

        // Total number of adapter discovered on the machine.
        public static string NumberOfAdapterDiscovered = "VS.UnitTest.TestRun.AdaptersDiscoveredCount";

        public static string NumberOfAdapterUsedToRunTests = "VS.UnitTest.TestRun.AdaptersUsedCount";

        // In case of parallel, it will be maximum of all time taken by different execution engine.
        public static string TimeTakenToStartExecutionEngineExe = "VS.UnitTest.TestRun.ExecutionEngineStartTime";

        // It will be the sum of the times taken by all adapter to run tests.
        // In case of parallel it can be more than total time taken to complete run request.
        public static string TimeTakenByAllAdaptersInSec = "VS.UnitTest.TestRun.TimeTakenByAllAdaptersInSec";


        // *********************Discovery****************************
        public static string TotalTestsDiscovered = "VS.UnitTest.TestDiscovery.TotalTests";

        // All the times are in sec
        public static string TimeTakenInSecForDiscovery = "VS.UnitTest.TestDiscovery.TotalTimeTakenInSec";

        public static string TimeTakenToLoadAdaptersInSec = "VS.UnitTest.TestDiscovery.TimeTakenToLoadAdapters";

        public static string TimeTakenInSecToStartDiscoveryEngine = "VS.UnitTest.TestDiscovery.TimeTakenToStartDiscoveryEngine";

        // It will be the sum of the times taken by all adapter to discover tests.
        public static string TimeTakenInSecByAllAdapters = "VS.UnitTest.TestDiscovery.TimeTakenByAllAdapters";

        // Adapter name will get appended. eg:- VS.UnitTest.TestDiscovery.TimeTaken.executor//cppunittestexecutor/v1
        public static string TimeTakenToDiscoverTestsByAnAdapter = "VS.UnitTest.TestDiscovery.TimeTaken";

        // Adapter name will get appended. eg:- VS.UnitTest.TestDiscovery.TotalTests.executor//cppunittestexecutor/v1
        public static string TotalTestsByAdapter = "VS.UnitTest.TestDiscovery.TotalTests";

        public static string DiscoveryState = "VS.UnitTest.TestDiscovery.DiscoveryState";

        public static string NumberOfSourcesSentForDiscovery = "VS.UnitTest.TestDiscovery.NumberOfSources";

        public static string NumberOfAdapterDiscoveredInTheMachine = "VS.UnitTest.TestDiscovery.AdaptersDiscoveredCount";

        public static string NumberOfAdapterUsedToDiscoverTests = "VS.UnitTest.TestDiscovery.AdaptersUsedCount";



        // **************Events Name **********************************
        public static string TestDiscoveryCompleteEvent = "vs/unittest/testdiscoverysession";

        public static string TestExecutionCompleteEvent = "vs/unittest/testexecutionsession";
    }
}
