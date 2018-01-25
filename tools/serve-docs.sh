#!/bin/bash

# Usage tools/serve-docs.sh [PORT]

set -e

port=${1:-8080}
src=bazel-genfiles/doc/srcs
stage=bazel-genfiles/doc/srcs-stage
env=bazel-bin/doc/serve/env
out=bazel-bin/doc/serve/out

# Set up python environment
bazel fetch //...
mkdir -p `dirname $env`
if [ ! -d "$env" ]; then
  virtualenv $env
  source $env/bin/activate
  pip install --upgrade \
      "six==1.10.0" \
      "pbr==3.1.0" \
      "setuptools==36.0.1" \
      "setuptools-git==1.2"
  pip install "Sphinx==1.6.4"
  CFLAGS="-O0" pip install "lxml==3.8.0"
  pip install \
      "sphinx_rtd_theme==0.2.5b1" \
      "sphinxcontrib_spelling==2.3.0" \
      "sphinx-csharp==0.1.6" \
      "sphinx-tabs==1.1.5" \
      "javasphinx==0.9.15" \
      https://github.com/djungelorm/sphinx-lua/releases/download/0.1.3/sphinx-lua-0.1.3.tar.gz
  pip install sphinx-autobuild pyinotify
else
  source $env/bin/activate
fi

# Auto-serve and auto-build the docs
python tools/do-serve-docs.py $port $src $stage $out
