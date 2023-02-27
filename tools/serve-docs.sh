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
      "six==1.11.0" \
      "pbr==5.11.1" \
      "setuptools==67.4.0" \
      "setuptools-git==1.2" \
      "jinja2==2.10.3" \
      "markupsafe==1.1.1"
  pip install "Sphinx==1.8.1"
  pip install "lxml==4.9.2"
  pip install \
      "sphinx_rtd_theme==0.4.2" \
      "sphinxcontrib_spelling==4.2.0" \
      "sphinx-csharp==0.1.6" \
      "sphinx-tabs==1.1.12" \
      "javasphinx==0.9.15" \
      "sphinxcontrib-luadomain==1.1.2"
  pip install sphinx-autobuild pyinotify
else
  source $env/bin/activate
fi

# Auto-serve and auto-build the docs
python tools/do-serve-docs.py $port $src $stage $out
