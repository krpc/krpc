#!/bin/bash

# Usage tools/serve-docs.sh PORT

set -e

port=${1:-8080}
dir=bazel-bin/doc/html
bazel build //doc:html
rm -rf $dir
unzip -q bazel-bin/doc/html.zip -d $dir
(cd $dir; python -m SimpleHTTPServer $port)
