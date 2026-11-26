#!/usr/bin/env python3
"""Makes the release commit, on the release preparation branch.

Releases the unreleased section of CHANGELOG.md, sets mod_version in
config.bzl to its version, and commits both as "Release kRPC for KSA
X.Y.Z". Pass a version to release the section under another version, e.g. a
minor version for a section the update script started as a patch. If KRPC.Core
changed since its version was last bumped, the step asks how to bump it, and
the release commit includes the bump.

Run on a branch off an up to date release branch: main, or a maintenance branch
such as release/0.8. The release commit reaches the release branch through the
release preparation pull request, and 40-tag.py tags it there.

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
    if branch == "HEAD" or lib.is_release_branch(branch):
        raise lib.ReleaseError("prepare on the release preparation branch")
    bases = lib.release_branches_at()
    if not bases:
        raise lib.ReleaseError(
            f"{branch} is not at the tip of origin/main or of a maintenance "
            "branch such as origin/release/0.8"
        )

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
    lib.bump_core_version()

    lib.banner(f"Release commit for kRPC for KSA {version}")
    lib.run("git", "add", ksalib.CHANGELOG, ksalib.VERSIONS, lib.CONFIG)
    lib.run("git", "commit", "-q", "-m", f"Release kRPC for KSA {version}")

    lib.banner("Done")
    print(
        f"Push {branch} and open the release preparation pull request against "
        f"{' or '.join(bases)}. Once it merges, check out the release commit "
        "and run experimental/ksa/tools/release/10-preflight.py."
    )


if __name__ == "__main__":
    ksalib.main(main)
