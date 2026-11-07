# CI Guide

How the GitHub Actions setup works, and why. The workflows are in `.github/workflows/`, the
composite actions they share in `.github/actions/`, and the Bazel settings CI uses in the `ci`
configs of `.bazelrc`.

## Workflows

| Workflow | Runs on | Does |
| --- | --- | --- |
| `ci` | pull request, merge queue, push to `main`, manual | Builds and tests everything, and keeps the caches |
| `flaky-tests` | completion of a `ci` run | Comments on the pull request when a test passed only on a retry |
| `changelog-hints` | pull request (`pull_request_target`) | Comments on the pull request about its changelog fragment |
| `docs` | push to `main`, `v*` tag | Publishes the documentation site to `gh-pages` |

## The Merge Queue

A pull request merges through the merge queue. The queue tests the tree `main` will have after
the merge, so two pull requests that pass on their own cannot break `main` together. It uses the
merge commit method, and requires a single check, `all-checks-passed`. "Require branches to be
up to date" is off, as the queue already tests each pull request on the latest `main`.

`all-checks-passed` needs every other job in `ci` and fails if any of them failed or was
cancelled. One required check means the branch protection settings never change when a job is
added or renamed. A new job goes in its `needs` list, or it gates nothing.

The queue fast-forwards `main` to the commit its run tested. So the push run that follows skips
the build and test jobs, and only refreshes the caches that later runs restore (see
[Caches](#caches)). `copy-artifacts` attaches the merge queue run's artifacts to the push run,
so `main` still lists its builds. Its list of artifacts must match what the jobs upload. The
`merge-group-run` action finds the merge queue run for the pushed commit, and waits for it to
finish. A direct push to `main` has no merge queue run and builds nothing.

A failed job removes the pull request from the queue once `all-checks-passed` reports. Failed
jobs cannot be re-run into the same queue entry, and queueing the pull request again starts a
new run. This is why CI retries the steps that fail for reasons outside the change (see
[Flakes](#flakes)).

## Jobs

### One Job per Thing Built or Tested

Each component is built and tested in a job of its own, on Linux and on Windows. A broken
component then shows as the job that failed, and the jobs run in parallel. Jobs that differ only
in their Bazel targets and artifact are entries of the `linux` and `windows` matrices, a few
lines each. A job stays outside a matrix when it produces more than one artifact, runs something
other than Bazel, saves a cache on a push, or is named in another job's `needs`, which cannot
address a single matrix entry.

Windows runs everything Linux runs, except these:

- **The serial transport tests.** They link two pseudo-terminals with socat, which has no Windows
  build. Windows tests its serial backend against a stand-in, in
  `//client/cnano:communication-serialio`.
- **clang-tidy.** It needs the libc++ headers that only the LLVM toolchain supplies.
- **`//tools/build/python:requirements.test`.** The Python lock file is resolved on Linux.
- **The C++ CMake fetch scenario.** It builds protobuf and abseil from source, and on Windows
  that build is uncached and takes the job past 30 minutes. Windows builds the system scenario,
  which shows the client compiles there.

Releases are built on Linux, so only the Linux jobs upload artifacts.

### Linux

The Linux Bazel jobs run in the `ghcr.io/krpc/buildenv` image (`tools/buildenv/`), pinned by
version in each job. The image holds socat for the serial transport tests, and an LLVM install
that the Linux jobs use in place of Bazel's own (see [Bazel Configuration](#bazel-configuration)).
The .NET SDK for the `KRPC.sln` build comes from `actions/setup-dotnet`. The container runs as
the `runner` user, so it owns the files the checkout writes.

The CMake and vcpkg consumer jobs run on the bare runner. They need only a compiler and the
packages the `vcpkg-linux` action installs.

### Windows

The Windows jobs use the tools the runner image ships: MSVC, Bazelisk and Git Bash. The
`bazel-fetch-windows` action writes the CI config to the system-wide `bazel.bazelrc`, with the
output root at `D:/b`. The default root, under the runner's profile, puts DLLs in the
sphinx-build runfiles past `MAX_PATH`. The action also sets `MSYS2_ARG_CONV_EXCL=*`, as Git
Bash otherwise rewrites `//pkg:target` patterns as paths.
The jobs build in `dbg` mode, whose `/DEBUG:FULL` link flag avoids the LNK4315 warning rules_cc
causes in fastbuild.

### Limits

The workflow token is read-only. A new push to a pull request cancels the run it superseded.
Every job times out after 30 minutes, about twice the longest job on a cache miss, so a hung job
gives its runner back instead of holding it for six hours.

### Version Stamping

The `build-setup` action stamps `config.bzl` with `x.y.z-<count>-g<sha>`, where the count is the
number of commits since the last release tag. The count orders dev builds and stays inside the
16-bit build field of a .NET assembly version. The checkout is shallow, so the action deepens it
first, fetching commits but no trees or blobs. The `g` prefix stops Python packaging reading an
all-digit sha as a number.

### The Tool Packages

`test-tool-packages` installs the krpctools and Python client wheels with pip, on bare Linux
and Windows runners with Python and .NET but no Bazel. `tools/ci/smoke_test_tools.py` then runs
each krpctools command once, as a user would. It checks what the Bazel tests cannot: the package
installs, its entry points run, and the bundled .NET tools start on the runner's dotnet host.

### The Solution Build

`linux-build-csharp-sln` and `windows-build-csharp-sln` build `KRPC.sln` with `dotnet`, from the
generated files `linux-build-genfiles` uploads. Bazel globs the C# sources, but the `.csproj`
files list them, so they also run `//:csproj-test`. It catches a file missing from a project,
which a compile cannot always see.

## Bazel Configuration

The `bazel-fetch` actions turn on `ci-linux` or `ci-windows`, from a user or system rc file, so
every later Bazel command in a job uses it. Both include `ci`. Keeping the settings in
`.bazelrc` lets a developer reproduce CI with `--config=ci-linux`.

- **Sandboxed.** CI builds in the processwrapper sandbox, as a developer does, so an action
  reading an undeclared file fails in CI too. The container blocks the user namespaces
  the Linux sandbox needs.
- **Every test runs.** `--nocache_test_results` runs a test even when the disk cache holds its
  result, so CI keeps catching flaky tests.
- **System LLVM on Linux.** `ci-linux` builds with the buildenv image's LLVM in place of the
  hermetic LLVM release Bazel would download. That release is about 2 GB, and GitHub keeps
  10 GB of caches per repository, so CI stores LLVM in the image instead of the download cache.
- **No `parse_headers`.** Abseil compiles each of its headers on its own as a check. That is a
  third of the compile actions a client's conformance testee needs.
- **Download retries.** A failed download is retried for about three and a half minutes, at most
  15 seconds apart. Bazel's default gives up after about 13 seconds, which a GitHub release
  download returning 500 can outlast.
- **Conformance retries.** A failing client conformance target runs up to three times (see
  [Flakes](#flakes)).

## Caches

GitHub evicts a cache unused for 7 days, and the least recently used ones over the repository's
10 GB. A cache saved by a pull request run is visible only to that pull request, and one saved
by a merge queue run is lost with the queue's branch. So the caches later runs restore are saved
by the push run on `main`, which builds only to fill a missing cache.

GitHub updates its runner images often, the Windows image about once a week. A cache keyed on
the image version would then go stale every week. A vcpkg cache miss rebuilds protobuf from
source, which takes 15 to 20 minutes on Windows. So no cache is keyed on the image version alone.
Each is keyed on what its contents depend on, and an image update forces a rebuild only when it
changes one of those, such as the compiler.

| Cache | Key | Saved by |
| --- | --- | --- |
| Bazel downloads | OS, Bazel version, `MODULE.bazel` and the Python lock file | Every job that misses it |
| Bazelisk | OS and `.bazelversion` | Every job that misses it |
| Third-party C++ libraries | The libraries' Bazel actions | `linux-test-client-cpp`, `windows-test-client-cpp` |
| vcpkg binaries | Package set, runner image and ABI hashes | The vcpkg actions |
| ccache | Compiler, CMake template and vcpkg ABI hashes | The CMake consumer jobs |

### Bazel Downloads

The `bazel-fetch` actions cache the archives in the `content_addressable` part of Bazel's
repository cache, and leave out the extracted repositories beside it, which are larger and
rebuilt per job. A dependency change misses the exact key, and restores the newest cache for the
same OS and Bazel version, so the fetch downloads only the archives that changed. Bazel updates
the mtime of each archive it reads, so the fetch step then drops the archives older than the
fetch, which keeps the cache from growing. Bazelisk's download of Bazel itself is cached apart.

### Third-Party C++ Libraries

The conformance testees use protobuf's JSON support, which compiles protobuf, abseil and zlib
twice, once for the protoc plugin that generates the well-known types. The `bazel-disk-cache`
action keeps a Bazel disk cache of those libraries, keyed on a hash of their `aquery` actions,
which changes with their versions, the build flags and, on Windows, the MSVC paths. The C++ test
jobs save it, and the C-nano, benchmark and doc example jobs restore it. Without it, compiling
these libraries is most of a C++ job's time.

### vcpkg

vcpkg names each archive in its binary cache after the ABI hash of the package, and rebuilds a
package whose archive is missing. The ABI hashes cover the inputs of a package's build,
including tools the runner image ships, such as the compiler, CMake and PowerShell. Most image
updates change none of them.

A job first restores the newest cache built on its own image, in a step of its own. Only if that
misses does it restore the newest cache for its packages from any image. A single restore step
would match a pull request's cache from another image first. So a job on a new image restores
the previous image's cache, and rebuilds only the packages whose ABI hashes changed, often none.
The saved cache is keyed on the image as well as the ABI hashes. During a gradual image rollout,
the jobs on the old and new images then each keep a cache, in place of replacing each other's.

Each package's `vcpkg_abi_info.txt` is logged in a collapsed group, so diffing two runs shows
why a package rebuilt.

### ccache

The CMake consumer jobs compile through ccache with `CCACHE_COMPILERCHECK=content`, so a new
runner image with the same compiler still hits.

## Flakes

The merge queue turns any failed job into a new full run, so CI hardens what can fail for
reasons outside the change.

- **Downloads.** Bazel retries downloads for longer, and the download cache falls back to its
  newest version, as above. apt retries a failed package download in the CMake jobs.
- **API calls.** `merge-group-run` retries a failed run listing until its 15-minute deadline.
  The CI scripts call GitHub through `tools/ci/gh_cli.py`, which retries a failed call.
- **Conformance tests.** A flaky conformance test should not stop a pull request with an
  unrelated change from merging. So a failing conformance target runs up to three times, and
  other tests still fail on their first failure.

A known flaky test is marked `@flaky` in the client's `expected-failures.txt`, which lets it pass
or fail. A test that fails and then passes on a retry is an unknown flake. It leaves the run
green, so CI tells the maintainer, who then marks or fixes it:

1. The `ci` config writes Bazel's build events to `bazel-test-events.json`, and the conformance
   driver writes JUnit XML. After the tests, each job runs the `flaky-tests` action, which runs
   `tools/ci/flaky_tests.py report`. For each target whose build events mark it `FLAKY`, it
   reads the JUnit XML of the failed attempts. It annotates the run with the tests that failed,
   and lists them in the job summary. It uploads the same as a `flaky-tests-*` artifact, named
   after the job.
2. The `flaky-tests` workflow runs when a `ci` run completes. If the run has such artifacts, it
   comments on its pull request, mentioning the maintainer, which sends an email. The comment
   lists the flaky tests under each job, with a link to the job's log. It finds the
   pull request from the merge queue branch name, or from the pull request run itself. A fork's
   run names no pull request, so it looks the pull request up by the run's head commit, then
   takes the open pull request of that branch.
   A re-run of the same `ci` run edits that comment.

`flaky-tests` runs on `workflow_run`, as the token of a fork's pull request run cannot comment.
It runs the workflow file and the code of `main`, never the pull request's. The artifact of a
fork's run is untrusted, so it is read as data, with a size limit, and each field is kept inside
a code span, so it cannot mention anyone or add markdown.

## Changelog Hints

`changelog-hints` posts one comment listing the problems with a pull request's changelog
fragment, and edits it as the pull request changes. It runs on `pull_request_target` for the
same reason as `flaky-tests`, reads the pull request through the API, and runs none of its code.
A hint never fails the check, and the check is not required.

## Documentation Site

`docs` publishes into the `gh-pages` branch, one directory per version. A push to `main`
refreshes `dev/`, and a `v*` tag writes `<version>/` once. `doc/gen_docs_index.py` then mirrors
the newest release into `latest/`, and regenerates the version switcher and the root redirect.
Runs share one concurrency group and never cancel each other, as a release moves a tag and
`main` together and both push to `gh-pages`.

## Changing CI

- A new job goes in the `needs` of `all-checks-passed`. A new artifact goes in the list in
  `copy-artifacts`.
- A cache that later runs restore must be saved by the push run on `main`.
- `pull_request_target` and `workflow_run` workflows run from `main`, so `changelog-hints` and
  `flaky-tests` cannot be tested by the pull request that changes them. Test them after merge.
- The push run's cache jobs and `copy-artifacts` also run only after merge.
- `//tools/ci:test` tests the CI scripts, and runs in `test-build-scripts`.
