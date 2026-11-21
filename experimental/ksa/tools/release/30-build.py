#!/usr/bin/env python3
"""Builds the release archive, bazel-bin/experimental/ksa/krpc-ksa-<version>.zip,
and runs the tests that need no game and the docs link check.

Builds //experimental/ksa:release from the release commit. It builds against the
KSA install in KSA_DIR, which should hold the KSA build in config.bzl.
"""

import ksalib
import lib


def main():
    lib.require("bazel")
    lib.require_clean_tree()
    if not ksalib.is_released():
        raise lib.ReleaseError(
            f"the top section of {ksalib.CHANGELOG} does not release "
            f"v{ksalib.VERSION} (run 02-prepare.py)"
        )

    lib.banner(f"Building kRPC for KSA {ksalib.VERSION}")
    lib.run("bazel", "build", "//experimental/ksa:release")
    if not ksalib.ARCHIVE.is_file():
        raise lib.ReleaseError(f"the build did not write {ksalib.ARCHIVE}")

    lib.banner("Running the test suite and the link check")
    lib.run(
        "bazel", "test", "//experimental/ksa:test", "//experimental/ksa/doc:linkcheck"
    )

    lib.banner("Done")
    size = ksalib.ARCHIVE.stat().st_size / 1e6
    print(f"{ksalib.ARCHIVE} ({size:.1f} MB)")
    print("Next: experimental/ksa/tools/release/40-tag.py")


if __name__ == "__main__":
    ksalib.main(main)
