#!/usr/bin/env python3
"""Makes the release commit, on the release preparation branch.

Releases the unreleased section of CHANGELOG.md, sets mod_version in
config.bzl to its version, and commits both as "Release kRPC for KSA
X.Y.Z". Pass a version to release the section under another version, e.g. a
minor version for a section the update script started as a patch.

Run on a branch off an up to date main. The release commit reaches main through
the release preparation pull request, and 40-tag.py tags it there.

    01-prepare.py
    01-prepare.py 0.3.0
"""

import argparse
import re

import ksalib
import lib


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
    if branch in ("main", "HEAD"):
        raise lib.ReleaseError("prepare on the release preparation branch")
    lib.run("git", "fetch", "-q", "origin", "main")
    if lib.capture("git", "rev-parse", "HEAD") != lib.capture(
        "git", "rev-parse", "origin/main"
    ):
        raise lib.ReleaseError(f"{branch} is not at origin/main")

    sections = ksalib.sections()
    if not sections or not sections[0].unreleased:
        raise lib.ReleaseError(f"{ksalib.CHANGELOG} has no unreleased section")
    section = sections[0]
    version = arguments.version or section.version
    if any(s.version == version for s in sections[1:]):
        raise lib.ReleaseError(f"{ksalib.CHANGELOG} already has a v{version} section")

    text = ksalib.CHANGELOG.read_text()
    text = text.replace(
        ksalib.changelog.unreleased_header(section.version), f"## [v{version}]", 1
    )
    ksalib.CHANGELOG.write_text(text)
    ksalib.set_pin("mod_version", version)

    lib.banner(f"Release commit for kRPC for KSA {version}")
    lib.run("git", "add", ksalib.CHANGELOG, ksalib.VERSIONS)
    lib.run("git", "commit", "-q", "-m", f"Release kRPC for KSA {version}")

    lib.banner("Done")
    print(
        f"Push {branch} and open the release preparation pull request. Once it "
        "merges, check out the release commit and run "
        "experimental/ksa/tools/release/10-preflight.py."
    )


if __name__ == "__main__":
    ksalib.main(main)
