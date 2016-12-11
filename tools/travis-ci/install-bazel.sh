#!/bin/bash
set -ev

VERSION=0.4.2
HASH=de12abbf8bf1b5ec5f7676afb32019e10e144fe986fb170ebb7d976bb2229539

DEB=bazel_$VERSION-linux-x86_64.deb
wget https://github.com/bazelbuild/bazel/releases/download/$VERSION/$DEB
echo "$HASH  $DEB" > ./$DEB.sha256
sha256sum -c ./$DEB.sha256
rm $DEB.sha256
sudo dpkg -i ./bazel_$VERSION-linux-x86_64.deb
rm $DEB
cat tools/travis-ci/bazelrc >> .bazelrc
