# System.Text.Json Migration: Detailed Technical Plan

## Architecture Overview

All JSON serialization in VSTest flows through a single hub:

```
JsonDataSerializer (singleton)
├── PayloadSerializerV1  (protocol 0, 1, 3)
│   └── TestPlatformContractResolver1
│       ├── TestCaseConverter (v1 format)
│       ├── TestResultConverter (v1 format)
│       ├── TestObjectConverter
│       └── TestRunStatisticsConverter
├── PayloadSerializerV2  (protocol 2, 4, 5, 6, 7)
│   └── DefaultTestPlatformContractResolver
│       ├── TestObjectConverter
│       └── TestRunStatisticsConverter
├── FastSerializer (perf-optimized, protocol v2+ only)
└── Serializer (generic, header-only)
```

## Key Newtonsoft Settings to Replicate

```csharp
// Current Newtonsoft settings (JsonDataSerializer.cs lines 37-60)
DateFormatHandling    = IsoDateFormat
DateParseHandling     = DateTimeOffset
DateTimeZoneHandling  = Utc
TypeNameHandling      = None
ReferenceLoopHandling = Ignore
MissingMemberHandling = Ignore
NullValueHandling     = Include
MaxDepth              = 64
DateFormatString      = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK"
FloatFormatHandling   = String  // ← floats serialized as strings!
Culture               = InvariantCulture
```

### STJ Equivalent Configuration

```csharp
var options = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    MaxDepth = 64,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    ReferenceHandler = ReferenceHandler.IgnoreCycles,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    WriteIndented = false,
};
```

**⚠️ Key behavioral difference**: Newtonsoft's `FloatFormatHandling.String` serializes floats as `"1.5"` (string). STJ writes `1.5` (number). If any float/double values flow through the protocol, this needs a custom converter.

## File-by-File Migration Plan

### Tier 1: Core Serializer (must be done first)

| File | Changes | Complexity |
|------|---------|------------|
| `JsonDataSerializer.cs` | Replace all `JsonSerializer`/`JsonSerializerSettings` with `JsonSerializerOptions`. Replace `JToken` deserialization with `JsonElement`/`JsonDocument`. Rewrite `Serialize<T>` and `Deserialize<T>` methods. | **High** |
| `Message.cs` | Change `JToken? Payload` → `JsonElement? Payload`. Update `ToString()`. | **Medium** (public API break) |
| `VersionedMessage.cs` | Inherits from `Message`, minimal changes | **Low** |

### Tier 2: Contract Resolvers → Custom Converters

STJ has no `ContractResolver` concept. Instead, register custom `JsonConverter<T>` instances on `JsonSerializerOptions.Converters`.

| File | Newtonsoft Concept | STJ Replacement |
|------|-------------------|-----------------|
| `DefaultTestPlatformContractResolver.cs` | Maps `List<KVP<TestProperty,object>>` → `TestObjectConverter` and `ITestRunStatistics` → `TestRunStatisticsConverter` | Register converters on `JsonSerializerOptions.Converters` |
| `TestPlatformContractResolver1.cs` | Adds `TestCaseConverter` + `TestResultConverter` for v1 | Separate `JsonSerializerOptions` instance with additional converters |

### Tier 3: Custom Converters (complete rewrite)

| Converter | Key Challenge |
|-----------|--------------|
| `TestCaseConverter` | Uses `JObject.Load` + `JToken` navigation. Must rewrite with `Utf8JsonReader`/`JsonDocument`. Write side uses `JsonWriter` → `Utf8JsonWriter`. |
| `TestResultConverter` | Same as TestCase but more complex (nested TestCase, Attachments, Messages). |
| `TestObjectConverter` | Read-only converter. Uses `JArray.Load` + `JToken.ToObject`. |
| `TestRunStatisticsConverter` | Simple interface→concrete mapping. STJ needs `JsonConverter<ITestRunStatistics>`. |

### Tier 4: Isolated Changes

| File | Change |
|------|--------|
| `DotnetTestHostManager.cs` | Replace `JObject`/`JToken` with `JsonDocument`/`JsonElement` for deps.json parsing |
| `TestRunCriteriaWithTests.cs` | Replace `[JsonConstructor]` (Newtonsoft) with `[JsonConstructor]` (STJ namespace) |
| `TestRunCriteriaWithSources.cs` | Same as above |
| `AfterTestRunEndResult.cs` | Remove Newtonsoft constructor handling comment; verify STJ ctor behavior |

### Tier 5: Package References

| Project | Action |
|---------|--------|
| `CommunicationUtilities.csproj` | Remove `Newtonsoft.Json` PackageReference |
| `TestHostProvider.csproj` | Remove `Newtonsoft.Json` PackageReference |
| `Microsoft.TestPlatform.csproj` | Remove packaging reference |
| `Microsoft.TestPlatform.Portable.csproj` | Remove packaging reference |
| `Microsoft.TestPlatform.CLI.csproj` | Remove packaging reference + Copy task |
| `eng/Versions.props` | Remove `NewtonsoftJsonVersion` property |

## Wire Format Compatibility

### V1 TestCase Format (protocols 0, 1, 3)
```json
{
  "Properties": [
    { "Key": {"Id":"TestCase.FullyQualifiedName",...}, "Value": "MyTest" },
    { "Key": {"Id":"TestCase.ExecutorUri",...}, "Value": "executor://mstest" },
    ...
  ]
}
```

### V2 TestCase Format (protocols 2, 4, 5, 6, 7)
```json
{
  "Id": "guid",
  "FullyQualifiedName": "MyTest",
  "DisplayName": "MyTest",
  "Source": "test.dll",
  "ExecutorUri": "executor://mstest",
  "CodeFilePath": null,
  "LineNumber": -1,
  "Properties": [
    { "Key": {"Id":"TestObject.Traits",...}, "Value": [...] }
  ]
}
```

**Both formats must be preserved exactly.**

## Comparison Test Strategy

Create `SerializationComparisonTests.cs` that:

1. Defines a set of representative test objects (TestCase, TestResult, etc.)
2. For each protocol version (1-7):
   - Serializes with current Newtonsoft implementation
   - Captures the JSON as a "golden" baseline
   - After migration, serializes with STJ
   - Compares character-by-character (or normalized JSON comparison)
3. Tests are parameterized by protocol version for easy extension
4. Designed to be reusable as performance benchmarks (data setup is separate from assertion)

## Potential Blockers

1. **`Message.Payload` as `JToken`** — This is a **shipped public API** (`PublicAPI.Shipped.txt` declares `Message.Payload.get -> Newtonsoft.Json.Linq.JToken?`). Changing it to `JsonElement?` is a breaking change. However, most consumers go through `JsonDataSerializer.DeserializePayload<T>()` which abstracts the type. The `PublicAPI.Unshipped.txt` must declare the new signature and the old one must be marked removed.
2. **`FloatFormatHandling.String`** — If any protocol messages carry float/double values, the wire format will differ (`"1.5"` vs `1.5`).
3. **`DateParseHandling.DateTimeOffset`** — STJ handles dates differently. Need to verify `DateTimeOffset` serialization matches.
4. **Trait values** — The special character escaping in traits (e.g., `\\\\` sequences) must be preserved exactly.
5. **net462 + netstandard2.0 targets** — Must add `System.Text.Json` as a NuGet package (it's not built-in for these TFMs).
