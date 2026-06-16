#!/bin/bash
# Collects all files for uploading to GitHub release assets in
# directory "assets"

set -ev

VERSION=`tools/krpc-version.sh`

bazel build //...
tools/dist/genfiles.sh

rm -rf assets
mkdir -p assets

cp bazel-bin/krpc-$VERSION.zip ./assets/
cp bazel-bin/client/cnano/krpc-cnano-$VERSION.zip ./assets/
cp bazel-bin/client/cpp/krpc-cpp-$VERSION.zip ./assets/
cp bazel-bin/client/csharp/krpc-csharp-$VERSION.zip ./assets/
cp bazel-bin/client/java/krpc-java-$VERSION.jar ./assets/
cp bazel-bin/client/lua/krpc-lua-$VERSION.zip ./assets/
cp bazel-bin/client/python/krpc-python-$VERSION.zip ./assets/
cp bazel-bin/doc/krpc-doc-$VERSION.pdf ./assets/
cp bazel-bin/tools/krpctools/krpctools-$VERSION.zip ./assets/
cp bazel-bin/tools/TestServer/TestServer-$VERSION.zip ./assets/
cp bazel-bin/krpc-genfiles-$VERSION.zip ./assets/
