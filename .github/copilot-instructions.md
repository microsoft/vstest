This is a .NET based repository that contains the VSTest test platform. Please follow these guidelines when contributing:

## Code Standards

You MUST follow all code-formatting and naming conventions defined in [`.editorconfig`](../.editorconfig).

In addition to the rules enforced by `.editorconfig`, you SHOULD:

- Favor style and conventions that are consistent with the existing codebase.
- Prefer file-scoped namespace declarations and single-line using directives.
- Ensure that the final return statement of a method is on its own line.
- Use pattern matching and switch expressions wherever possible.
- Use `nameof` instead of string literals when referring to member names.
- Always use `is null` or `is not null` instead of `== null` or `!= null`.
- Trust the C# null annotations and don't add null checks when the type system says a value cannot be null.
- Prefer `?.` if applicable (e.g. `scope?.Dispose()`).
- Use `ObjectDisposedException.ThrowIf` where applicable.
- Respect StyleCop.Analyzers rules, in particular:
  - SA1028: Code must not contain trailing whitespace
  - SA1316: Tuple element names should use correct casing
  - SA1518: File is required to end with a single newline character

You MUST minimize adding public API surface area but any newly added public API MUST be declared in the related `PublicAPI.Unshipped.txt` file.

## Localization Guidelines

Anytime you add a new localization resource, you MUST:
- Add a corresponding entry in the localization resource file.
- Add an entry in all `*.xlf` files related to the modified `.resx` file.
- Do not modify existing entries in '*.xlf' files unless you are also modifying the corresponding `.resx` file.

## Unattended Work Instructions

When working autonomously on issues (e.g. from a milestone), follow this workflow:

### Before Starting

- **Assign the issue** to the user you are working on behalf of before starting work.
- **Skip issues with active PRs** — if an issue already has a PR with recent activity, don't duplicate the work.

### Implement

- Create a feature branch from `main` (never commit to `main` directly).
- Implement the change and write tests where applicable.
- Build locally with `-c Release` before pushing — CI uses Release mode with `TreatWarningsAsErrors`, so warnings like IDE0005 (unnecessary using) become build errors.
- Run relevant tests locally: `.dotnet\dotnet.exe test <project> --no-build -c Release --filter <testname>`.

### Create PR

- Push the branch to `origin` (the fork).
- Create the PR against `microsoft/vstest` (the upstream repo) — **never PR to the fork**.

### Monitor PRs

- CI builds take approximately one hour.
- Check **both** CI status (pass/fail) **and** mergeable state — PR checks can show green even when there are merge conflicts. Always verify with `gh pr view <number> --json mergeable`.
- If a build fails, investigate the failure, push a fix to the same branch, and wait for the rebuild.
- If a PR becomes CONFLICTING, rebase the branch onto `main` and force-push.
