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

## Working with Git Worktrees

This repository uses git worktrees to work on multiple things at the same time. Worktrees live in `../vstest-tree/<branch>` relative to the main clone at `c:\p\vstest`.

The following global git aliases are configured:

```shell
git config --global alias.wta '!f() { mkdir -p "$(git rev-parse --show-toplevel)/../vstest-tree" 2>/dev/null; git worktree add -b "$1" "../vstest-tree/$1"; }; f'
git config --global alias.wtr '!f() { git worktree remove "../vstest-tree/$1"; }; f'
git config --global alias.wtl '!f() { git worktree list; }; f'
```

Usage:

```shell
git wta my-feature-branch   # creates c:\p\vstest-tree\my-feature-branch
git wtl                     # list all active worktrees
git wtr my-feature-branch   # remove worktree when done
```

The upstream remote `upstream` points to `https://github.com/microsoft/vstest`. To sync with upstream:

```shell
git fetch upstream
git checkout main
git merge upstream/main
```

## Localization Guidelines

Anytime you add a new localization resource, you MUST:
- Add a corresponding entry in the localization resource file.
- Add an entry in all `*.xlf` files related to the modified `.resx` file.
- Do not modify existing entries in '*.xlf' files unless you are also modifying the corresponding `.resx` file.
