# VSTest: Newtonsoft.Json → System.Text.Json Migration

## Executive Summary

VSTest currently depends on **Newtonsoft.Json 13.0.3** for all JSON serialization between test runner processes. We propose migrating to **System.Text.Json**, which ships with the .NET runtime.

## Why Migrate?

| Benefit | Impact |
|---------|--------|
| **No external dependency** | Removes a NuGet package from the critical path |
| **Better performance** | System.Text.Json is 2-5x faster for typical payloads |
| **AOT compatibility** | Required for Native AOT test host scenarios |
| **Framework alignment** | Aligns with .NET team direction; Newtonsoft.Json is maintenance-mode |

## Scope

- **11 source files** to change (all in `CommunicationUtilities` + `TestHostProvider`)
- **13 test files** with Newtonsoft references
- **5 custom converters** + 2 contract resolvers to rewrite
- **7 protocol versions** (0-7) must produce identical wire format

## Risk Assessment

| Risk | Severity | Mitigation |
|------|----------|------------|
| Wire format regression | **High** | Before/after comparison tests for every message type |
| Behavioral differences (case sensitivity, trailing commas) | Medium | Configure STJ to match Newtonsoft defaults |
| `Message.Payload` is `JToken` (public API) | **High** | Change to `JsonElement?`; assess breaking change impact |
| Old test hosts on different protocol versions | Medium | V1 converter rewrite preserves exact format |

## Approach

1. **Write comparison tests first** — capture current serialization output for all message types
2. **Migrate core** (`JsonDataSerializer`) with STJ equivalents behind feature flag
3. **Rewrite converters** — TestCase, TestResult, TestObject, TestRunStatistics
4. **Replace JObject/JToken** — in DotnetTestHostManager (isolated change)
5. **Remove package references** — final cleanup

## Timeline Estimate

This is a **medium-large** change. The serialization core is centralized (good), but the custom converters are complex and protocol-version-sensitive.

## Decision Needed

- **Breaking change policy**: `Message.Payload` changing from `JToken?` to `JsonElement?` affects any external code holding a `Message` reference. Is this acceptable?
