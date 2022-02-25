// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.Common.Telemetry;

/// <summary>
/// The Telemetry data constants.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible", Justification = "Part of the public API.")]
public static class TelemetryDataConstants
{
    // ******************** Execution ***********************
    public const string ParallelEnabledDuringExecution = "VS.TestRun.ParallelEnabled";

    // Total number of tests ran under one test request
    public const string TotalTestsRun = "VS.TestRun.TotalTests";

    // Total time taken to complete one test run request
    public const string TimeTakenInSecForRun = "VS.TestRun.TimeTakenInSec";

    public const string TestSettingsUsed = "VS.TestRun.IsTestSettingsUsed";

    public const string DisableAppDomain = "VS.TestRun.DisableAppDomain";

    // All data related to legacy settings nodes will be prefixed with this.
    public const string LegacySettingPrefix = "VS.TestRun.LegacySettings";

    public const string DataCollectorsEnabled = "VS.TestRun.DataCollectorsEnabled";

    internal const string InvokedDataCollectors = "VS.TestRun.InvokedDataCollectors";

    internal const string DataCollectorsCorProfiler = "VS.TestPlatform.DataCollector.CorProfiler";

    internal const string DataCollectorsCoreClrProfiler = "VS.TestPlatform.DataCollector.CoreClrProfiler";

    public const string RunState = "VS.TestRun.RunState";

    public const string NumberOfSourcesSentForRun = "VS.TestRun.NumberOfSources";

    public const string TargetDevice = "VS.TestRun.TargetDevice";

    public const string TargetFramework = "VS.TestRun.TargetFramework";

    public const string TargetPlatform = "VS.TestRun.TargetPlatform";

    public const string MaxCPUcount = "VS.TestRun.MaxCPUcount";

    public const string TestPlatformVersion = "VS.TestRun.TestPlatformVersion";

    public const string TargetOS = "VS.TestRun.TargetOS";

    public const string LoggerUsed = "VS.TestRun.LoggersUsed";

    public const string CommandLineSwitches = "VS.TestRun.CommandLineSwitches";

    // Adapter name will get appended. eg:- VS.TestRun.TotalTestsRun.executor//cppunittestexecutor/v1
    // In case of parallel it will be sum of all tests ran by an adapter in different execution process
    public const string TotalTestsRanByAdapter = "VS.TestRun.TotalTestsRun";

    // Adapter name will get appended. eg:- VS.TestRun.TimeTakenToRun.executor//cppunittestexecutor/v1
    // In case of parallel it will be sum of all time taken by an adapter to run tests in different execution process
    public const string TimeTakenToRunTestsByAnAdapter = "VS.TestRun.TimeTakenToRun";

    // Reports details for MSTestV1. Reports just the count when TPv1 is used.
    // Reports legacy when the legacy runner TMI / TPv0 is used.
    // Adds an extension when an extension (tips) is used.
    // eg:- VS.TestRun.TotalTestsRunByMSTestv1 - a unit test using TPv1 and MSTestV1
    // eg:- VS.TestRun.TotalTestsRunByMSTestv1.legacy - a unit test using TPv0 and MSTestV1
    // eg:- VS.TestRun.TotalTestsRunByMSTestv1.legacy.extension.codedui - a coded ui test using TPv0 and MSTestV1
    // Counts in this metric are not subtracted from the TotalTestsRanByAdapter. This metrics just
    // provides more insight into what was actually executed.
    public const string TotalTestsRunByMSTestv1 = "VS.TestRun.TotalTestsRunByMSTestv1";

    // Total number of adapter discovered on the machine.
    public const string NumberOfAdapterDiscoveredDuringExecution = "VS.TestRun.AdaptersDiscoveredCount";

    public const string NumberOfAdapterUsedToRunTests = "VS.TestRun.AdaptersUsedCount";

    // It will be the sum of the times taken by all adapter to run tests.
    // In case of parallel it can be more than total time taken to complete run request.
    public const string TimeTakenByAllAdaptersInSec = "VS.TestRun.TimeTakenByAllAdapters";

    // *********************Discovery****************************
    public const string TotalTestsDiscovered = "VS.TestDiscovery.TotalTests";

    public const string ParallelEnabledDuringDiscovery = "VS.TestDiscovery.ParallelEnabled";

    // All the times are in sec
    public const string TimeTakenInSecForDiscovery = "VS.TestDiscovery.TotalTimeTakenInSec";

    public const string TimeTakenToLoadAdaptersInSec = "VS.TestDiscovery.TimeTakenToLoadAdaptersInSec";

    // It will be the sum of the times taken by all adapter to discover tests.
    public const string TimeTakenInSecByAllAdapters = "VS.TestDiscovery.TimeTakenInSecByAllAdapters";

    // Adapter name will get appended. eg:- VS.TestDiscovery.TimeTakenAdapter.executor//cppunittestexecutor/v1
    public const string TimeTakenToDiscoverTestsByAnAdapter = "VS.TestDiscovery.TimeTakenAdapter";

    // Adapter name will get appended. eg:- VS.TestDiscovery.TotalTestsDiscovered.executor//cppunittestexecutor/v1
    public const string TotalTestsByAdapter = "VS.TestDiscovery.TotalTestsDiscovered";

    public const string DiscoveryState = "VS.TestDiscovery.DiscoveryState";

    public const string NumberOfSourcesSentForDiscovery = "VS.TestDiscovery.NumberOfSources";

    public const string NumberOfAdapterDiscoveredDuringDiscovery = "VS.TestDiscovery.AdaptersDiscoveredCount";

    public const string NumberOfAdapterUsedToDiscoverTests = "VS.TestDiscovery.AdaptersUsedCount";

    // *********************Attachments Processing****************************
    public const string NumberOfAttachmentsSentForProcessing = "VS.AttachmentsProcessing.InitialAttachmentsCount";

    public const string NumberOfAttachmentsAfterProcessing = "VS.AttachmentsProcessing.FinalAttachmentsCount";

    public const string TimeTakenInSecForAttachmentsProcessing = "VS.AttachmentsProcessing.TotalTimeTakenInSec";

    public const string AttachmentsProcessingState = "VS.AttachmentsProcessing.State";

    // *********************Test Sessions****************************
    public const string ParallelEnabledDuringStartTestSession = "VS.TestSession.ParallelEnabled";

    public const string TestSessionId = "VS.TestSession.Id";

    public const string TestSessionSpawnedTesthostCount = "VS.TestSession.SpawnedTesthostCount";

    public const string TestSessionTesthostSpawnTimeInSec = "VS.TestSession.TesthostSpawnTimeInSec";

    public const string TestSessionState = "VS.TestSession.State";

    public const string TestSessionTotalSessionTimeInSec = "VS.TestSession.TotalSessionTimeInSec";

    // **************Events Name **********************************
    public const string TestDiscoveryCompleteEvent = "vs/testplatform/testdiscoverysession";

    public const string TestExecutionCompleteEvent = "vs/testplatform/testrunsession";

    public const string TestAttachmentsProcessingCompleteEvent = "vs/testplatform/testattachmentsprocessingsession";

    public const string StartTestSessionCompleteEvent = "vs/testplatform/starttestsession";

    public const string StopTestSessionCompleteEvent = "vs/testplatform/stoptestsession";
}
