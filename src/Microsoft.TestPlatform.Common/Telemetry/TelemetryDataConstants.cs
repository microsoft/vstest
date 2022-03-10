// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Telemetry;

/// <summary>
/// The Telemetry data constants.
/// </summary>
public static class TelemetryDataConstants
{
    // ******************** Execution ***********************
    public static string ParallelEnabledDuringExecution = "VS.TestRun.ParallelEnabled";

    // Total number of tests ran under one test request
    public static string TotalTestsRun = "VS.TestRun.TotalTests";

    // Total time taken to complete one test run request
    public static string TimeTakenInSecForRun = "VS.TestRun.TimeTakenInSec";

    public static string TestSettingsUsed = "VS.TestRun.IsTestSettingsUsed";

    public static string DisableAppDomain = "VS.TestRun.DisableAppDomain";

    // All data related to legacy settings nodes will be prefixed with this.
    public static string LegacySettingPrefix = "VS.TestRun.LegacySettings";

    public static string DataCollectorsEnabled = "VS.TestRun.DataCollectorsEnabled";

    internal const string InvokedDataCollectors = "VS.TestRun.InvokedDataCollectors";

    internal const string DataCollectorsCorProfiler = "VS.TestPlatform.DataCollector.CorProfiler";

    internal const string DataCollectorsCoreClrProfiler = "VS.TestPlatform.DataCollector.CoreClrProfiler";

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

    // Adapter name will get appended. eg:- VS.TestRun.TotalTestsRun.executor//cppunittestexecutor/v1
    // In case of parallel it will be sum of all tests ran by an adapter in different execution process
    public static string TotalTestsRanByAdapter = "VS.TestRun.TotalTestsRun";

    // Adapter name will get appended. eg:- VS.TestRun.TimeTakenToRun.executor//cppunittestexecutor/v1
    // In case of parallel it will be sum of all time taken by an adapter to run tests in different execution process
    public static string TimeTakenToRunTestsByAnAdapter = "VS.TestRun.TimeTakenToRun";

    // Reports details for MSTestV1. Reports just the count when TPv1 is used.
    // Reports legacy when the legacy runner TMI / TPv0 is used.
    // Adds an extension when an extension (tips) is used.
    // eg:- VS.TestRun.TotalTestsRunByMSTestv1 - a unit test using TPv1 and MSTestV1
    // eg:- VS.TestRun.TotalTestsRunByMSTestv1.legacy - a unit test using TPv0 and MSTestV1
    // eg:- VS.TestRun.TotalTestsRunByMSTestv1.legacy.extension.codedui - a coded ui test using TPv0 and MSTestV1
    // Counts in this metric are not subtracted from the TotalTestsRanByAdapter. This metrics just
    // provides more insight into what was actually executed.
    public static string TotalTestsRunByMSTestv1 = "VS.TestRun.TotalTestsRunByMSTestv1";

    // Total number of adapter discovered on the machine.
    public static string NumberOfAdapterDiscoveredDuringExecution = "VS.TestRun.AdaptersDiscoveredCount";

    public static string NumberOfAdapterUsedToRunTests = "VS.TestRun.AdaptersUsedCount";

    // It will be the sum of the times taken by all adapter to run tests.
    // In case of parallel it can be more than total time taken to complete run request.
    public static string TimeTakenByAllAdaptersInSec = "VS.TestRun.TimeTakenByAllAdapters";

    // *********************Discovery****************************
    public static string TotalTestsDiscovered = "VS.TestDiscovery.TotalTests";

    public static string ParallelEnabledDuringDiscovery = "VS.TestDiscovery.ParallelEnabled";

    // All the times are in sec
    public static string TimeTakenInSecForDiscovery = "VS.TestDiscovery.TotalTimeTakenInSec";

    public static string TimeTakenToLoadAdaptersInSec = "VS.TestDiscovery.TimeTakenToLoadAdaptersInSec";

    // It will be the sum of the times taken by all adapter to discover tests.
    public static string TimeTakenInSecByAllAdapters = "VS.TestDiscovery.TimeTakenInSecByAllAdapters";

    // Adapter name will get appended. eg:- VS.TestDiscovery.TimeTakenAdapter.executor//cppunittestexecutor/v1
    public static string TimeTakenToDiscoverTestsByAnAdapter = "VS.TestDiscovery.TimeTakenAdapter";

    // Adapter name will get appended. eg:- VS.TestDiscovery.TotalTestsDiscovered.executor//cppunittestexecutor/v1
    public static string TotalTestsByAdapter = "VS.TestDiscovery.TotalTestsDiscovered";

    public static string DiscoveryState = "VS.TestDiscovery.DiscoveryState";

    public static string NumberOfSourcesSentForDiscovery = "VS.TestDiscovery.NumberOfSources";

    public static string NumberOfAdapterDiscoveredDuringDiscovery = "VS.TestDiscovery.AdaptersDiscoveredCount";

    public static string NumberOfAdapterUsedToDiscoverTests = "VS.TestDiscovery.AdaptersUsedCount";

    // *********************Attachments Processing****************************
    public static string NumberOfAttachmentsSentForProcessing = "VS.AttachmentsProcessing.InitialAttachmentsCount";

    public static string NumberOfAttachmentsAfterProcessing = "VS.AttachmentsProcessing.FinalAttachmentsCount";

    public static string TimeTakenInSecForAttachmentsProcessing = "VS.AttachmentsProcessing.TotalTimeTakenInSec";

    public static string AttachmentsProcessingState = "VS.AttachmentsProcessing.State";

    // *********************Test Sessions****************************
    public static string ParallelEnabledDuringStartTestSession = "VS.TestSession.ParallelEnabled";

    public static string TestSessionId = "VS.TestSession.Id";

    public static string TestSessionSpawnedTesthostCount = "VS.TestSession.SpawnedTesthostCount";

    public static string TestSessionTesthostSpawnTimeInSec = "VS.TestSession.TesthostSpawnTimeInSec";

    public static string TestSessionState = "VS.TestSession.State";

    public static string TestSessionTotalSessionTimeInSec = "VS.TestSession.TotalSessionTimeInSec";

    // **************Events Name **********************************
    public static string TestDiscoveryCompleteEvent = "vs/testplatform/testdiscoverysession";

    public static string TestExecutionCompleteEvent = "vs/testplatform/testrunsession";

    public static string TestAttachmentsProcessingCompleteEvent = "vs/testplatform/testattachmentsprocessingsession";

    public static string StartTestSessionCompleteEvent = "vs/testplatform/starttestsession";

    public static string StopTestSessionCompleteEvent = "vs/testplatform/stoptestsession";
}
