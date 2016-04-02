#!/bin/bash
#FIXME: updates the version in config.bzl when doing a travis build. Workaround for bazel not being able to use git describe to get the version
set -e
version=$(git describe --long --always)
echo "version = ${version:1}"
sed -i "s/^version = '.*'$/version = '${version:1}'/g" config.bzl
