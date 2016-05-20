#!/bin/bash
set -ev
VERSION=0.2.3
wget https://github.com/bazelbuild/bazel/releases/download/$VERSION/bazel-$VERSION-jdk7-installer-linux-x86_64.sh
chmod +x bazel-$VERSION-jdk7-installer-linux-x86_64.sh
./bazel-$VERSION-jdk7-installer-linux-x86_64.sh --user
rm ./bazel-$VERSION-jdk7-installer-linux-x86_64.sh
cat tools/travis-ci/bazelrc >> .bazelrc
