# System.Text.Json Migration: Change Log

*This document tracks every file modified during the migration.*

## Status: 🟡 In Progress

---

## Changes Made

### Phase 0: Golden Baseline Tests
*(Capture current serialization output before any changes)*

| File | Status | Notes |
|------|--------|-------|
| `test/.../Serialization/SerializationGoldenTests.cs` | ✅ Done | 16 tests, all passing against Newtonsoft baseline |

### Phase 1: Core Serializer Migration

| File | Status | Notes |
|------|--------|-------|
| `src/.../JsonDataSerializer.cs` | ✅ Done | Full rewrite: JsonSerializer + JsonSerializerOptions |
| `src/.../Messages/Message.cs` | ✅ Done | `JToken? Payload` → `JsonElement? Payload` |
| `src/.../Messages/VersionedMessage.cs` | ✅ Done | No changes needed (inherits from Message) |

### Phase 2: Converter Rewrites

| File | Status | Notes |
|------|--------|-------|
| `src/.../Serialization/TestCaseConverter.cs` | ✅ Done | `JsonConverter<TestCase>` with Utf8JsonReader/Writer |
| `src/.../Serialization/TestResultConverter.cs` | ✅ Done | `JsonConverter<TestResult>` with Utf8JsonReader/Writer |
| `src/.../Serialization/TestObjectConverter.cs` | ✅ Done | `JsonConverter<List<KVP<TestProperty,object>>>`, removed v7 |
| `src/.../Serialization/TestRunStatisticsConverter.cs` | ✅ Done | `JsonConverter<ITestRunStatistics>` |
| `src/.../Serialization/DefaultTestPlatformContractResolver.cs` | ✅ Done | Gutted, replaced by Options config |
| `src/.../Serialization/TestPlatformContractResolver1.cs` | ✅ Done | Gutted, replaced by Options config |

### Phase 3: Model Changes

| File | Status | Notes |
|------|--------|-------|
| `src/.../ObjectModel/TestRunCriteriaWithTests.cs` | ✅ Done | `using System.Text.Json.Serialization` |
| `src/.../ObjectModel/TestRunCriteriaWithSources.cs` | ✅ Done | `using System.Text.Json.Serialization` |

### Phase 4: Isolated Changes

| File | Status | Notes |
|------|--------|-------|
| `src/.../DotnetTestHostManager.cs` | ✅ Done | JObject → JsonDocument for deps.json |
| `src/.../PublicAPI/PublicAPI.Shipped.txt` | ✅ Done | Updated Payload type signature |

### Phase 5: Package References

| File | Status | Notes |
|------|--------|-------|
| `eng/Versions.props` | ✅ Done | Added `SystemTextJsonVersion` = 8.0.5 |
| `src/.../CommunicationUtilities.csproj` | ✅ Done | Newtonsoft → System.Text.Json |
| `src/.../TestHostProvider.csproj` | ✅ Done | Newtonsoft → System.Text.Json |
| `src/package/.../CLI.csproj` | ✅ Done | Package + Copy target updated |
| `src/package/.../Portable.csproj` | ✅ Done | Package reference updated |
| `src/package/.../TestPlatform.csproj` | ✅ Done | Package reference updated |

### Phase 6: Test Migration

| File | Status | Notes |
|------|--------|-------|
| `test/.../JsonDataSerializerTests.cs` | 🟡 In progress | Agent working on migration |
| `test/.../Serialization/TestCaseSerializationTests.cs` | 🟡 In progress | |
| `test/.../Serialization/TestResultSerializationTests.cs` | 🟡 In progress | |
| `test/.../DataCollectionRequestHandlerTests.cs` | 🟡 In progress | |
| `test/.../DataCollectionTestCaseEventHandlerTests.cs` | 🟡 In progress | |
| `test/.../DataCollectionTestCaseEventSenderTests.cs` | 🟡 In progress | |
| `test/.../DesignModeClientTests.cs` | 🟡 In progress | |
| `test/.../VsTestConsoleRequestSenderTests.cs` | 🟡 In progress | |
| `test/.../IntegrationTestBuild.cs` | 🟡 In progress | |

### Not Yet Addressed

| File | Status | Notes |
|------|--------|-------|
| `samples/Microsoft.TestPlatform.Protocol/` | ⬜ Pending | Separate sample, has own JsonDataSerializer copy |
| `playground/AdapterUtilitiesPlayground/` | ⬜ Pending | Playground project |
| `test/TestAssets/NewtonSoftDependency/` | ⬜ Pending | Test asset specifically for Newtonsoft dependency testing |
| `eng/Versions.props` NewtonsoftJsonVersion removal | ⬜ Pending | Keep until all refs removed |

---

## Build Status

- `dotnet build src/Microsoft.TestPlatform.CommunicationUtilities` → ✅ 0 errors, 0 warnings
- Full solution build → 🟡 Test projects still being migrated
