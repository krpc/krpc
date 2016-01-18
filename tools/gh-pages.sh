#!/bin/bash

set -e

git checkout master
if git rev-parse --verify -q gh-pages ; then
  git branch -D gh-pages
fi
git checkout -b gh-pages

bazel build //doc:html
rm -rf docs
unzip -q bazel-bin/doc/html.zip -d docs

git add docs
git commit -m "gh-pages"
git push origin gh-pages -f
git checkout master
