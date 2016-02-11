#!/bin/bash

# Requires twine:
#   sudo pip install twine

set -e

TWINE=twine
VERSION=`tools/get-version.sh`

bazel build //tools/clientgen

$TWINE upload bazel-bin/tools/clientgen/krpc.clientgen-$VERSION.zip
