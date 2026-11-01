# Release guide for kRPC

A release is a sequence of small Python scripts in `tools/release/`, run in numbered order, with a
few manual steps in between. Each script does one step, can be re-run if something fails, and asks
for confirmation before publishing anything externally. They can be run from any directory; they
locate the repository root themselves. They need nothing beyond a Python 3.12 interpreter and the
tools of the step being run.

All the credentials live in one git-ignored TOML file at the root of the repository. Copy
`tools/release/release-credentials.toml.template` to `release-credentials.toml`, fill in every value, and `chmod 600` it; the template says what each one is and where to get it. The scripts pass these to `gh`, `twine`,
`dotnet`, `luarocks`, `aws` and `docker` explicitly, so none of those tools needs to be configured
or logged in beforehand, and whatever account they are already set up with is ignored. Git is the
exception: pushes to `krpc/krpc`, `krpc-arduino` and your vcpkg fork use your normal git setup.

The two vcpkg steps also need a git checkout of vcpkg pointed to by `VCPKG_ROOT` (the same one the
client vcpkg test scripts use) and a fork of `microsoft/vcpkg` on the release account; `gh` creates
the fork the first time if it does not exist.

## 1. Prepare

A release preparation pull request takes `main` from the version being released to the next one.
`version` in `config.bzl` names the version being released.

1. Create a branch off `main`, such as `release-vx.y.z`.
2. `tools/release/01-fold-changelogs.py` - writes the fragments in `changelog.d/` into each
   component's `CHANGELOG.md`, deletes them, and drops the ` - unreleased` suffix. Reorder and edit
   the merged entries by hand, then commit everything as one commit, the fold commit. Components
   without user-facing changes get no section and are omitted from the release notes.
3. `tools/release/02-bump-version.py` - sets `version` in `config.bzl` to the next version to work
   towards, and commits. Builds from `main` are then stamped as pre-releases of that version.
4. Push the branch, open the pull request, and add it to the merge queue once its checks pass. A
   pull request merged in the meantime lands its fragment on the next version.
5. Once it merges, check out the fold commit, the parent of the bump commit:
   `git checkout <bump commit>^`. The remaining steps run from there.
6. `tools/release/10-preflight.py` - checks the tree is clean, the commit is on `origin/main`, the
   changelogs are folded, reports which components have changelog sections for the new version,
   and checks the needed tools are installed and every credential is present. Read-only, so run it
   as often as you like.
7. `tools/release/20-build-and-test.py` - builds everything and runs the full headless test and
   lint suites. Pass `--expunge` to start from a pristine Bazel state.

## 2. Tag

8. `tools/release/30-tag.py` - makes the annotated `vx.x.x` tag on the fold commit and pushes it.
   Pushing the version tag also triggers the docs workflow, which freezes the documentation website
   under `/<version>/` and adds the release to the version dropdown. No manual docs step is needed.

## 3. Release on GitHub

9. `tools/release/40-build-assets.py` — builds all the release archives and collects them in
   `assets/`, along with a SHA256 checksum file.
10. `tools/release/50-release-github.py` — creates a **draft** GitHub release for the tag with the
   changelog as release notes and the assets attached. Review the draft on GitHub and publish it
   from there.

## 4. Release the clients and tools

Each of these publishes one package, and they can be run in any order. Each builds and re-tests
the component first, and prompts before uploading.

11. `tools/release/60-release-python.py` — Python client to PyPI.
12. `tools/release/61-release-krpctools.py` — krpctools to PyPI.
13. `tools/release/62-release-nuget.py` — C# client to nuget.org.
14. `tools/release/63-release-arduino.py` — updates and tags the `krpc-arduino` repository.
15. `tools/release/64-release-lua.py` — Lua client archive to S3 and the rockspec to luarocks.org.
16. `tools/release/65-release-docker.py` — TestServer image to ghcr.io.
17. `tools/release/66-release-vcpkg-cpp.py` — C++ client port to `microsoft/vcpkg`.
18. `tools/release/67-release-vcpkg-cnano.py` — C-nano client port to `microsoft/vcpkg`.
19. `tools/release/68-release-ckan.py` — confirms the mod reached CKAN.

The two vcpkg steps hash the archive attached to the published GitHub release and open a pull
request to `microsoft/vcpkg` from your fork, so run them after step 10's release has been published,
not just drafted.

CKAN indexes kRPC automatically from the published GitHub release, so unlike the others this step
publishes nothing: it waits for the NetKAN bot's generated metadata to appear in
`KSP-CKAN/CKAN-meta` and reports it, so the CKAN channel is a definite done rather than something to
check back on later. It too needs step 10's release published, not just drafted. If the metadata has
not landed within the wait (inflation can lag by a few hours), re-run the step later to re-check.

## 5. Publish the mod and announce

20. `tools/release/70-announcements.py` — prints the checklist of remaining manual steps:
    * Upload `assets/krpc-x.x.x.zip` to CurseForge and SpaceDock, linking the changelog at
      `https://krpc.github.io/krpc/<version>/changelog.html`.
    * Bump the version number on [KSP-AVC online](https://ksp-avc.cybutek.net/).
    * Update the forum release thread, and post an update notice to it.
    * Post on Discord and Reddit.
