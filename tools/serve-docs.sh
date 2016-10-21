#!/bin/bash

# Usage tools/serve-docs.sh PORT

set -e

port=${1:-8080}
src=bazel-bin/doc/serve/src
env=bazel-bin/doc/serve/env
out=bazel-bin/doc/serve/out

bazel build //doc:srcs

mkdir -p `dirname $env`
rm -rf $src
cp -R bazel-genfiles/doc/srcs $src

if [ ! -d "$env" ]; then
  virtualenv $env --system-site-packages
  source $env/bin/activate
  pip install \
    sphinx==1.3.5 sphinx-autobuild sphinx_rtd_theme sphinxcontrib_spelling sphinx-csharp \
    https://github.com/djungelorm/sphinx-lua/releases/download/0.1.2/sphinx-lua-0.1.2.tar.gz \
    javasphinx
else
  source $env/bin/activate
fi

sphinx-autobuild $src $out
