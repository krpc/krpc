#!/bin/bash
set -ev

VERSION=0.4.1
HASH=a2ea9a5789713d79f439c54a93177e58136cd622fdcad8d2906d3448303bd937

DEB=bazel_$VERSION-linux-x86_64.deb
wget https://github.com/bazelbuild/bazel/releases/download/$VERSION/$DEB
echo "$HASH  $DEB" > ./$DEB.sha256
sha256sum -c ./$DEB.sha256
rm $DEB.sha256
sudo dpkg -i ./bazel_$VERSION-linux-x86_64.deb
rm $DEB
cat tools/travis-ci/bazelrc >> .bazelrc
