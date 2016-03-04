#!/bin/bash

# Update the documentation site
# Must be run with master as the current bracnh and a clean working directory

set -ev

BRANCH=`git rev-parse --abbrev-ref HEAD`
COMMIT=`git rev-parse HEAD`

if [[ $BRANCH == "HEAD" ]]; then
    echo "Not on a branch"
    exit 1
fi

if [[ $(git status --porcelain | wc -l) != 0 ]]; then
    echo "Working directory is not clean"
    exit 1
fi

bazel build //doc:html
git co gh-pages
git rm -rf .
printf "bazel-*\nlib\n" > .gitignore
echo "" > .nojekyll
git add .gitignore .nojekyll
git clean -df
unzip -q bazel-bin/doc/html.zip
git add .
if [[ $(git diff --cached | wc -l) == 0 ]]; then
    echo "No changes to commit"
    git co $BRANCH
    exit 1
fi
git commit -m "Update site from branch $BRANCH ($COMMIT)"
git co $BRANCH
