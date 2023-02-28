#!/bin/bash

# Usage tools/serve-docs.sh [PORT]

set -e

port=${1:-8080}
src=bazel-bin/doc/srcs
stage=bazel-bin/doc/srcs-stage
env=bazel-bin/doc/serve/env
out=bazel-bin/doc/serve/out

# Set up bazel
if [ ! -d "bazel-bin" ]; then
    bazel build //:krpc
fi
bazel fetch //...
mkdir -p `dirname $env`

# Set up python environment
if [ ! -d "$env" ]; then
  virtualenv $env --python python3
  source $env/bin/activate
  pip install --upgrade \
      "jinja2==3.1.2" \
      "markupsafe==2.1.2"
  pip install "Sphinx==6.1.3"
  pip install "lxml==4.9.2"
  pip install \
      "sphinx_rtd_theme==1.2.0" \
      "sphinxcontrib_spelling==8.0.0" \
      "sphinx-csharp==0.1.8" \
      "sphinx-tabs==3.4.1" \
      "sphinxcontrib-luadomain==1.1.2" \
      "https://krpc.s3.amazonaws.com/lib/javasphinx/javalang-0.13.1.tar.gz" \
      "https://krpc.s3.amazonaws.com/lib/javasphinx/javasphinx-0.9.16.tar.gz"
  pip install \
      "sphinx-autobuild==2021.3.14" \
      "pyinotify==0.9.6"
else
  source $env/bin/activate
fi

# Auto-serve and auto-build the docs
python tools/do-serve-docs.py $port $src $stage $out
