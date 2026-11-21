#!/usr/bin/env python3
"""Publishes the docs to https://krpc.github.io/krpc/experimental/ksa/.

Runs publish-docs.sh, which builds the docs with Bazel and commits them to the
gh-pages branch, then pushes the commit.
"""

import ksalib
import lib


def main():
    lib.require("bash", "git")
    lib.require_clean_tree()
    if not ksalib.is_released():
        raise lib.ReleaseError(
            f"the top section of {ksalib.CHANGELOG} does not release "
            f"v{ksalib.VERSION} (run 02-prepare.py)"
        )

    lib.banner(f"Publishing the docs of kRPC for KSA {ksalib.VERSION}")
    lib.confirm(f"Build the docs and push them to {ksalib.DOCS}?")
    lib.run("bash", ksalib.PUBLISH_DOCS, "--push")

    lib.banner("Done")
    print(f"Published: {ksalib.DOCS}")
    print("Next: experimental/ksa/tools/release/61-release-spacedock.py")


if __name__ == "__main__":
    ksalib.main(main)
