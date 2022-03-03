// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.Common.Telemetry;

/// <summary>
/// The Telemetry data constants.
/// </summary>
internal static class TelemetryDataConstants
{
    // ******************** Execution ***********************
    internal static readonly string ParallelEnabledDuringExecution = "VS.TestRun.ParallelEnabled";

    // Total number of tests ran under one test request
    internal static readonly string TotalTestsRun = "VS.TestRun.TotalTests";

    // Total time taken to complete one test run request
    internal static readonly string TimeTakenInSecForRun = "VS.TestRun.TimeTakenInSec";

    internal static readonly string TestSettingsUsed = "VS.TestRun.IsTestSettingsUsed";

    internal static readonly string DisableAppDomain = "VS.TestRun.DisableAppDomain";

    // All data related to legacy settings nodes will be prefixed with this.
    internal static readonly string LegacySettingPrefix = "VS.TestRun.LegacySettings";

    internal static readonly string DataCollectorsEnabled = "VS.TestRun.DataCollectorsEnabled";

    internal static readonly string InvokedDataCollectors = "VS.TestRun.InvokedDataCollectors";

    internal static readonly string DataCollectorsCorProfiler = "VS.TestPlatform.DataCollector.CorProfiler";

    internal static readonly string DataCollectorsCoreClrProfiler = "VS.TestPlatform.DataCollector.CoreClrProfiler";

    internal static readonly string RunState = "VS.TestRun.RunState";

    internal static readonly string NumberOfSourcesSentForRun = "VS.TestRun.NumberOfSources";

    internal static readonly string TargetDevice = "VS.TestRun.TargetDevice";

    internal static readonly string TargetFramework = "VS.TestRun.TargetFramework";

    internal static readonly string TargetPlatform = "VS.TestRun.TargetPlatform";

    internal static readonly string MaxCPUcount = "VS.TestRun.MaxCPUcount";

    internal static readonly string TestPlatformVersion = "VS.TestRun.TestPlatformVersion";

    internal static readonly string TargetOS = "VS.TestRun.TargetOS";

    internal static readonly string LoggerUsed = "VS.TestRun.LoggersUsed";

    internal static readonly string CommandLineSwitches = "VS.TestRun.CommandLineSwitches";

    // Adapter name will get appended. eg:- VS.TestRun.TotalTestsRun.executor//cppunittestexecutor/v1
    // In case of parallel it will be sum of all tests ran by an adapter in different execution process
    internal static readonly string TotalTestsRanByAdapter = "VS.TestRun.TotalTestsRun";

    // Adapter name will get appended. eg:- VS.TestRun.TimeTakenToRun.executor//cppunittestexecutor/v1
    // In case of parallel it will be sum of all time taken by an adapter to run tests in different execution process
    internal static readonly string TimeTakenToRunTestsByAnAdapter = "VS.TestRun.TimeTakenToRun";

    // Reports details for MSTestV1. Reports just the count when TPv1 is used.
    // Reports legacy when the legacy runner TMI / TPv0 is used.
    // Adds an extension when an extension (tips) is used.
    // eg:- VS.TestRun.TotalTestsRunByMSTestv1 - a unit test using TPv1 and MSTestV1
    // eg:- VS.TestRun.TotalTestsRunByMSTestv1.legacy - a unit test using TPv0 and MSTestV1
    // eg:- VS.TestRun.TotalTestsRunByMSTestv1.legacy.extension.codedui - a coded ui test using TPv0 and MSTestV1
    // Counts in this metric are not subtracted from the TotalTestsRanByAdapter. This metrics just
    // provides more insight into what was actually executed.
    internal static readonly string TotalTestsRunByMSTestv1 = "VS.TestRun.TotalTestsRunByMSTestv1";

    // Total number of adapter discovered on the machine.
    internal static readonly string NumberOfAdapterDiscoveredDuringExecution = "VS.TestRun.AdaptersDiscoveredCount";

    internal static readonly string NumberOfAdapterUsedToRunTests = "VS.TestRun.AdaptersUsedCount";

    // It will be the sum of the times taken by all adapter to run tests.
    // In case of parallel it can be more than total time taken to complete run request.
    internal static readonly string TimeTakenByAllAdaptersInSec = "VS.TestRun.TimeTakenByAllAdapters";

    // *********************Discovery****************************
    internal static readonly string TotalTestsDiscovered = "VS.TestDiscovery.TotalTests";

    internal static readonly string ParallelEnabledDuringDiscovery = "VS.TestDiscovery.ParallelEnabled";

    // All the times are in sec
    internal static readonly string TimeTakenInSecForDiscovery = "VS.TestDiscovery.TotalTimeTakenInSec";

    internal static readonly string TimeTakenToLoadAdaptersInSec = "VS.TestDiscovery.TimeTakenToLoadAdaptersInSec";

    // It will be the sum of the times taken by all adapter to discover tests.
    internal static readonly string TimeTakenInSecByAllAdapters = "VS.TestDiscovery.TimeTakenInSecByAllAdapters";

    // Adapter name will get appended. eg:- VS.TestDiscovery.TimeTakenAdapter.executor//cppunittestexecutor/v1
    internal static readonly string TimeTakenToDiscoverTestsByAnAdapter = "VS.TestDiscovery.TimeTakenAdapter";

    // Adapter name will get appended. eg:- VS.TestDiscovery.TotalTestsDiscovered.executor//cppunittestexecutor/v1
    internal static readonly string TotalTestsByAdapter = "VS.TestDiscovery.TotalTestsDiscovered";

    internal static readonly string DiscoveryState = "VS.TestDiscovery.DiscoveryState";

    internal static readonly string NumberOfSourcesSentForDiscovery = "VS.TestDiscovery.NumberOfSources";

    internal static readonly string NumberOfAdapterDiscoveredDuringDiscovery = "VS.TestDiscovery.AdaptersDiscoveredCount";

    internal static readonly string NumberOfAdapterUsedToDiscoverTests = "VS.TestDiscovery.AdaptersUsedCount";

    // *********************Attachments Processing****************************
    internal static readonly string NumberOfAttachmentsSentForProcessing = "VS.AttachmentsProcessing.InitialAttachmentsCount";

    internal static readonly string NumberOfAttachmentsAfterProcessing = "VS.AttachmentsProcessing.FinalAttachmentsCount";

    internal static readonly string TimeTakenInSecForAttachmentsProcessing = "VS.AttachmentsProcessing.TotalTimeTakenInSec";

    internal static readonly string AttachmentsProcessingState = "VS.AttachmentsProcessing.State";

    // *********************Test Sessions****************************
    internal static readonly string ParallelEnabledDuringStartTestSession = "VS.TestSession.ParallelEnabled";

    internal static readonly string TestSessionId = "VS.TestSession.Id";

    internal static readonly string TestSessionSpawnedTesthostCount = "VS.TestSession.SpawnedTesthostCount";

    internal static readonly string TestSessionTesthostSpawnTimeInSec = "VS.TestSession.TesthostSpawnTimeInSec";

    internal static readonly string TestSessionState = "VS.TestSession.State";

    internal static readonly string TestSessionTotalSessionTimeInSec = "VS.TestSession.TotalSessionTimeInSec";

    // **************Events Name **********************************
    internal static readonly string TestDiscoveryCompleteEvent = "vs/testplatform/testdiscoverysession";

    internal static readonly string TestExecutionCompleteEvent = "vs/testplatform/testrunsession";

    internal static readonly string TestAttachmentsProcessingCompleteEvent = "vs/testplatform/testattachmentsprocessingsession";

    internal static readonly string StartTestSessionCompleteEvent = "vs/testplatform/starttestsession";

    internal static readonly string StopTestSessionCompleteEvent = "vs/testplatform/stoptestsession";
}
