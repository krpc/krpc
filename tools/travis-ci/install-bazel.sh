#!/bin/bash
set -ev

VERSION=0.4.4
HASH=c0b4df929bc2e1e12fb4ce72961cb77901cf6f594a789e195ea50f7c8673ddea

DEB=bazel_$VERSION-linux-x86_64.deb
wget https://github.com/bazelbuild/bazel/releases/download/$VERSION/$DEB
echo "$HASH  $DEB" > ./$DEB.sha256
sha256sum -c ./$DEB.sha256
rm $DEB.sha256
sudo dpkg -i ./bazel_$VERSION-linux-x86_64.deb
rm $DEB
cat tools/travis-ci/bazelrc >> .bazelrc
