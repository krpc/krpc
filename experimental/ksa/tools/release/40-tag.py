#!/usr/bin/env python3
"""Tags the release commit ksa-vX.Y.Z, and pushes the tag.

HEAD is the release commit, checked out once the release preparation pull
request has merged. The tag has the ksa- prefix: a v* tag starts the docs
workflow of the kRPC release.
"""

import ksalib
import lib


def main():
    lib.require("git")
    lib.require_clean_tree()
    if not ksalib.is_released():
        raise lib.ReleaseError(
            f"the top section of {ksalib.CHANGELOG} does not release "
            f"v{ksalib.VERSION} (run 01-prepare.py)"
        )

    lib.run("git", "fetch", "-q", "origin", "main")
    if not lib.succeeds("git", "merge-base", "--is-ancestor", "HEAD", "origin/main"):
        raise lib.ReleaseError(
            "HEAD is not on origin/main; merge the release preparation pull request"
        )

    if lib.succeeds("git", "rev-parse", "-q", "--verify", f"refs/tags/{ksalib.TAG}"):
        if lib.capture("git", "rev-parse", f"{ksalib.TAG}^{{commit}}") != lib.capture(
            "git", "rev-parse", "HEAD"
        ):
            raise lib.ReleaseError(
                f"tag {ksalib.TAG} already exists but points elsewhere"
            )
        print(f"Tag {ksalib.TAG} already exists and points at HEAD.")
    else:
        lib.banner(f"Tagging HEAD as {ksalib.TAG}")
        lib.run("git", "tag", "-a", ksalib.TAG, "-m", f"kRPC for KSA v{ksalib.VERSION}")

    lib.banner("Pushing the tag")
    lib.confirm(f"Push {ksalib.TAG} to origin?")
    lib.run("git", "push", "origin", ksalib.TAG)

    lib.banner("Done")
    print("Next: experimental/ksa/tools/release/50-release-github.py")


if __name__ == "__main__":
    ksalib.main(main)
