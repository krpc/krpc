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
exception: pushes to `krpc/krpc` and `krpc-arduino` use your normal git setup.

## 1. Prepare

1. Bump `version` in `config.bzl`, make sure every component with user-facing changes has a
   `vx.x.x` section in its `CHANGES.txt` (components without user-facing changes get no section
   and are omitted from the release notes), and commit and push to `main`.
2. `tools/release/10-preflight.py` — checks the tree is clean and in sync with `origin/main`,
   reports which components have changelog sections for the new version, and checks the needed
   tools are installed and every credential is present. Read-only, so run it as often as you like.
3. `tools/release/20-build-and-test.py` — builds everything and runs the full headless test and
   lint suites. Pass `--expunge` to start from a pristine Bazel state.

## 2. Tag

4. `tools/release/30-tag.py` — makes the annotated `vx.x.x` tag, moves the `latest-version` tag,
   and pushes the branch and both tags. Pushing the version tag also triggers the docs workflow,
   which freezes the documentation website under `/<version>/` and adds the release to the version
   dropdown — no manual docs step is needed.
5. Wait for the CI workflow on `main` to pass (the tag script prints a `gh run watch` command).

## 3. Release on GitHub

6. `tools/release/40-build-assets.py` — builds all the release archives and collects them in
   `assets/`, along with a SHA256 checksum file.
7. `tools/release/50-release-github.py` — creates a **draft** GitHub release for the tag with the
   changelog as release notes and the assets attached. Review the draft on GitHub and publish it
   from there.

## 4. Release the clients and tools

Each of these publishes one package, and they can be run in any order. Each builds and re-tests
the component first, and prompts before uploading.

8. `tools/release/60-release-python.py` — Python client to PyPI.
9. `tools/release/61-release-krpctools.py` — krpctools to PyPI.
10. `tools/release/62-release-nuget.py` — C# client to nuget.org.
11. `tools/release/63-release-arduino.py` — updates and tags the `krpc-arduino` repository.
12. `tools/release/64-release-lua.py` — Lua client archive to S3 and the rockspec to luarocks.org.
13. `tools/release/65-release-docker.py` — TestServer image to ghcr.io.

## 5. Publish the mod and announce

14. `tools/release/70-announcements.py` — prints the changelog formatted for the mod-hosting
    sites and the checklist of remaining manual steps:
    * Upload `assets/krpc-x.x.x.zip` to CurseForge and SpaceDock, with the changelog.
    * Bump the version number on [KSP-AVC online](https://ksp-avc.cybutek.net/).
    * Update the forum release and development threads, and post an update notice to the release
      thread.
    * Post on Discord and Reddit.
