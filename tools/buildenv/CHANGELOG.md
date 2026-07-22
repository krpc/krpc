## [3.9.1] - unreleased
- Add `python-is-python3`, providing a `python` executable for the CI
  version-stamping step in the build-setup action

## [3.9.0]
- Replace mono with a system .NET SDK (the `KRPC.sln` consumer build now uses
  `dotnet build`)
- Remove dotnet SDK/runtime (`rules_dotnet` is hermetic)
- Remove `cppcheck` (replaced by `clang-tidy`), `maven` and `openjdk-11-jdk`
  (`rules_jvm_external` + `remotejdk_11`)
- Remove `python3` dev/venv and `python-is-python3` (hermetic `rules_python`)
- Install system LLVM (`clang-20` from apt.llvm.org) and build the C/C++ clients
  against it in CI (`--config=system-llvm`) instead of downloading the ~2 GB
  hermetic LLVM release; dev keeps the hermetic toolchain by default
- Cache Bazel's downloaded archives (a small `--repository_cache`) between CI jobs
  instead of the huge extracted output base, to stay under the GitHub Actions
  10 GB cache cap

## [3.8.1]
- Add `graphviz` for dot graph generation in documentation

## [3.8.0]
- Add system-installed nanopb for C-nano client testing
- Remove autotools

## [3.7.1]
- Add `ccache`

## [3.7.0]
- Add system-installed protobuf and ASIO for C++ client testing

## [3.6.0]
- Update bazel to 9.1.1

## [3.5.0]
- Update base image to Ubuntu 24.04
- Update to Python 3.12
- Update bazel to 7.2.1
- Update to dotnet 10

## [3.4.3]
- Run with github actions uid and gid
- Fix `buildifier` not installed for all users

## [3.4.2]
- Remove `--distinct_host_configuration` from `bazelrc`

## [3.4.1]
- Update `buildifier` to 7.1.2

## [3.4.0]
- Update to Bazel 7.2.1

## [3.3.0]
- Update to Bazel 6.5.0

## [3.2.0]
- Update to Bazel 6.1.1
- Remove ksp libs (can now be obtained from ksp-lib repository)
- Remove `gendarme`

## [3.1.1]
- Add `buildifier`

## [3.1.0]
- Update to Bazel 6.1.0
- Add dotnet

## [3.0.1]
- Add `cmake`

## [3.0.0]
- Clean up to only include necessary dependencies
- Remove bazel caching (done using github actions caching instead)
- Remove costly user management
- Add `gendarme` tool
- Update bazel config to allow testing in parallel
- Update bazel config to do less verbose logging

## [2.6.0]
- Update base image to Ubuntu 22.04
- Update to Bazel 6.0.0
- Update to KSP 1.12.5

## [2.5.1]
- Update to Bazel 3.7.0

## [2.5.0]
- Update to KSP 1.10.1

## [2.4.0]
- Update to KSP 1.9.0

## [2.3.0]
- Update build tools to use python 3
- Update to Bazel 0.28.1

## [2.2.0]
- Update to KSP 1.7.3

## [2.1.2]
- Add `latexmk`

## [2.1.1]
- Remove Bazel workarounds

## [2.1.0]
- Update to Bazel 0.28.0

## [2.0.0]
- Move to Ubuntu 18.04

## [1.12.0]
- Update to KSP 1.5.1
- Update to Bazel 0.18.0

## [1.11.0]
- Update to KSP 1.4.5
- Update to Bazel 0.16.1

## [1.10.0]
- Update to KSP 1.4.3
- Update to Bazel 0.15.2

## [1.9.0]
- Update to Bazel 0.13.0
- Remove protobuf and asio (no longer required by build scripts)

## [1.8.0]
- Update to KSP 1.4.3
- Update to Bazel 0.12.0

## [1.7.0]
- Update to KSP 1.4.2

## [1.6.0]
- Update to KSP 1.4.1

## [1.5.0]
- Update to KSP 1.4.0
- Update the Bazel 0.11.1
- Update to protobuf v3.5.1

## [1.4.0]
- Update to Bazel 0.7.0
- Add `socat` for SerialIO tests

## [1.3.0]
- Update to KSP 1.3.1
- Update to Bazel 0.6.1
- Update to protobuf v3.4.1
- Update Mono to use Ubuntu 16.04 repository
- Add dependencies for running lint tests

## [1.2.0]
- Update to Bazel 0.5.4
- Update to protobuf v3.4.0

## [1.1.0]
- Update to KSP 1.3
- Update to Bazel 0.5.0
- Update to protobuf v3.3.0

## [1.0.0]
- Initial version.
