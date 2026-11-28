#!/usr/bin/env python3
"""Makes the release commit, on the release preparation branch.

Folds the experimental/ksa sections of the changelog fragments into
CHANGELOG.md, releases them, sets the version of the ksa train in the root
config.bzl to the version, and commits it all as "Release kRPC for KSA X.Y.Z".
Pass a version to release the entries under another version.
03-bump-version.py then makes the commit above it.

Run on a branch off an up to date release branch: main, or a maintenance branch
such as release/0.8. 02-test.py tests the branch. The release commit reaches the
release branch through the release preparation pull request, and 40-tag.py tags
it there.

    01-prepare.py
    01-prepare.py 0.3.0
"""

import argparse
import re

import ksalib
import lib
import versions

changelog = ksalib.changelog


def main():
    parser = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter
    )
    parser.add_argument("version", nargs="?", help="the version, e.g. 0.3.0")
    arguments = parser.parse_args()
    if arguments.version and not re.fullmatch(r"\d+\.\d+\.\d+", arguments.version):
        raise lib.ReleaseError(f"not a version: {arguments.version}")

    lib.require("git")
    lib.require_clean_tree()
    branch = lib.capture("git", "rev-parse", "--abbrev-ref", "HEAD")
    if branch == "HEAD" or lib.is_release_branch(branch):
        raise lib.ReleaseError("prepare on the release preparation branch")
    bases = lib.release_branches_at()
    if not bases:
        raise lib.ReleaseError(
            f"{branch} is not at the tip of origin/main or of a maintenance "
            "branch such as origin/release/0.8"
        )

    fragments = lib.fragments()
    try:
        text = changelog.fold(
            ksalib.CHANGELOG.read_text(),
            ksalib.CHANGELOG,
            ksalib.VERSION,
            fragments,
            changelog.KSA,
        )
        sections = changelog.parse_changelog(text, str(ksalib.CHANGELOG))
    except changelog.ChangelogError as exception:
        raise lib.ReleaseError(str(exception)) from exception
    if not sections or not sections[0].unreleased:
        raise lib.ReleaseError(
            f"nothing to release: no fragment in {changelog.FRAGMENT_DIR}/ has a "
            f"## {changelog.KSA} section"
        )
    section = sections[0]
    version = arguments.version or section.version
    if any(s.version == version for s in sections[1:]):
        raise lib.ReleaseError(f"{ksalib.CHANGELOG} already has a v{version} section")

    text = text.replace(
        changelog.unreleased_header(section.version), f"## [v{version}]", 1
    )
    ksalib.CHANGELOG.write_text(text)
    folded = changelog.remove_folded(fragments, [changelog.KSA])
    versions.set_trains(["ksa"], version)

    lib.banner(f"Release commit for kRPC for KSA {version}")
    lib.run("git", "add", "-A", "--", ksalib.CHANGELOG, lib.CONFIG, *folded)
    lib.run("git", "commit", "-q", "-m", f"Release kRPC for KSA {version}")

    lib.banner("Done")
    print("Next: experimental/ksa/tools/release/02-test.py")


if __name__ == "__main__":
    ksalib.main(main)
