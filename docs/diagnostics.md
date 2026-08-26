# Test Platform diagnostic IDs (`TPVS`)

Deprecated APIs in this repository report a stable, project-owned diagnostic ID with the `TPVS` prefix
instead of the generic compiler code `CS0618`. A dedicated ID lets you suppress a single deprecation
without silencing every other obsolete-API warning in your project.

All `TPVS` diagnostics are **warnings**. None of them is reported as an error.

## Suppressing a `TPVS` diagnostic

> [!IMPORTANT]
> `ObsoleteAttribute.DiagnosticId` requires .NET 5 or newer. The Test Platform assemblies also target
> .NET Framework and `netstandard2.0`, where the attribute property does not exist and the compiler keeps
> reporting `CS0618`. **The reported ID therefore depends on the target framework of the project that
> consumes the API**, and a multi-targeted project sees `TPVS0nn` on its .NET leg and `CS0618` on its
> .NET Framework or `netstandard2.0` leg.
>
> If you already suppress `CS0618` for one of these APIs, suppress both IDs so the suppression keeps
> working on every leg:
>
> ```csharp
> #pragma warning disable CS0618, TPVS004
> runConfiguration.TargetFrameworkVersion = FrameworkVersion.Framework45;
> #pragma warning restore CS0618, TPVS004
> ```
>
> The same applies to `<NoWarn>`:
>
> ```xml
> <NoWarn>$(NoWarn);CS0618;TPVS004</NoWarn>
> ```
>
> Listing an ID the current compiler does not recognise is harmless, so the combined form is safe on all
> target frameworks.

## Allocated IDs

| ID | Deprecated API | Use instead |
| -- | -------------- | ----------- |
| [`TPVS001`](#tpvs001) | `IVsTestConsoleWrapperAsync`, and every member of it except the two `ProcessTestRunAttachmentsAsync` overloads | The synchronous members of `IVsTestConsoleWrapper`. |
| [`TPVS002`](#tpvs002) | `ITestRunEventsHandler2` and `ITestRunEventsHandler2.AttachDebuggerToProcess` | `ITestRunEventsHandler`, plus `ITestHostLauncher2` or `ITestHostLauncher3`. |
| [`TPVS003`](#tpvs003) | `IDataCollectorAttachments` | `IDataCollectorAttachmentProcessor`. |
| [`TPVS004`](#tpvs004) | `RunConfiguration.TargetFrameworkVersion` | `RunConfiguration.TargetFramework`. |
| [`TPVS005`](#tpvs005) | `TestPropertyAttributes.Trait` | The `TestObject.Traits` collection. |
| [`TPVS006`](#tpvs006) | `IFrameworkHandle.EnableShutdownAfterTestRun` | Nothing, the property has no effect. |

<a id="tpvs001"></a>

### TPVS001 — `IVsTestConsoleWrapperAsync`

The asynchronous Translation Layer API does not work as intended. Use the synchronous members of
`IVsTestConsoleWrapper` instead.

`IVsTestConsoleWrapper` still derives from `IVsTestConsoleWrapperAsync`, because removing the base
interface would be a binary breaking change. `TPVS001` is applied both to the interface *and* to each of its
deprecated members, and both are required: an `[Obsolete]` attribute on an interface is **not** reported when
a member is reached through a derived interface, so dropping the per-member attributes would silence the
warning for every consumer that holds an `IVsTestConsoleWrapper`.

The two `ProcessTestRunAttachmentsAsync` overloads are the exception: they carry no `[Obsolete]` attribute,
because they have no synchronous replacement. They still sit on a deprecated interface, so naming
`IVsTestConsoleWrapperAsync` to reach them reports `TPVS001` — hold an `IVsTestConsoleWrapper` instead, where
the inherited overloads report nothing.

<a id="tpvs002"></a>

### TPVS002 — `ITestRunEventsHandler2`

You do not have to implement this interface; `AttachDebuggerToProcess` is never called back. Implement
`ITestRunEventsHandler` and, to attach a debugger, `ITestHostLauncher2` or `ITestHostLauncher3`.

The interface and its `AttachDebuggerToProcess` method deliberately share one ID, so a single suppression
silences both.

<a id="tpvs003"></a>

### TPVS003 — `IDataCollectorAttachments`

Use `IDataCollectorAttachmentProcessor`, which supports asynchronous processing and cancellation.

<a id="tpvs004"></a>

### TPVS004 — `RunConfiguration.TargetFrameworkVersion`

Use `RunConfiguration.TargetFramework`. The `FrameworkVersion` enum cannot express every target framework,
whereas `TargetFramework` accepts any framework moniker.

<a id="tpvs005"></a>

### TPVS005 — `TestPropertyAttributes.Trait`

Use the `TestObject.Traits` collection to read and write traits.

The flag itself cannot be removed: its bit value is part of the serialized shape of the `TestObject.Traits`
property and has to stay `0x04` for wire compatibility with older hosts and adapters.

<a id="tpvs006"></a>

### TPVS006 — `IFrameworkHandle.EnableShutdownAfterTestRun`

The property has no effect and there is no replacement. Remove the assignment.

## Allocating a new ID

* IDs are allocated sequentially from `TPVS001` and are **never reused**, because a retired ID may still
  appear in a consumer's suppression list.
* An ID is unique across the whole product, not per assembly. Add a new entry to the table above before
  using it so the next allocation does not collide.
* Apply an ID to the **public API being deprecated, not to internal implementations of it**. The diagnostic
  a consumer sees comes from the declaring type, so an ID on an internal implementation of an obsolete
  interface member is inert.
* Set `DiagnosticId` and `UrlFormat` inside an `#if NET` block and keep an `#else` branch with the plain
  attribute, so the API stays deprecated on .NET Framework and `netstandard2.0`.
* Point `UrlFormat` at `#tpvs0nn`, the **lowercased** id, and add a matching `<a id="tpvs0nn"></a>` above the
  section below. GitHub lowercases the `id` of an anchor while sanitizing rendered markdown but resolves the
  fragment case sensitively, so an uppercase fragment silently lands at the top of the page. That also rules
  out `UrlFormat = "...#{0}"`, because `{0}` expands to the diagnostic id verbatim, which is uppercase.
* Do not confuse this prefix with `TPEXP`, which `Microsoft.Testing.Platform` uses to mark experimental
  APIs with `[Experimental]`.

## Related

* [`ObsoleteAttribute.DiagnosticId`](https://learn.microsoft.com/dotnet/api/system.obsoleteattribute.diagnosticid)
* [Suppress code analysis warnings](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/suppress-warnings)
