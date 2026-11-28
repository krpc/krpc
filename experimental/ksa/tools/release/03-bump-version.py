#!/usr/bin/env python3
"""Makes the commit above the release commit, on the release preparation branch.

Sets the version of the ksa train in config.bzl to the next patch, and commits
it as "Work towards kRPC for KSA X.Y.Z". Run it once 02-test.py passes, as the
tag goes on the release commit below it.
"""

import ksalib
import lib
import versions


def main():
    lib.require("git")
    lib.require_clean_tree()
    branch = lib.capture("git", "rev-parse", "--abbrev-ref", "HEAD")
    if branch == "HEAD" or lib.is_release_branch(branch):
        raise lib.ReleaseError("bump on the release preparation branch")
    ksalib.require_release_commit()

    planned = versions.bump(ksalib.VERSION, "patch")
    lib.banner(f"Working towards kRPC for KSA {planned}")
    versions.set_trains(["ksa"], planned)
    lib.run(
        "git", "commit", "-q", "-m", f"Work towards kRPC for KSA {planned}", lib.CONFIG
    )

    lib.banner("Done")
    print(
        f"Push {branch} and open the release preparation pull request against "
        "its release branch. Once it merges, check out the release commit, the "
        f"parent of 'Work towards kRPC for KSA {planned}', and run "
        "experimental/ksa/tools/release/10-preflight.py."
    )


if __name__ == "__main__":
    ksalib.main(main)
