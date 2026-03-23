# SettingsMigrator Report: What It Actually Does

## Summary

The SettingsMigrator reads 8 categories of settings from `.testsettings` files.
**6 out of 8 go into `<LegacySettings>` and force `<MSTest><ForcedLegacyMode>true</ForcedLegacyMode>`.**
Only 2 settings get truly native runsettings placement (+ 1 that goes top-level for web tests).

**Verdict: If your .testsettings uses Deployment, Scripts, Hosts, UnitTestRunConfig,
parallelTestCount, hostProcessPlatform, or testTimeout — the tool forces legacy mode,
making it essentially useless as a "migration" tool. It just wraps the old settings
in a `<LegacySettings>` envelope.**

---

## Property Migration Map

### ❌ Properties that REQUIRE Legacy Mode

These go into `<LegacySettings>` and set `<ForcedLegacyMode>true</ForcedLegacyMode>`:

| .testsettings XPath | Destination in .runsettings | Notes |
|---|---|---|
| `/TestSettings/Deployment` | `<LegacySettings><Deployment .../>` | Copied as-is |
| `/TestSettings/Scripts` | `<LegacySettings><Scripts .../>` | Copied with attributes (setupScript, etc.) |
| `/TestSettings/Execution` attr `parallelTestCount` | `<LegacySettings><Execution parallelTestCount="...">` | Attribute on Execution element |
| `/TestSettings/Execution` attr `hostProcessPlatform` | `<LegacySettings><Execution hostProcessPlatform="...">` | Attribute on Execution element |
| `/TestSettings/Execution/Timeouts` attr `testTimeout` | `<LegacySettings><Execution><Timeouts testTimeout="..."/>` | Child of Execution |
| `/TestSettings/Execution/Hosts` | `<LegacySettings><Execution><Hosts>...</Hosts>` | Copied as-is |
| `/TestSettings/Execution/TestTypeSpecific/UnitTestRunConfig` | `<LegacySettings><Execution><TestTypeSpecific><UnitTestRunConfig>...` | Wrapped in TestTypeSpecific |

If **any** of the above are present → the tool creates `<LegacySettings>` node and forces legacy mode.

### ✅ Properties that get NATIVE runsettings placement

| .testsettings XPath | Destination in .runsettings | Notes |
|---|---|---|
| `/TestSettings/Execution/Timeouts` attr `runTimeout` | `<RunConfiguration><TestSessionTimeout>value</TestSessionTimeout>` | Truly native, no legacy needed |
| `/TestSettings/Execution/AgentRule/DataCollectors/DataCollector` | `<DataCollectionRunSettings><DataCollectors><DataCollector .../>` | Each DataCollector copied as-is |

### ⚠️ Top-level (not legacy, but niche)

| .testsettings XPath | Destination in .runsettings | Notes |
|---|---|---|
| `/TestSettings/Execution/TestTypeSpecific/WebTestRunConfiguration` | Top-level `<WebTestRunConfiguration>` under `<RunSettings>` | Only relevant for web/load tests |

### 🚫 Unsupported (dropped with a warning)

| .testsettings XPath | Result |
|---|---|
| `Timeouts/@agentNotRespondingTimeout` | Dropped, warning printed |
| `Timeouts/@deploymentTimeout` | Dropped, warning printed |
| `Timeouts/@scriptTimeout` | Dropped, warning printed |

---

## Example Output Structure

For a typical .testsettings file that uses Deployment, Scripts, Timeouts, Hosts,
UnitTestRunConfig, and DataCollectors, the migrator produces:

```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <!-- ❌ LEGACY: forces old MSTest v1 runner -->
  <MSTest>
    <ForcedLegacyMode>true</ForcedLegacyMode>
  </MSTest>

  <!-- ❌ LEGACY: just a wrapper around the old settings -->
  <LegacySettings>
    <Deployment>
      <DeploymentItem filename="..." />
    </Deployment>
    <Scripts setupScript=".\setup.bat" cleanupScript=".\cleanup.bat" />
    <Execution parallelTestCount="2" hostProcessPlatform="MSIL">
      <Timeouts testTimeout="120000" />
      <Hosts>...</Hosts>
      <TestTypeSpecific>
        <UnitTestRunConfig>
          <AssemblyResolution>
            <TestDirectory ... />
          </AssemblyResolution>
        </UnitTestRunConfig>
      </TestTypeSpecific>
    </Execution>
  </LegacySettings>

  <!-- ✅ NATIVE: actually migrated to runsettings -->
  <RunConfiguration>
    <TestSessionTimeout>60000</TestSessionTimeout>
  </RunConfiguration>

  <!-- ✅ NATIVE: actually migrated to runsettings -->
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="Event Log" ... />
    </DataCollectors>
  </DataCollectionRunSettings>

  <!-- ⚠️ TOP-LEVEL: web test specific -->
  <WebTestRunConfiguration>
    <Browser>
      <Headers>
        <Header ... />
      </Headers>
    </Browser>
  </WebTestRunConfiguration>
</RunSettings>
```

---

## Bottom Line

| Question | Answer |
|---|---|
| Does the tool truly migrate settings to native runsettings? | **Mostly no.** Only `runTimeout` → `TestSessionTimeout` and DataCollectors get native placement. |
| Does it require legacy mode? | **Yes**, for 7 out of 10 property categories. Any .testsettings using Deployment, Scripts, Hosts, UnitTestRunConfig, parallelTestCount, hostProcessPlatform, or testTimeout will force `<ForcedLegacyMode>true</ForcedLegacyMode>`. |
| Is the tool useful if legacy mode makes it useless? | **No.** For any real-world .testsettings file (which almost always has at least Deployment or testTimeout), this tool just wraps old settings in `<LegacySettings>` XML — it doesn't actually migrate them to modern equivalents. |

---

## Existing Telemetry

There is telemetry already in place to measure usage of these features. Query your
telemetry backend for these metrics to decide if removal is safe.

### Telemetry Constants (`TelemetryDataConstants.cs`)

| Metric Name | What It Tracks |
|---|---|
| `VS.TestRun.IsTestSettingsUsed` | Whether a `.testsettings` file is active in the run |
| `VS.TestRun.LegacySettings.Elements` | Which LegacySettings child elements are present (Deployment, Scripts, etc.) |
| `VS.TestRun.LegacySettings.DeploymentAttributes` | Attributes on the `<Deployment>` node |
| `VS.TestRun.LegacySettings.ExecutionAttributes` | Attributes on the `<Execution>` node (parallelTestCount, hostProcessPlatform) |

### Collection Flow

1. **`TestRequestManager.LogTelemetryForLegacySettings()`** — called during `RunTests()` when telemetry is opted in
2. Calls **`InferRunSettingsHelper.TryGetLegacySettingElements()`** which inspects the runsettings XML for:
   - `/RunSettings/LegacySettings` child elements
   - `/RunSettings/LegacySettings/Execution/TestTypeSpecific/UnitTestRunConfig/AssemblyResolution`
   - `/RunSettings/LegacySettings/Execution/Timeouts`
   - `/RunSettings/LegacySettings/Execution/Hosts`
   - Deployment and Execution attributes
3. All metrics are published as part of the **`vs/testplatform/testrunsession`** event

### What to Query

| Question | Query |
|---|---|
| How many users still use .testsettings? | `VS.TestRun.IsTestSettingsUsed = true` |
| Which legacy features are in use? | `VS.TestRun.LegacySettings.Elements` values |
| Is Deployment used? | `VS.TestRun.LegacySettings.DeploymentAttributes` is non-empty |
| Is parallelTestCount/hostProcessPlatform used? | `VS.TestRun.LegacySettings.ExecutionAttributes` is non-empty |
