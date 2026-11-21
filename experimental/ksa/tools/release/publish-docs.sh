#!/bin/bash
# Commit the kRPC for KSA documentation into the gh-pages branch, under
# experimental/ksa/, served at https://krpc.github.io/krpc/experimental/ksa/.
#
#   publish-docs.sh         build the docs with Bazel and commit them to a local gh-pages
#                           worktree
#   publish-docs.sh --push  also push the commit to origin/gh-pages
#
# The docs workflow replaces only its own dev/ and <version>/ folders, and the version
# switcher lists only those, so this folder is left alone and unlisted.
set -euo pipefail

ksa="$(readlink -f "$(dirname "$0")/../..")"
repo="$(dirname "$(dirname "$ksa")")"
mod_version="$(sed -n 's/^mod_version = "\(.*\)"$/\1/p' "$ksa/config.bzl")"
worktree="$ksa/bin/gh-pages"
subpath="experimental/ksa"

push=0
for arg in "$@"; do
  case "$arg" in
    --push) push=1 ;;
    *) echo "unknown argument: $arg" >&2; exit 1 ;;
  esac
done

(cd "$repo" && bazel build //experimental/ksa/doc:html)

git -C "$repo" fetch --no-tags origin gh-pages
# FETCH_HEAD belongs to the main checkout, so resolve it there
commit="$(git -C "$repo" rev-parse FETCH_HEAD)"
if [ -d "$worktree" ]; then
  git -C "$worktree" checkout --quiet --detach "$commit"
else
  git -C "$repo" worktree add --detach "$worktree" "$commit"
fi

rm -rf "${worktree:?}/$subpath"
mkdir -p "$worktree/$subpath"
unzip -q "$repo/bazel-bin/experimental/ksa/doc/html.zip" -d "$worktree/$subpath"

git -C "$worktree" add -A "$subpath"
if git -C "$worktree" diff --cached --quiet; then
  echo "No documentation changes to publish"
  exit 0
fi
git -C "$worktree" commit --quiet -m "Publish kRPC for KSA $mod_version docs to $subpath"
git -C "$worktree" show --stat --oneline HEAD | tail -3

if [ "$push" = 1 ]; then
  git -C "$worktree" push origin HEAD:gh-pages
else
  echo "Committed in $worktree. Push with: git -C $worktree push origin HEAD:gh-pages"
fi
