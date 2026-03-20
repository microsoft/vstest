---
name: settings-migration
description: Migrate legacy Visual Studio .testsettings files to .runsettings format. Use when users need to convert old test configuration files, understand the mapping between formats, or troubleshoot migration issues. Replaces the deprecated SettingsMigrator.exe tool.
---

# Migrating .testsettings to .runsettings

This skill replaces the deprecated `SettingsMigrator.exe` tool. It covers migrating legacy `.testsettings` files (and `.runsettings` files with embedded `.testsettings` references) to the modern `.runsettings` format.

## Background

Visual Studio historically used `.testsettings` files for test configuration. The modern format is `.runsettings`. The two formats differ in structure, and some `.testsettings` nodes map to `<LegacySettings>` wrappers in `.runsettings` to preserve backward compatibility.

See also: [RFC 0023 - TestSettings Deprecation](../../docs/RFCs/0023-TestSettings-Deprecation.md)

## When to Use

- User has a `.testsettings` file and needs a `.runsettings` file
- User has a `.runsettings` with an `<MSTest><SettingsFile>path.testsettings</SettingsFile></MSTest>` embedded reference
- User asks about migrating test settings, test configuration formats, or legacy settings

## Step-by-Step Migration

### Step 1: Identify the Source File Type

Check the file extension:
- **`.testsettings`** → Go to [Migrate from .testsettings](#migrate-from-testsettings)
- **`.runsettings` with embedded settings** → Go to [Migrate embedded .testsettings](#migrate-embedded-testsettings-from-runsettings)

To check for embedded settings in a `.runsettings` file, look for:
```xml
<RunSettings>
  <MSTest>
    <SettingsFile>path\to\file.testsettings</SettingsFile>
  </MSTest>
</RunSettings>
```

### Step 2: Read the .testsettings File

Parse the `.testsettings` XML and extract these nodes (all are optional):

| XPath in .testsettings | Node Name | Purpose |
|---|---|---|
| `/TestSettings/Deployment` | Deployment | File deployment settings |
| `/TestSettings/Scripts` | Scripts | Setup/cleanup scripts |
| `/TestSettings/Execution/TestTypeSpecific/WebTestRunConfiguration` | WebSettings | Web test configuration |
| `/TestSettings/Execution/AgentRule/DataCollectors/DataCollector` | DataCollectors | Data collector definitions |
| `/TestSettings/Execution/Timeouts` | Timeouts | Timeout attributes |
| `/TestSettings/Execution/TestTypeSpecific/UnitTestRunConfig` | UnitTestConfig | Unit test run configuration |
| `/TestSettings/Execution/Hosts` | Hosts | Host configuration |
| `/TestSettings/Execution` | Execution | Execution attributes (parallelTestCount, hostProcessPlatform) |

### Step 3: Build the .runsettings Output

Start with a minimal `.runsettings` skeleton:

```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
</RunSettings>
```

Then apply each mapping rule below.

#### 3a. WebTestRunConfiguration (top-level)

If the `.testsettings` has a `WebTestRunConfiguration` node, copy it directly as a child of `<RunSettings>`:

```xml
<RunSettings>
  <WebTestRunConfiguration>
    <!-- copied from .testsettings as-is -->
  </WebTestRunConfiguration>
</RunSettings>
```

#### 3b. LegacySettings Node

If **any** of these are present: `Deployment`, `Scripts`, `UnitTestConfig`, `Hosts`, `parallelTestCount`, `testTimeout`, or `hostProcessPlatform`, create a `<LegacySettings>` node and also set `<MSTest><ForcedLegacyMode>true</ForcedLegacyMode></MSTest>`:

```xml
<RunSettings>
  <LegacySettings>
    <!-- Deployment node (copied as-is) -->
    <Deployment ... />

    <!-- Scripts node (copied as-is) -->
    <Scripts ... />

    <!-- Execution node (built from parts) -->
    <Execution parallelTestCount="N" hostProcessPlatform="X">
      <Timeouts testTimeout="M" />
      <Hosts>...</Hosts>
      <TestTypeSpecific>
        <UnitTestRunConfig>...</UnitTestRunConfig>
      </TestTypeSpecific>
    </Execution>
  </LegacySettings>

  <MSTest>
    <ForcedLegacyMode>true</ForcedLegacyMode>
  </MSTest>
</RunSettings>
```

**Rules for the Execution sub-node:**
- Only create `<Execution>` if at least one of `UnitTestConfig`, `parallelTestCount`, `testTimeout`, `Hosts` is present
- `parallelTestCount` and `hostProcessPlatform` are **attributes** on the `<Execution>` element
- `testTimeout` goes into `<Timeouts testTimeout="..."/>` child element
- `Hosts` is copied as a child element
- `UnitTestRunConfig` is wrapped in `<TestTypeSpecific>` child element

#### 3c. TestSessionTimeout (from runTimeout)

If the `.testsettings` `Timeouts` node has a `runTimeout` attribute, map it to:

```xml
<RunSettings>
  <RunConfiguration>
    <TestSessionTimeout>VALUE</TestSessionTimeout>
  </RunConfiguration>
</RunSettings>
```

#### 3d. DataCollectors

If the `.testsettings` has DataCollector nodes, copy them to:

```xml
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector ...>...</DataCollector>
      <!-- each DataCollector copied as-is -->
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

#### 3e. Unsupported Attributes (warn the user)

These `.testsettings` timeout attributes are **not supported** and will be lost:
- `agentNotRespondingTimeout`
- `deploymentTimeout`
- `scriptTimeout`

If any of these are present, warn the user that they cannot be migrated.

### Migrate from .testsettings

1. Parse the `.testsettings` file (Step 2)
2. Build a fresh `.runsettings` (Step 3)
3. Save the output file

### Migrate Embedded .testsettings from .runsettings

1. Parse the existing `.runsettings` file
2. Find `<MSTest><SettingsFile>` node and read the `.testsettings` path
   - If the path is relative, resolve it relative to the `.runsettings` file location
3. Remove the `<SettingsFile>` node from the `.runsettings`
4. Parse the referenced `.testsettings` file (Step 2)
5. Apply all mappings (Step 3) into the existing `.runsettings` document
6. If there's already a `<LegacySettings>` node in the `.runsettings`, remove it first (it will be replaced)
7. Save the output file

## Complete Example

### Input: `legacy.testsettings`

```xml
<?xml version="1.0" encoding="UTF-8"?>
<TestSettings name="MySettings">
  <Deployment>
    <DeploymentItem filename="TestData\" />
  </Deployment>
  <Scripts setupScript="setup.bat" cleanupScript="cleanup.bat" />
  <Execution parallelTestCount="4">
    <Timeouts testTimeout="30000" runTimeout="600000" agentNotRespondingTimeout="300000" />
    <TestTypeSpecific>
      <UnitTestRunConfig testTypeId="13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b">
        <AssemblyResolution>
          <TestDirectory useLoadContext="true" />
        </AssemblyResolution>
      </UnitTestRunConfig>
    </TestTypeSpecific>
    <Hosts />
  </Execution>
  <AgentRule name="LocalMachineDefaultRole">
    <DataCollectors>
      <DataCollector uri="datacollector://Microsoft/CodeCoverage/2.0"
                     assemblyQualifiedName="Microsoft.VisualStudio.Coverage.DynamicCoverageDataCollector">
      </DataCollector>
    </DataCollectors>
  </AgentRule>
</TestSettings>
```

### Output: `migrated.runsettings`

```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <LegacySettings>
    <Deployment>
      <DeploymentItem filename="TestData\" />
    </Deployment>
    <Scripts setupScript="setup.bat" cleanupScript="cleanup.bat" />
    <Execution parallelTestCount="4">
      <Timeouts testTimeout="30000" />
      <Hosts />
      <TestTypeSpecific>
        <UnitTestRunConfig testTypeId="13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b">
          <AssemblyResolution>
            <TestDirectory useLoadContext="true" />
          </AssemblyResolution>
        </UnitTestRunConfig>
      </TestTypeSpecific>
    </Execution>
  </LegacySettings>
  <RunConfiguration>
    <TestSessionTimeout>600000</TestSessionTimeout>
  </RunConfiguration>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector uri="datacollector://Microsoft/CodeCoverage/2.0"
                     assemblyQualifiedName="Microsoft.VisualStudio.Coverage.DynamicCoverageDataCollector">
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
  <MSTest>
    <ForcedLegacyMode>true</ForcedLegacyMode>
  </MSTest>
</RunSettings>
```

> **Note:** The `agentNotRespondingTimeout` attribute was dropped because it is not supported in the modern format. The user should be warned about this.
