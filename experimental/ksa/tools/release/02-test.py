#!/usr/bin/env python3
"""Tests the release on the release preparation branch.

Builds the release archive, and runs the tests that need no game and the docs
link check. A failure is fixed on the branch, so the fix merges with the
release commit. Re-running it re-tests.
"""

import ksalib
import lib


def main():
    lib.require("bazel", "git")
    lib.require_clean_tree()
    branch = lib.capture("git", "rev-parse", "--abbrev-ref", "HEAD")
    if branch == "HEAD" or lib.is_release_branch(branch):
        raise lib.ReleaseError("test on the release preparation branch")
    if not ksalib.is_released():
        raise lib.ReleaseError(
            f"the top section of {ksalib.CHANGELOG} does not release "
            f"v{ksalib.VERSION} (run 01-prepare.py)"
        )

    lib.banner(f"Testing kRPC for KSA {ksalib.VERSION}")
    lib.run("bazel", "build", "//experimental/ksa:release")
    lib.run(
        "bazel", "test", "//experimental/ksa:test", "//experimental/ksa/doc:linkcheck"
    )

    lib.banner("Done")
    print(
        f"Push {branch} and open the release preparation pull request against "
        "its release branch. Once it merges, check out the release commit and run "
        "experimental/ksa/tools/release/10-preflight.py."
    )


if __name__ == "__main__":
    ksalib.main(main)
