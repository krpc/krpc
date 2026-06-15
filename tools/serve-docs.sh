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
  python3 -m venv $env
  $env/bin/pip install --upgrade \
      "jinja2==3.1.6" \
      "markupsafe==3.0.3" \
      "Sphinx==9.1.0" \
      "sphinx_rtd_theme==3.1.0" \
      "sphinxcontrib_spelling==8.0.2" \
      "sphinxcontrib-jquery==4.1" \
      "sphinx-csharp==0.1.8" \
      "sphinx-tabs==3.5.0" \
      "sphinxcontrib-luadomain==1.1.2" \
      "https://krpc.s3.amazonaws.com/lib/javasphinx/javalang-0.13.1.tar.gz" \
      "https://krpc.s3.amazonaws.com/lib/javasphinx/javasphinx-0.9.16.tar.gz" \
      "sphinx-autobuild==2024.10.3" \
      "watchdog"
fi

# Auto-serve and auto-build the docs
source $env/bin/activate
python tools/do-serve-docs.py $port $src $stage $out
