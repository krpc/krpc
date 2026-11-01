#!/usr/bin/env python3
"""Tags HEAD as vx.x.x and pushes the tag to GitHub.

HEAD is the fold commit of the release preparation pull request, checked out
once the pull request has merged. Pushing the version tag triggers the docs
workflow, which freezes the release documentation under /<version>/ and
regenerates the version dropdown.
"""

import lib


def main():
    lib.require("git")
    lib.require_clean_tree()
    if lib.unreleased_changelogs() or not lib.released_changelogs():
        raise lib.ReleaseError(
            f"HEAD does not release {lib.TAG}; check out the fold commit"
        )

    if lib.succeeds("git", "rev-parse", "-q", "--verify", f"refs/tags/{lib.TAG}"):
        if lib.capture("git", "rev-parse", f"{lib.TAG}^{{commit}}") != lib.capture(
            "git", "rev-parse", "HEAD"
        ):
            raise lib.ReleaseError(f"tag {lib.TAG} already exists but points elsewhere")
        print(f"Tag {lib.TAG} already exists and points at HEAD.")
    else:
        lib.banner(f"Tagging HEAD as {lib.TAG}")
        lib.run("git", "tag", "-a", lib.TAG, "-m", f"kRPC {lib.TAG}")

    lib.banner("Pushing to GitHub")
    lib.confirm(f"Push {lib.TAG} to origin?")
    lib.run("git", "push", "origin", lib.TAG)

    lib.banner("Done")
    print("Next: tools/release/40-build-assets.py")


if __name__ == "__main__":
    lib.main(main)
