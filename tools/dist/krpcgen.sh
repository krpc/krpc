#!/bin/bash

# Requires twine:
#   sudo pip install twine

set -e

TWINE=twine
VERSION=`tools/get-version.sh`

bazel build //tools/krpcgen

$TWINE upload bazel-bin/tools/krpcgen/krpcgen-$VERSION.zip
