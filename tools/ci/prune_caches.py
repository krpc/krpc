"""Deletes the caches that no later run can restore.

A cache saved by a pull request run is visible only to that pull request, and
one saved on a branch, such as a merge queue branch, only to that branch. Once
the pull request closes, or the branch is deleted, the cache is unreachable, but
it still counts towards the repository's 10 GB. GitHub evicts the least recently
used caches over that limit, which are the caches of main that only the daily
run touches. Caches of main, of open pull requests, of existing branches and of
other refs are kept.

Usage: prune_caches.py --repo OWNER/NAME
"""

import argparse
import re
import subprocess
import sys
from collections.abc import Callable
from dataclasses import dataclass

from gh_cli import gh, gh_lines

# The ref of a pull request run, such as refs/pull/1410/merge
_PULL_REQUEST_REF_RE = re.compile(r"^refs/pull/(\d+)/merge$")
# The ref of a branch, such as refs/heads/gh-readonly-queue/main/pr-1410-e91e2665...
_BRANCH_REF_RE = re.compile(r"^refs/heads/(.+)$")

Gh = Callable[..., str]


@dataclass
class Cache:
    id: int
    ref: str
    key: str
    size: int


def caches(repo: str, api: Gh = gh) -> list[Cache]:
    """Every cache of the repository."""
    return [
        Cache(c["id"], c["ref"], c["key"], c["size_in_bytes"])
        for c in gh_lines(
            "api",
            "--paginate",
            f"repos/{repo}/actions/caches?per_page=100",
            "--jq",
            ".actions_caches[] | {id, ref, key, size_in_bytes}",
            api=api,
        )
    ]


def pull_request_open(repo: str, number: int, api: Gh = gh) -> bool:
    return (
        api("api", f"repos/{repo}/pulls/{number}", "--jq", ".state").strip() == "open"
    )


def found(api: Gh, *args: str) -> bool:
    """Run a gh api call, and return whether it found its object.

    A 404 is not retried, as a retry would not change it. Any other error is.
    """
    for retry in (False, True):
        try:
            api("api", *args, retry=retry)
            return True
        except subprocess.CalledProcessError as error:
            if "HTTP 404" in (error.stderr or ""):
                return False
            if retry:
                raise
    raise AssertionError("unreachable")


def branch_exists(repo: str, branch: str, api: Gh = gh) -> bool:
    return found(api, f"repos/{repo}/branches/{branch}", "--jq", ".name")


def unreachable(repo: str, cache: Cache, api: Gh = gh) -> bool:
    """Whether no later run can restore a cache."""
    if match := _PULL_REQUEST_REF_RE.match(cache.ref):
        return not pull_request_open(repo, int(match.group(1)), api)
    if cache.ref == "refs/heads/main":
        return False
    if match := _BRANCH_REF_RE.match(cache.ref):
        return not branch_exists(repo, match.group(1), api)
    return False


def prune(repo: str, api: Gh = gh) -> list[Cache]:
    """Delete the unreachable caches, and return them."""
    reachable: dict[str, bool] = {}
    deleted = []
    for cache in caches(repo, api):
        if cache.ref not in reachable:
            reachable[cache.ref] = not unreachable(repo, cache, api)
        if not reachable[cache.ref]:
            # A 404 is a cache already deleted, such as by a concurrent run
            found(api, "--method", "DELETE", f"repos/{repo}/actions/caches/{cache.id}")
            deleted.append(cache)
    return deleted


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(
        description="Deletes the caches that no later run can restore."
    )
    parser.add_argument("--repo", required=True)
    args = parser.parse_args(argv)
    deleted = prune(args.repo)
    for cache in deleted:
        print(f"deleted {cache.key} of {cache.ref}, {cache.size // 1_000_000} MB")
    print(
        f"deleted {len(deleted)} caches, {sum(c.size for c in deleted) // 1_000_000} MB"
    )
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
