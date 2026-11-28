# Release guide for kRPC

A release is a sequence of small Python scripts in `tools/release/`, run in numbered order, with a
few manual steps in between. Each script does one step, can be re-run if something fails, and asks
for confirmation before publishing anything externally. They can be run from any directory; they
locate the repository root themselves. They need nothing beyond a Python 3.12 interpreter and the
tools of the step being run. This guide releases the KSP mod, the clients and the tools. The KSA
mod releases separately.

All the credentials live in one git-ignored TOML file at the root of the repository. Copy
`tools/release/release-credentials.toml.template` to `release-credentials.toml`, fill in every value, and `chmod 600` it; the template says what each one is and where to get it. The scripts pass these to `gh`, `twine`,
`dotnet`, `luarocks`, `aws`, `gpg` and `npm` explicitly, so none of those tools needs to be configured
or logged in beforehand, and whatever account they are already set up with is ignored. Git is the
exception: pushes to `krpc/krpc`, `krpc-arduino` and your vcpkg fork use your normal git setup.
The `krpc-arduino` clone and the vcpkg fork are reached over SSH, so they need an SSH key on the
release account.

The two vcpkg steps also need a git checkout of vcpkg pointed to by `VCPKG_ROOT` (the same one the
client vcpkg test scripts use) and a fork of `microsoft/vcpkg` on the release account; `gh` creates
the fork the first time if it does not exist.

The Maven Central step signs the Java client with the project's GPG key. The credentials file holds
the armored secret key and its passphrase, and the step imports them into a throwaway keyring. The
public key must be on `keys.openpgp.org` or `keyserver.ubuntu.com`, where the Central Portal looks
it up. Publish a new key with `gpg --send-keys --keyserver keyserver.ubuntu.com <fingerprint>`.

## Release Branches

A release is made from `main`, or from a maintenance branch under `release/`, such as
`release/0.8`. A maintenance branch carries a series `main` can no longer release, such as the
KSA mod for the 0.8 clients while `main` works towards 1.0.

* **A change for the series lands on the maintenance branch first.** Pull requests target it.
* **The maintenance branch merges forward into `main`**, through a pull request soon after each
  change. The merge commit resolves the conflicts with `main`. Changes never move back from `main`.
* **To create one**, push it from the last commit of `main` the series can release. The rulesets
  that target `release/**` protect it from the start, and require its pull requests to pass CI
  up to date with the branch (`CI-Guide.md`).
* **Delete it** once a release from `main` replaces the series.

## 1. Prepare

A release preparation pull request takes the release branch from the version being released to
the next one. `versions` in `config.bzl` holds the version each release train works towards. A
release takes every train but `ksa`, at the version of the `ksp` train. The preflight step checks
that they match. Every release step sets `KRPC_RELEASE=1` for the builds it runs, so they are
release builds, with no `-dev` marker.

1. Create a branch off the release branch, such as `release-vx.y.z`.
2. `tools/release/01-fold-changelogs.py` - writes the fragments in `changelog.d/` into each
   component's `CHANGELOG.md`, takes those sections out of the fragments, and drops the
   ` - unreleased` suffix. A fragment keeps its `## experimental/ksa` section for the KSA release,
   and is deleted once it is empty. Reorder and edit the merged entries by hand, then commit
   everything as one commit, the fold commit. Components without user-facing changes get no
   section and are omitted from the release notes.
3. `tools/release/02-bump-version.py` - sets every train's version in `config.bzl` to the next
   version to work towards, and commits.
4. Push the branch, open the pull request, and run every CI job on it by hand:
   `gh workflow run ci.yml --ref release-vx.y.z`. The pull request and the merge queue skip the
   daily jobs, such as Windows and the CMake and vcpkg builds. Add the pull request to the merge
   queue once its checks and the manual run pass. A pull request merged in the meantime lands its
   fragment on the next version.
5. Once it merges, check out the fold commit, the parent of the bump commit:
   `git checkout <bump commit>^`. The remaining steps run from there.
6. `tools/release/10-preflight.py` - checks the release can start, and publishes nothing, so run
   it as often as you like:
   * The tree is clean, the commit is on a release branch of `origin`, and the changelogs are
     folded. It also reports which components have changelog sections for the new version.
   * `bazel`, `git` and `gh` are installed. A missing publish tool, such as `npm`, or a missing
     `VCPKG_ROOT`, is a warning, as only its own step needs it.
   * Every credential is present. It logs in to GitHub, luarocks.org, AWS, the Central Portal,
     npm and SpaceDock, and imports the GPG key. The PyPI and nuget.org keys get a format check
     only, as an upload is their only test.
   * The version is not already on npm or Maven Central, where it cannot be published again.
7. `tools/release/20-build-and-test.py` - builds everything from a clean output tree and runs the
   full headless test and lint suites, plus `//doc:linkcheck`, which needs network access. Every
   test runs, with no cached results. Check the build log is clean, with no warnings. Pass
   `--expunge` to also discard the downloaded toolchains.

## 2. Tag

8. `tools/release/30-tag.py` - makes the annotated `vx.x.x` tag on the fold commit and pushes it.
   Pushing the version tag also triggers the docs workflow, which freezes the documentation website
   under `/<version>/` and adds the release to the version dropdown. No manual docs step is needed.

## 3. Release on GitHub

9. `tools/release/40-build-assets.py` - builds the release assets and collects them in `assets/`:
   the mod, each client's package and the krpctools wheel. GitHub records the SHA-256 digest of
   each asset.
10. `tools/release/50-release-github.py` - creates a **draft** GitHub release for the tag with the
   changelog as release notes and the assets attached. The release is not marked as latest, as the
   KSA mod releases from the same repository. Review the draft on GitHub and publish it from there.

## 4. Release the mod, clients and tools

Each script releases one package, and they can be run in any order, except that the npm approval
follows the npm staging. The Python, krpctools, C#, Lua, Java and JavaScript scripts build and
re-test the component first. Each script prompts
before publishing. Every step needs the whole credentials file, even the keys it does not use, so
a release with a missing key stops before it publishes anything.

11. `tools/release/60-release-python.py` - Python client to PyPI.
12. `tools/release/61-release-krpctools.py` - krpctools to PyPI.
13. `tools/release/62-release-nuget.py` - C# client to nuget.org.
14. `tools/release/63-release-arduino.py` - updates and tags the `krpc-arduino` repository.
15. `tools/release/64-release-lua.py` - Lua client archive to S3 and the rockspec to luarocks.org.
16. `tools/release/65-release-maven.py` - Java client to Maven Central.
17. `tools/release/66-release-vcpkg-cpp.py` - C++ client port to `microsoft/vcpkg`.
18. `tools/release/67-release-vcpkg-cnano.py` - C-nano client port to `microsoft/vcpkg`.
19. `tools/release/68-release-ckan.py` - confirms the mod reached CKAN.
20. `tools/release/69-stage-npm.py` - stages the JavaScript client on npm.
21. `tools/release/70-approve-npm.py` - publishes the staged JavaScript client.
22. `tools/release/71-release-spacedock.py` - the mod to SpaceDock, emailing its followers.

Two steps are done by hand:

23. Upload `assets/krpc-x.x.x.zip` to
    [CurseForge](https://www.curseforge.com/kerbal/ksp-mods/krpc-control-the-game-using-c-c-java-lua-python)
    by hand, linking the changelog at `https://krpc.github.io/krpc/<version>/changelog.html`.
24. Bump the version number on [KSP-AVC online](https://ksp-avc.cybutek.net/).

The Arduino step builds the C-nano client and lays it out as an Arduino library. It then pushes
the library and tags it, and the Arduino Library Manager indexes the tag. No CI job compiles the
library, so at the push prompt compile an example against the library the step prints:

```bash
sketch=$(mktemp -d)/ConnectingArduino
mkdir "$sketch"
cp doc/src/scripts/cnano/ConnectingArduino.ino "$sketch"
ARDUINO_DIRECTORIES_USER=$(mktemp -d) arduino-cli compile \
  -b arduino:avr:uno --library <printed path> "$sketch"
```

arduino-cli needs the sketch in a folder of the same name. The empty user directory hides any
kRPC library installed in `~/Arduino/libraries`.

The Maven Central step uploads the signed bundle to the Central Portal, which validates it without
publishing. The step publishes it only once validation passes and you confirm. A version on Maven
Central cannot be removed or replaced. Declining drops the upload, so the step can be re-run. The
step returns once publishing starts, and prints the Central Portal page to check it finishes.

The two vcpkg steps hash the archive attached to the published GitHub release and open a pull
request to `microsoft/vcpkg` from your fork, so run them after step 10's release has been published,
not just drafted.

CKAN indexes kRPC automatically from the published GitHub release, so unlike the others this step
publishes nothing: it waits for the NetKAN bot's generated metadata to appear in
`KSP-CKAN/CKAN-meta` and reports it, so the CKAN channel is a definite done rather than something to
check back on later. It too needs step 10's release published, not just drafted. If the metadata has
not landed within the wait (inflation can lag by a few hours), re-run the step later to re-check.

The npm steps publish the package as `@krpc-org/krpc`, so the release account must be a member of
the `@krpc-org` organization on npm. They need npm 11.15.0 or later, which preflight checks. The
first step stages the package with the token. npm then reviews the staged package for a few
minutes, and shows no progress. The second step approves the package with `npm stage approve`,
which asks for your 2FA. A 409 error from npm means the review is still running, so wait and
re-run the step. A version on npm cannot be published again, even after it is unpublished.

SpaceDock has no API keys, so its step logs in with the account's username and password. The email
to followers cannot be recalled, and a version can only be deleted by hand on the site. The step
uploads `assets/krpc-x.x.x.zip`, so it needs step 9's assets. It links the changelog page of the
docs, so it checks that the docs workflow of the tag has published that page.

## 5. Announce the release

25. `tools/release/72-announcements.py` - prints the checklist of announcements:
    * Update the forum release thread, and post an update notice to it.
    * Post on Discord and Reddit.

## Rebuilding the docs of a released version

`tools/release/publish-docs.py` rebuilds the frozen docs of a released version, for a fix made
after the release. The fix is a commit on a `v<version>-docs` branch off the tag. The script
builds that content with the current toolchain and theme. With `--publish DIR`, it stages the
result into the `gh-pages` checkout `DIR` and commits, for you to push. `--help` lists its other
options.

## Adding a changelog component

A new component goes in `COMPONENTS` in `tools/release/changelog.py`, and its `CHANGELOG.md` in
`//doc:changelog`. A service's `CHANGELOG.md` reaches `//doc:changelog` through the registry. The
docs page and the release notes take their component names and order from `COMPONENTS`.

The pull request that adds a component writes its entries in the new `CHANGELOG.md`, under a
`## [vX.Y.Z] - unreleased` header, in place of a fragment. No other pull request edits that file,
so it cannot conflict.
