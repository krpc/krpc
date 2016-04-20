#!/bin/bash

# Requires twine:
#   sudo pip install twine

set -e

TWINE=twine
VERSION=`tools/krpc-version.sh`

bazel build //tools/clientgen:krpc.clientgen

$TWINE upload bazel-bin/tools/clientgen/krpc.clientgen-$VERSION.zip
