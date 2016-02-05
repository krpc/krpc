#!/bin/bash

# Usage tools/serve-docs.sh PORT

set -e

port=$1

bazel build //doc:html
rm -rf docs
unzip -q bazel-bin/doc/html.zip -d docs
cd docs; python -m SimpleHTTPServer $port
