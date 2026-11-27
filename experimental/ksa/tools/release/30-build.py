#!/usr/bin/env python3
"""Builds the release archive, bazel-bin/experimental/ksa/krpc-ksa-v<version>.zip,
and runs the tests that need no game and the docs link check. A failed link
check only warns.

Builds //experimental/ksa:release from the release commit. It builds against the
KSA install in KSA_DIR, which should hold the KSA build in config.bzl.
"""

import ksalib
import lib


def main():
    lib.require("bazel")
    lib.require_clean_tree()
    ksalib.require_release_commit()

    lib.banner(f"Building kRPC for KSA {ksalib.VERSION}")
    lib.run("bazel", "build", "//experimental/ksa:release")
    if not ksalib.ARCHIVE.is_file():
        raise lib.ReleaseError(f"the build did not write {ksalib.ARCHIVE}")

    lib.banner("Running the test suite")
    lib.run("bazel", "test", "//experimental/ksa:test")

    # A link can break after 02-test.py passed, and the release can still go out
    lib.banner("Running the link check")
    try:
        lib.run("bazel", "test", "//experimental/ksa/doc:linkcheck")
    except lib.ReleaseError:
        lib.warning(
            "the link check failed; see "
            "bazel-testlogs/experimental/ksa/doc/linkcheck/test.log"
        )

    lib.banner("Done")
    size = ksalib.ARCHIVE.stat().st_size / 1e6
    print(f"{ksalib.ARCHIVE} ({size:.1f} MB)")
    print("Next: experimental/ksa/tools/release/40-tag.py")


if __name__ == "__main__":
    ksalib.main(main)
