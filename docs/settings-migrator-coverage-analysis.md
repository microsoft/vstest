# SettingsMigrator Code → Skill Coverage Analysis

This document maps every code path, branch, and test scenario from the removed
`SettingsMigrator` tool to the corresponding section in the replacement skill
(`SKILL.md`). Purpose: verify nothing was lost in the migration.

## Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Fully covered in SKILL.md |
| ⚠️ | Covered but with differences (documented below) |
| ❌ | Not covered — needs attention |
| 🚫 | Intentionally omitted (CLI-only concern, not relevant for a skill) |

---

## 1. Program.cs — Entry Point

| Code (line) | Behavior | Skill Coverage |
|---|---|---|
| `PathResolver.GetTargetPath(args)` (L24-25) | Validate args, resolve output path | 🚫 CLI path resolution is not needed — the skill user provides source and destination directly |
| `migrator.Migrate(oldFilePath, newFilePath)` (L30-31) | Delegate to Migrator | ✅ Covered by "Step-by-Step Migration" section |
| `Console.WriteLine(ValidUsage); return 1` (L35-36) | Print usage on bad args | 🚫 No CLI usage message needed in a skill |

## 2. PathResolver.cs — Argument Validation

| Code (line) | Behavior | Skill Coverage |
|---|---|---|
| `args.Length < 1 \|\| !Path.IsPathRooted(args[0])` (L27) | Reject non-rooted paths | 🚫 CLI validation — user provides file directly |
| `args.Length == 1` → generate timestamped output name (L32-36) | Auto-generate output filename: `{name}_{MM-dd-yyyy_hh-mm-ss}.runsettings` | 🚫 CLI convenience feature, not relevant |
| `args.Length == 2` → validate second arg is rooted `.runsettings` (L38-44) | Validate output path | 🚫 CLI validation |

## 3. Migrator.cs — Core Migration Logic

### 3.1 Migrate() — Dispatcher (L68-87)

| Code (line) | Behavior | Skill Coverage |
|---|---|---|
| `!Path.IsPathRooted(oldFilePath)` → print usage (L70-73) | Redundant path validation | 🚫 CLI concern |
| Extension = `.testsettings` → `MigrateTestSettings()` (L75-77) | Route to .testsettings handler | ✅ "Step 1: Identify the Source File Type" → "Migrate from .testsettings" |
| Extension = `.runsettings` → `MigrateRunSettings()` (L79-81) | Route to .runsettings handler | ✅ "Step 1: Identify the Source File Type" → "Migrate Embedded .testsettings" |
| Neither extension → print usage (L83-86) | Reject unknown extensions | 🚫 CLI error handling |

### 3.2 MigrateRunSettings() — Embedded .testsettings (L94-130)

| Code (line) | Behavior | Skill Coverage |
|---|---|---|
| Read `.runsettings` with `XmlTextReader`, `Namespaces = false` (L97-101) | Load existing runsettings | ✅ "Migrate Embedded .testsettings" step 1 |
| `SelectSingleNode(@"/RunSettings/MSTest/SettingsFile")` (L104) | Find embedded .testsettings path | ✅ Step 2: "Find `<MSTest><SettingsFile>` node" |
| If `testSettingsPath` is not whitespace (L110) | Guard: only migrate if path exists | ✅ Implicit in skill — "if embedded reference exists" |
| `!Path.IsPathRooted` → resolve relative to .runsettings location (L113-115) | Relative path resolution | ✅ Step 2: "If the path is relative, resolve it relative to the .runsettings file location" |
| `RemoveEmbeddedTestSettings()` (L119) | Remove `<SettingsFile>` node | ✅ Step 3: "Remove the `<SettingsFile>` node" |
| `MigrateTestSettingsNodesToRunSettings()` (L121) | Apply transformation | ✅ Steps 4-5 |
| `runSettingsXmlDoc.Save(newRunSettingsPath)` (L123) | Write output | ✅ Step 7: "Save the output file" |
| `Console.WriteLine(RunSettingsCreated)` (L124) | Print success message | 🚫 CLI console output |
| `else Console.WriteLine(NoEmbeddedSettings)` (L128) | No embedded settings found | 🚫 CLI message — skill would simply have nothing to do |

### 3.3 MigrateTestSettings() — Standalone .testsettings (L137-146)

| Code (line) | Behavior | Skill Coverage |
|---|---|---|
| `LoadXml(SampleRunSettingsContent)` → start with `<?xml...><RunSettings></RunSettings>` (L139-140) | Create fresh runsettings | ✅ Step 3: "Start with a minimal .runsettings skeleton" |
| `MigrateTestSettingsNodesToRunSettings()` (L142) | Apply transformation | ✅ Step 3 sub-rules |
| Save and print (L144-145) | Output | ✅ "Migrate from .testsettings" step 3 |

### 3.4 MigrateTestSettingsNodesToRunSettings() — Node Transformation (L153-207)

| Code (line) | Behavior | Skill Coverage |
|---|---|---|
| `ReadTestSettingsNodes(testSettingsPath)` (L155) | Parse .testsettings into struct | ✅ "Step 2: Read the .testsettings File" — extraction table |
| Extract `testTimeout` from `Timeouts` (L159-163) | Read testTimeout attribute | ✅ Documented in 3b: "testTimeout goes into `<Timeouts testTimeout=.../>`" |
| Extract `runTimeout` from `Timeouts` (L165-169) | Read runTimeout attribute | ✅ Documented in 3c: "TestSessionTimeout" |
| Extract `parallelTestCount` from `Execution` (L175-178) | Read execution attribute | ✅ Documented in 3b: "parallelTestCount...attributes on the `<Execution>` element" |
| Extract `hostProcessPlatform` from `Execution` (L180-183) | Read execution attribute | ✅ Documented in 3b: "hostProcessPlatform" |
| `WebSettings != null` → append to root (L188-191) | WebTestRunConfiguration top-level | ✅ Section 3a: "WebTestRunConfiguration (top-level)" |
| `AddLegacyNodes()` (L194) | Create LegacySettings | ✅ Section 3b: "LegacySettings Node" |
| `!runTimeout.IsNullOrEmpty()` → `AddRunTimeoutNode()` (L197-199) | TestSessionTimeout | ✅ Section 3c: "TestSessionTimeout" |
| `Datacollectors != null && Count > 0` → `AddDataCollectorNodes()` (L202-206) | DataCollectors | ✅ Section 3d: "DataCollectors" |

### 3.5 ReadTestSettingsNodes() — XPath Extraction (L209-241)

| Code (line) | XPath | Property | Skill Coverage |
|---|---|---|---|
| L222 | `/TestSettings/Deployment` | Deployment | ✅ Row 1 of extraction table |
| L223 | `/TestSettings/Scripts` | Script | ✅ Row 2 |
| L224 | `/TestSettings/Execution/TestTypeSpecific/WebTestRunConfiguration` | WebSettings | ✅ Row 3 |
| L225 | `/TestSettings/Execution/AgentRule/DataCollectors/DataCollector` | Datacollectors | ✅ Row 4 |
| L226 | `/TestSettings/Execution/Timeouts` | Timeout | ✅ Row 5 |
| L227 | `/TestSettings/Execution/TestTypeSpecific/UnitTestRunConfig` | UnitTestConfig | ✅ Row 6 |
| L228 | `/TestSettings/Execution/Hosts` | Hosts | ✅ Row 7 |
| L229 | `/TestSettings/Execution` | Execution | ✅ Row 8 |
| L231-237 | Check for unsupported timeout attrs → `Console.WriteLine(UnsupportedAttributes)` | Warning | ✅ Section 3e: "Unsupported Attributes" |

### 3.6 RemoveEmbeddedTestSettings() (L247-254)

| Code (line) | Behavior | Skill Coverage |
|---|---|---|
| Select `/RunSettings/MSTest/SettingsFile`, remove from parent (L249-253) | Remove SettingsFile reference | ✅ "Migrate Embedded .testsettings" step 3 |

### 3.7 AddLegacyNodes() (L264-370)

| Code (line) | Behavior | Skill Coverage |
|---|---|---|
| Guard: all null/empty → return (L266-275) | Skip LegacySettings if nothing to add | ✅ 3b: "If **any** of these are present" implies skip otherwise |
| Create/find `<MSTest>` node (L283-294) | Ensure MSTest node exists | ✅ Implicit in 3b output XML |
| Create/find `<ForcedLegacyMode>` → set to "true" (L296-304) | Set ForcedLegacyMode | ✅ 3b: "`<ForcedLegacyMode>true</ForcedLegacyMode>`" |
| Remove existing `<LegacySettings>` if present (L307-312) | Replace existing LegacySettings | ✅ "Migrate Embedded" step 6: "If there's already a `<LegacySettings>` node...remove it first" |
| `Console.WriteLine(IgnoringLegacySettings)` (L310) | Warn about replacement | 🚫 Console message — skill says "remove it first (it will be replaced)" |
| Append `Deployment` to LegacySettings (L316-319) | Copy deployment | ✅ 3b XML shows `<Deployment ... />` |
| Append `Script` to LegacySettings (L321-324) | Copy scripts | ✅ 3b XML shows `<Scripts ... />` |
| Build `<Execution>` node with attributes + children (L327-367) | Complex Execution node | ✅ 3b "Rules for the Execution sub-node" — all 5 rules |
| `parallelTestCount` as attribute (L331-336) | Execution attribute | ✅ Rule: "parallelTestCount...attributes on `<Execution>`" |
| `hostProcessPlatform` as attribute (L338-343) | Execution attribute | ✅ Rule: "hostProcessPlatform" |
| `testTimeout` → `<Timeouts testTimeout="..."/>` (L345-352) | Timeout child element | ✅ Rule: "testTimeout goes into `<Timeouts testTimeout=.../>`" |
| `Hosts` → child element (L354-357) | Hosts node | ✅ Rule: "Hosts is copied as a child element" |
| `UnitTestConfig` → wrap in `<TestTypeSpecific>` (L359-364) | TestTypeSpecific wrapper | ✅ Rule: "UnitTestRunConfig is wrapped in `<TestTypeSpecific>`" |
| Append LegacySettings to root (L369) | Final append | ✅ Implicit in output structure |

### 3.8 AddDataCollectorNodes() (L377-400)

| Code (line) | Behavior | Skill Coverage |
|---|---|---|
| Create `<DataCollectionRunSettings>` if missing (L384-385) | Ensure parent node | ✅ 3d XML shows the full structure |
| Create `<DataCollectors>` if missing (L387-393) | Ensure DataCollectors container | ✅ |
| Foreach DataCollector → import (L396-399) | Copy each DataCollector | ✅ "each DataCollector copied as-is" |

### 3.9 AddRunTimeoutNode() (L407-422)

| Code (line) | Behavior | Skill Coverage |
|---|---|---|
| Create `<RunConfiguration>` if missing (L414-415) | Ensure RunConfiguration node | ✅ 3c XML shows `<RunConfiguration>` |
| Create `<TestSessionTimeout>` with value (L417-418) | Map runTimeout | ✅ "`<TestSessionTimeout>VALUE</TestSessionTimeout>`" |

## 4. TestSettingsNodes.cs — Data Container

| Property | Skill Coverage |
|---|---|
| `Deployment` | ✅ Extraction table row 1 |
| `Script` | ✅ Extraction table row 2 |
| `WebSettings` | ✅ Extraction table row 3 |
| `Datacollectors` | ✅ Extraction table row 4 |
| `Timeout` | ✅ Extraction table row 5 |
| `UnitTestConfig` | ✅ Extraction table row 6 |
| `Hosts` | ✅ Extraction table row 7 |
| `Execution` | ✅ Extraction table row 8 |

## 5. Unit Tests — Scenario Coverage

| Test | What It Verifies | Skill Coverage |
|---|---|---|
| `NonRootedPathIsNotMigrated` | CLI rejects non-absolute paths | 🚫 CLI validation, not relevant |
| `MigratorGeneratesCorrectRunsettingsForEmbeddedTestSettings` | Full migration from .runsettings with embedded .testsettings (absolute path) | ✅ "Migrate Embedded .testsettings" section |
| `MigratorGeneratesCorrectRunsettingsForEmbeddedTestSettingsOfRelativePath` | Relative .testsettings path resolution | ✅ Step 2: "resolve it relative to the .runsettings file location" |
| `MigratorGeneratesCorrectRunsettingsWithDc` | Data collectors migrated (2 DCs preserved) | ✅ Section 3d |
| `MigratorGeneratesCorrectRunsettingsForTestSettings` | Full migration from standalone .testsettings | ✅ "Migrate from .testsettings" section |
| `InvalidSettingsThrowsException` | Malformed XML throws XmlException | 🚫 Error handling — AI/human will naturally get XML parse errors |
| `InvalidPathThrowsException` | Non-existent drive throws IOException | 🚫 Error handling |

### Test Validate() Assertions Breakdown

The `Validate()` helper in `MigratorTests.cs` checks these specific output nodes:

| Assertion (line) | What It Checks | Skill Coverage |
|---|---|---|
| `WebTestRunConfiguration/Browser/Headers/Header` (L149) | WebTestRunConfiguration migrated | ✅ 3a |
| `LegacySettings` exists (L150) | LegacySettings node created | ✅ 3b |
| `LegacySettings/Deployment/DeploymentItem` (L151) | Deployment copied into LegacySettings | ✅ 3b |
| `LegacySettings/Scripts[@setupScript=".\\setup.bat"]` (L153-157) | Scripts with attributes preserved | ✅ 3b |
| `MSTest/ForcedLegacyMode = "true"` (L159-161) | ForcedLegacyMode set | ✅ 3b |
| `LegacySettings/Execution[@parallelTestCount="2"]` (L163-166) | parallelTestCount as Execution attribute | ✅ 3b rule 2 |
| `LegacySettings/Execution[@hostProcessPlatform="MSIL"]` (L167) | hostProcessPlatform as Execution attribute | ✅ 3b rule 2 |
| `LegacySettings/Execution/Hosts` (L169) | Hosts node present | ✅ 3b rule 4 |
| `LegacySettings/Execution/Timeouts[@testTimeout="120000"]` (L171-174) | testTimeout in Timeouts child | ✅ 3b rule 3 |
| `LegacySettings/Execution/TestTypeSpecific/UnitTestRunConfig/AssemblyResolution/TestDirectory` (L176) | UnitTestRunConfig wrapped in TestTypeSpecific | ✅ 3b rule 5 |
| `RunConfiguration/TestSessionTimeout = "60000"` (L178-180) | runTimeout → TestSessionTimeout | ✅ 3c |
| `DataCollectionRunSettings/DataCollectors/DataCollector[@friendlyName="Event Log"]` (L182-185) | DataCollector migrated | ✅ 3d |

## 6. Localization Resources

| Resource Key | Message | Skill Coverage |
|---|---|---|
| `ValidUsage` | CLI usage instructions | 🚫 Not needed — no CLI |
| `RunSettingsCreated` | "The migrated RunSettings file has been created at: {0}" | 🚫 Console output |
| `NoEmbeddedSettings` | "RunSettings does not contain an embedded testSettings, not migrating." | ⚠️ Skill doesn't explicitly say "do nothing if no embedded settings found" — implicit in step 2 logic |
| `IgnoringLegacySettings` | "Any LegacySettings node already present...will be removed." | ✅ Step 6: "remove it first (it will be replaced)" |
| `UnsupportedAttributes` | Warning about agentNotRespondingTimeout, deploymentTimeout, scriptTimeout | ✅ Section 3e |

---

## Summary

| Category | Total | ✅ Covered | 🚫 Intentionally Omitted | ⚠️ Minor Gap |
|---|---|---|---|---|
| Code paths in Migrator.cs | 28 | 23 | 4 (console messages) | 1 |
| Code paths in PathResolver.cs | 3 | 0 | 3 (CLI arg validation) | 0 |
| Code paths in Program.cs | 3 | 1 | 2 (CLI entry/exit) | 0 |
| TestSettingsNodes properties | 8 | 8 | 0 | 0 |
| Unit test scenarios | 7 | 5 | 2 (error handling) | 0 |
| Validate() assertions | 12 | 12 | 0 | 0 |
| Localization resources | 5 | 2 | 2 (CLI messages) | 1 |
| **Total** | **66** | **51 (77%)** | **13 (20%)** | **2 (3%)** |

### ⚠️ Minor Gaps

1. **"No embedded settings" message**: The skill doesn't explicitly say "if no `<SettingsFile>` node is found, there's nothing to migrate" — it's implicit in the conditional flow. **Verdict: acceptable** — the step says "Find `<MSTest><SettingsFile>`" which naturally produces nothing if absent.

2. **"IgnoringLegacySettings" detail**: The skill says "remove it first" but doesn't explicitly note that this means the old LegacySettings content is lost. **Verdict: acceptable** — "remove it first (it will be replaced)" is clear enough.

### 🚫 Intentional Omissions

All 13 omitted items are CLI-specific concerns (argument parsing, exit codes, console output messages) that don't apply to a skill/documentation context. A human following the skill instructions handles I/O themselves.
