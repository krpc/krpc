#!/bin/bash

# Usage tools/serve-docs.sh [PORT]

set -e

port=${1:-8080}
src=bazel-genfiles/doc/srcs
env=bazel-bin/doc/serve/env
out=bazel-bin/doc/serve/out

# Build the doc sources
bazel fetch //...
bazel build //doc:srcs

# Set up python environment
mkdir -p `dirname $env`
if [ ! -d "$env" ]; then
  virtualenv $env
  source $env/bin/activate
  pip install \
    sphinx==1.3.5 sphinx-autobuild sphinx_rtd_theme sphinxcontrib_spelling sphinx-csharp sphinx-tabs \
    https://github.com/djungelorm/sphinx-lua/releases/download/0.1.2/sphinx-lua-0.1.2.tar.gz \
    javasphinx pyinotify
else
  source $env/bin/activate
fi

# Auto-serve and auto-build the docs
python tools/do-serve-docs.py $port $src $out
