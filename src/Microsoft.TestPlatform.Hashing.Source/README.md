# Microsoft.TestPlatform.Hashing.Source

Shared source (not a NuGet package, not a separate assembly). The files here are compiled
directly into `Microsoft.TestPlatform.ObjectModel` and `Microsoft.TestPlatform.AdapterUtilities`
as `internal` types, following the same pattern as `src/Microsoft.TestPlatform.Filter.Source`.
`TestIdSeed.cs` is additionally compiled into `Microsoft.TestPlatform.CrossPlatEngine`, which has
to reproduce the id of a test case from the runner process on the Microsoft.Testing.Platform path.

## What is here

| File | Origin |
|---|---|
| `XxHash128.cs` | `dotnet/runtime` — `src/libraries/System.IO.Hashing/src/System/IO/Hashing/XxHash128.cs` |
| `XxHashShared.cs` | `dotnet/runtime` — `src/libraries/System.IO.Hashing/src/System/IO/Hashing/XxHashShared.cs` |
| `BitOperations.cs` | polyfill of `System.Numerics.BitOperations` for target frameworks that lack it |
| `TestIdGuid.cs` | vstest-authored — turns a 128-bit hash into an RFC 9562 version 8 UUID |
| `TestIdSeed.cs` | vstest-authored — composes the string a test case id is hashed from |

`XxHash128.cs` and `XxHashShared.cs` were vendored via [microsoft/testfx][testfx-hashing],
which vendors them from `dotnet/runtime`. Both upstreams are MIT licensed.

## Why vendored instead of `PackageReference Include="System.IO.Hashing"`

`Microsoft.TestPlatform.ObjectModel` and `Microsoft.TestPlatform.AdapterUtilities` ship
`netstandard2.0` and `net462` assets that are loaded by hosts which do **not** have binding
redirects — the Distributed Test Agent (DTA) being the canonical example. Taking
`System.IO.Hashing` would:

1. add a brand new assembly identity (`System.IO.Hashing`) that needs a binding redirect in
   `src/vstest.console/app.config`, `src/testhost.x86/app.config` **and**
   `src/datacollector/app.config` — miss one and net462 hosts get a `FileLoadException`;
2. add a new DLL to every shipped nupkg, changing `eng/expected-nupkg-file-counts.json` and
   `eng/expected-dll-frameworks.json` and landing a new file next to every test adapter;
3. add a new public package dependency to `Microsoft.TestPlatform.ObjectModel`, which is
   consumed extremely widely.

Vendoring avoids all three. It only makes `System.Memory` and
`System.Runtime.CompilerServices.Unsafe` explicit on the .NET Framework / netstandard2.0 legs —
both of which `ObjectModel` already pulled in transitively via `System.Reflection.Metadata`, and
both of which **already** have binding redirects in all three app.configs.

microsoft/testfx reached the same conclusion for the same reason and vendors these exact files.

## Keeping in sync

Keep the vendored files as close to upstream as possible so future syncs stay cheap. The
adaptations applied are listed in a header comment at the top of each file; there are only two
per file (explicit `using` directives, because vstest does not enable `ImplicitUsings`, and a
namespace change). Do not reformat these files and do not "fix" analyzer complaints in them —
they are excluded from repo style enforcement via `.editorconfig`.

[testfx-hashing]: https://github.com/microsoft/testfx/tree/main/src/Platform/Microsoft.Testing.Extensions.TrxReport/Hashing
