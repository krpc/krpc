#!/bin/bash

# Requires twine:
#   sudo pip install twine

set -e

TWINE=twine
VERSION=`tools/krpc-version.sh`

bazel build //tools/krpctools:krpctools

$TWINE upload bazel-bin/tools/krpctools/krpctools-$VERSION.zip
