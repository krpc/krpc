#!/bin/bash
set -ev

VERSION=0.4.4
HASH=4d950b6833ab90d54273743bd0fd6988016f95e6afbf9775ab8c24a9862bdf52

DEB=bazel_$VERSION-linux-x86_64.deb
wget https://github.com/bazelbuild/bazel/releases/download/$VERSION/$DEB
echo "$HASH  $DEB" > ./$DEB.sha256
sha256sum -c ./$DEB.sha256
rm $DEB.sha256
sudo dpkg -i ./bazel_$VERSION-linux-x86_64.deb
rm $DEB
cat tools/travis-ci/bazelrc >> .bazelrc
