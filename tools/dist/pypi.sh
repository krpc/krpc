#!/bin/bash

# Requires twine:
#   sudo pip install twine

set -e

TWINE=twine
VERSION=`tools/krpc-version.sh`

bazel build //client/python
bazel test //client/python:test --cache_test_results=no

$TWINE upload bazel-bin/client/python/krpc-$VERSION.zip
