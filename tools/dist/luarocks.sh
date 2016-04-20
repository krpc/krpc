#!/bin/bash

set -e

LUAROCKS=luarocks
VERSION=`tools/krpc-version.sh`

bazel build //client/lua
bazel test //client/lua:test --cache_test_results=no

$LUAROCKS upload --skip-pack bazel-bin/client/lua/krpc-$VERSION-0.rockspec
