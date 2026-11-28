#!/bin/sh
# The commit, for the VERSION.txt of each package, marked -dirty when tracked files differ
# from it. Prints nothing outside a git checkout.
commit="$(git rev-parse HEAD 2>/dev/null)" || exit 0
git diff --quiet HEAD -- 2>/dev/null || commit="$commit-dirty"
echo "STABLE_GIT_COMMIT $commit"
