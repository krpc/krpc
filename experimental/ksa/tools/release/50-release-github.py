#!/usr/bin/env python3
"""Creates the GitHub release for the pushed ksa-vX.Y.Z tag.

The changelog section of the version becomes the release notes, and the
release archive the only asset. The release is published, with --latest=false
so that the kRPC release stays the latest release of the repository. Publishing
it also has the Borea watcher stamp it.
"""

import ksalib
import lib


def main():
    lib.require("gh", "git")
    credentials = ksalib.load_credentials()
    environment = ksalib.gh_environment(credentials)

    if not lib.capture("git", "ls-remote", "origin", f"refs/tags/{ksalib.TAG}"):
        raise lib.ReleaseError(
            f"tag {ksalib.TAG} has not been pushed to origin (run 40-tag.py first)"
        )
    if not ksalib.ARCHIVE.is_file():
        raise lib.ReleaseError(f"{ksalib.ARCHIVE} not found (run 30-build.py first)")
    if lib.succeeds("gh", "release", "view", ksalib.TAG, env=environment):
        raise lib.ReleaseError(f"a GitHub release for {ksalib.TAG} already exists")

    lib.banner("Release notes")
    notes = ksalib.notes()
    print(notes)

    lib.banner(f"Creating release {ksalib.TAG}")
    lib.confirm(f"Publish the release, with {ksalib.ARCHIVE.name}?")
    url = lib.capture(
        "gh",
        "release",
        "create",
        ksalib.TAG,
        "--verify-tag",
        "--latest=false",
        "--title",
        f"kRPC for KSA v{ksalib.VERSION}",
        "--notes",
        notes,
        ksalib.ARCHIVE,
        env=environment,
    )

    lib.banner("Done")
    print(f"Published: {url}")
    print("Next: experimental/ksa/tools/release/60-publish-docs.py")


if __name__ == "__main__":
    ksalib.main(main)
