# Release guide for kRPC

This document details the steps necessary to make a release of kRPC.

1. The repository should be clean from changes. Also run a `bazel clean --expunge`.
1. Bump the kRPC version number in `config.bzl` and commit the file.
1. Make an annotated tag for the new version (using `git tag -a vx.x.x) and move the latest-version
   tag to this commit.
1. Run `tools/build-against-all-versions.sh` to check that the mod at least builds against all
   supported versions of KSP.
1. Build kRPC and run all of the tests locally to check that they pass using
   `bazel build //... && bazel test //:test`
1. Push the vx.x.x commit and tag to the main branch on GitHub using `git push && git push --tags`
1. Wait for the CI workflow to pass.
1. Run `tools/dist/genfiles.sh` to build the genfiles archive to include in the release.
1. Do the release on Github:
   1. Use `tools/dist/changes.py github` to get changelog to include with the release
   1. Upload the release archive, krpctools, genfiles, TestServer, documentation and all clients (11 files in total)
1. Update the documentation website by merging the vx.x.x commit into the docs branch.
   Push it to GitHub. The docs GitHub workflow should automatically build and deploy the new website.
1. Do a release on Curse
1. Do a release on SpaceDock
1. Bump the version number on [KSP AVC online](https://ksp-avc.cybutek.net/)
1. Release all the clients and tools to their various platforms:
   1. Upload the Python client to pypi using twine
   1. Upload krpctools to pypi using twine
   1. Release C# client on nuget.org
   1. Run `tools/update-arduino-library.sh push` to update Arduino library repository and then run
      `tools/update-arduino-library.sh release` to push a new version of Arduino library.
   1. Upload Lua client to `s3://krpc/lua/...` and release the rockspec file on luarocks.org
   1. Build and push docker image for TestServer using the makefile in `tools/TestServer/docker`
1. Post release details to various forums etc:
   1. Update release and dev thread on forums, and post an update notice to the release thread.
   1. Post on Discord
   1. Post on Reddit
