# Release guide for kRPC for KSA

kRPC for KSA releases on its own train, tagged `ksa-vX.Y.Z`. A release publishes the mod archive on
GitHub, the docs on the `gh-pages` branch, and the mod on Borea, SpaceDock and the KSA forum. KSA
updates often and each update can break the mod, so most releases follow a KSA update.

A release is a sequence of small Python scripts in `experimental/ksa/tools/release/`, run in
numbered order. Each script does one step, can be re-run if something fails, and asks for
confirmation before publishing anything. They build on the helpers of the kRPC release scripts in
`tools/release/`, and run from any directory.

They read the same credentials file as the kRPC release, `release-credentials.toml` at the root of
the repository, and need its `GITHUB_TOKEN`, `SPACEDOCK_USERNAME` and `SPACEDOCK_PASSWORD`. Pushes
to `krpc/krpc` and to your fork of the Borea content index use your normal git setup.

The Bazel targets take the game from `KSA_DIR`, and run StarMap from `STARMAP_DIR`. Set both to
absolute paths.

The guide starts from a release branch that holds the code to release: `main`, or a maintenance
branch such as `release/0.8` ([release branches](../../Release-Guide.md#release-branches)).

## 1. Prepare

1. `01-prepare.py [version]` - on a release preparation branch off an up to date release branch,
   releases the unreleased section of `CHANGELOG.md`, sets `mod_version` in `config.bzl`, and
   commits them as `Release kRPC for KSA X.Y.Z`. A release that only follows a KSA update is the
   patch version the update started. Pass a version for a minor release. If `core/src` changed
   since `core_version` in the root `config.bzl` was last bumped, the step asks how to bump it, as
   the kRPC release guide's fold step does. Check that
   `client_version` in `config.bzl` names the kRPC client release whose protocol the mod speaks,
   as the docs and the stubs readme name it. Push the branch, open the release preparation pull
   request, and run every CI job on it by hand with `gh workflow run ci.yml --ref <branch>`. The
   pull request and the merge queue skip the daily jobs, such as Windows. Merge it once its checks
   and the manual run pass, then check out the release commit.
2. `10-preflight.py` - checks the tree, the release commit, the tools, the KSA install, the
   credentials, SpaceDock and the Borea listing. Read-only, so run it as often as you like.
3. `20-borea-listing.py [--game-min <build>] [--loader-min <version>]` - sets the KSA and StarMap
   minimums of the `kRPC` listing in the Borea content index. They default to the KSA build and
   StarMap release in `config.bzl`. The step opens a pull request from your fork of
   `KSAModding/content-index`, and waits for it to merge itself. The Borea watcher stamps a
   release with the listing's bounds at the time, so this comes before the GitHub release.

## 2. Build and Tag

4. `30-build.py` - builds `bazel-bin/experimental/ksa/krpc-ksa-X.Y.Z.zip` with
   `//experimental/ksa:release`, against the KSA install in `KSA_DIR`, and runs
   `//experimental/ksa:test` and the docs link check.
5. `40-tag.py` - makes the annotated `ksa-vX.Y.Z` tag on the release commit, which must be on
   the release branch, and pushes it. A `v*` tag would start the docs workflow of the kRPC release.

## 3. Publish

6. `50-release-github.py` - creates and publishes the GitHub release, with the changelog section
   as its notes and the archive as its only asset. `--latest=false` keeps the kRPC release as the
   latest release of the repository.
7. `60-publish-docs.py` - builds the docs and pushes them to the `gh-pages` branch, served at
   `https://krpc.github.io/krpc/experimental/ksa/`.
8. `61-release-spacedock.py` - uploads the archive to SpaceDock, for the KSA build in
   `config.bzl`, with the changelog page of the docs as its changelog, and emails its followers.
   The email cannot be recalled, and a version can only be deleted by hand on the site.
9. `62-check-borea.py` - waits for the Borea watcher to stamp the release into
   `KSAModding/content-index-releases`, and shows the bounds it was stamped with.
10. Read the description of the
    [listing](https://github.com/KSAModding/content-index/blob/main/listings/kRPC.toml) against
    the release, and update it when the release makes it out of date. Borea shows this text, and
    the watcher never changes it. Edit it with the
    [listing page](https://ksamodding.github.io/content-index/), under "Change a listed one",
    which opens the pull request from your fork. The pull request merges itself once its checks
    pass.
11. `70-announcements.py` - prints the post for the
    [forum thread](https://forums.ahwoo.com/forums/kitten-space-agency/mod-releases/krpc-v0-1-0-control-ksa-with-scripts-in-python-c-c-c-java-lua-and-more.1462/).
    Post it, and update the first post of the thread. The forum has no API for this.
