#!/usr/bin/env python3
"""Sets the version of every train in config.bzl to the one to work towards
next.

The second commit of the release preparation branch, above the fold commit,
which 30-tag.py tags.
"""

import changelog
import lib
import versions


def main():
    lib.require("git")
    lib.require_clean_tree()

    branch = lib.capture("git", "rev-parse", "--abbrev-ref", "HEAD")
    if branch == "HEAD" or lib.is_release_branch(branch):
        raise lib.ReleaseError("bump on the release preparation branch")

    # The bump goes above the fold commit, which 30-tag.py tags
    if changelog.fragment_paths(lib.ROOT) or lib.unreleased_changelogs():
        raise lib.ReleaseError(
            "the changelogs are not folded; run 01-fold-changelogs.py and commit first"
        )
    if not lib.released_changelogs():
        raise lib.ReleaseError(
            f"no changelog has a {lib.TAG} section; fold the changelogs first, "
            "and bump only once"
        )

    # Only the maintainer knows whether what comes next is a patch or a larger
    # step, so the suggestion is the conservative one, to be typed over
    version = lib.ask_version(
        "Version to work towards", versions.bump(lib.VERSION, "patch"), lib.VERSION
    )

    lib.banner(f"Setting the version of every train in {lib.CONFIG} to {version}")
    versions.set_trains(lib.TRAINS, version)
    lib.run("git", "commit", "-m", f"Work towards {version}", str(lib.CONFIG))

    lib.banner("Done")
    print(
        f"Push {branch} and open the release preparation pull request. Once it "
        f"merges, check out the fold commit below 'Work towards {version}' and "
        f"run tools/release/10-preflight.py."
    )


if __name__ == "__main__":
    lib.main(main)
