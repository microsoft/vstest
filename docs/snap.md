# Snapping vstest to a new version

A snap moves `main` on to the next version and leaves the version that just stabilized on its own
`rel/<version>` branch. Most of it is two small pull requests. The rest happens in Maestro and in
the Arcade publishing configuration, outside this repository, and that is the part that keeps
being missed.

Two snaps in a row broke publishing on `main`, four weeks apart, for the same reason both times:

- 18.11, snapped 2026-07-28. Official builds of `main` failed until 2026-07-30.
- 18.12, snapped 2026-08-25. Official builds of `main` failed the same way.

Neither failure needed a change in this repository, and neither was a regression in vstest. Both
were a missing entry in the Arcade publishing configuration. Work through this whole list, not
just the branding pull requests.

## Step 1: the branding pull requests

A snap is two pull requests:

| Branch | Title | Change |
| --- | --- | --- |
| `rel/<version>` | `Branding as <version>-release` | `PreReleaseVersionLabel` from `preview` to `release` in `eng/Versions.props` |
| `main` | `Branding as <next version>` | `VersionPrefix` to the next version in `eng/Versions.props` |

The 18.11 to 18.12 snap is the worked example:
[microsoft/vstest#16407](https://github.com/microsoft/vstest/pull/16407) `Branding as 18.11.0-release`
on `rel/18.11`, and [microsoft/vstest#16408](https://github.com/microsoft/vstest/pull/16408)
`Branding as 18.12` on `main`.

This is the easy part. The steps below are the ones that get skipped.

## Step 2: create the `VS <next version>` channel and point `main` at it

Every version has its own Maestro channel, named `VS <version>`. Create the new one and move the
default channel mapping for `main` on to it:

```powershell
darc add-channel --name "VS 18.12"
darc delete-default-channel --channel "VS 18.11" --branch main --repo https://github.com/microsoft/vstest
darc add-default-channel --channel "VS 18.12" --branch main --repo https://github.com/microsoft/vstest
```

These commands do not change anything directly. Each one opens a pull request against the Maestro
configuration repository, which then has to be merged. The darc documentation also says to consult
dnceng before creating a channel, so ask them first.

Creating the channel is not enough on its own. Step 3 is the other half of it.

## Step 3: get the new channel into the Arcade publishing configuration

**This is the step that has now been missed twice.** A channel that exists in Maestro is still not
a channel that anything can publish to. The channel id also has to be listed in
[`PublishingConstants.cs`](https://github.com/dotnet/arcade/blob/main/src/Microsoft.DotNet.Build.Tasks.Feed/src/model/PublishingConstants.cs)
in dotnet/arcade. Ask dnceng to add it, or open the pull request yourself. The procedure is written
down in Arcade's own
[servicing policy](https://github.com/dotnet/arcade/blob/main/Documentation/Policy/ArcadeServicing.md).

Skip it and every official build of `main` fails at the `Publish Using Darc` step, starting with
the first build after the branding commit. It stays broken until the Arcade pull request is merged
**and** an Arcade build carrying it has been promoted. Merging alone does not help, because the
promotion pipeline uses a published Arcade version rather than Arcade's `main`. Both times this
took about two days.

What happened the last two times:

- **VS 18.11, channel id 10800.** vstest branding merged 2026-07-28.
  [dotnet/arcade#17215](https://github.com/dotnet/arcade/pull/17215) added the channel on
  2026-07-29. `main` published again on 2026-07-30, with no change in this repository.
- **VS 18.12, channel id 10894.** vstest branding merged 2026-08-25.
  [dotnet/arcade#17404](https://github.com/dotnet/arcade/pull/17404) added the channel about
  ninety minutes later, and the next build of `main` still failed, because the promotion pipeline
  only picks the change up once an Arcade build carrying it has been promoted.

The error is `Channel with ID '<id>' is not configured to be published to.` Step 6 explains where
to find it, because the step that fails does not print it.

Do not try to fix this from vstest. The publishing configuration is not in this repository, and the
promotion pipeline restores its own Arcade version rather than the one pinned in
`eng/Version.Details.xml`, so bumping Arcade here changes nothing.

## Step 4: add the default channel for the new `rel/<version>` branch

The new release branch needs its own default channel mapping:

```powershell
darc add-default-channel --channel "VS 18.11" --branch rel/18.11 --repo https://github.com/microsoft/vstest
```

This one is easy to forget because nothing turns red when it is missing. The official build of the
release branch reports **success** and publishes nothing. It produces a BAR record attached to no
channel, so no assets are promoted anywhere, and no log anywhere contains an error.

At the time of writing `rel/18.11` is in exactly that state. Every other release branch, from
`rel/16.7` up to `rel/18.10`, has a default channel. `rel/18.11` has none, even though the
`VS 18.11` channel exists. Its build on 2026-08-25 succeeded, and the BAR build it produced,
328503, is attached to no channel.

## Step 5: verify the snap actually published

Check the default channel mappings first:

```powershell
darc get-default-channels --source-repo microsoft/vstest
```

`main` must map to `VS <next version>`, and `rel/<version>` must map to `VS <version>`. If either
row is missing, go back to step 2 or step 4.

Then check that the first official build on each branch published something. A green build is not
proof, because a build with no default channel succeeds and publishes nothing. The BAR record is
what shows whether assets were attached to a channel:

```powershell
darc get-build --id <bar build id>
```

The BAR build id is printed by the `Publish Build Assets` step of the
`Publish to Build Asset Registry` leg of the official build. If the build lists no channel,
nothing was published.

## Step 6: reading a `Publish Using Darc` failure

The `Publish Using Darc` step never contains the real error. All it prints is:

```text
The promotion build finished but the build isn't associated with at least one of the target channels. This is an error scenario.
Details are available in the following build: https://dev.azure.com/dnceng/internal/_build/results?buildId=<id>
```

Follow that build id. It is a `Maestro Build Promotion` build, and the real error is in its
`Publish packages, blobs and symbols` task:

```text
PublishArtifactsInManifest.proj(129,5): error : Channel with ID '10894' is not configured to be published to.
```

That message means step 3 has not finished yet. A red `Publish Using Darc` on `main` within a
couple of days of a `Branding as <version>` commit is this, and not a regression in vstest.
