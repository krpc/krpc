#!/usr/bin/env python3
"""Moves the mod to a new KSA build, and optionally a new StarMap release.

Sets the versions in config.bzl, and adds
"Support KSA v<version>" to the unreleased section of CHANGELOG.md, creating a
section for the ksa train's version in the root config.bzl if there is none. For a new StarMap, it
downloads the release zip to take its sha256.

Commits nothing: the change goes in a pull request with the fixes the new
build needs.

    update-ksa.py <ksa build>
    update-ksa.py <ksa build> --starmap <starmap release>
"""

import argparse
import hashlib
import re
import urllib.error
import urllib.request

import ksalib
import lib

STARMAP_ZIP = (
    "https://github.com/StarMapLoader/StarMap/releases/download/"
    "{version}/StarMap-{version}.zip"
)


def starmap_sha256(version):
    url = STARMAP_ZIP.format(version=version)
    print(f"Downloading {url}")
    request = urllib.request.Request(url, headers={"User-Agent": "krpc-release-tools"})
    try:
        with urllib.request.urlopen(request, timeout=300) as response:
            return hashlib.sha256(response.read()).hexdigest()
    except urllib.error.HTTPError as exception:
        raise lib.ReleaseError(
            f"no StarMap {version} release zip ({exception.code} {exception.reason})"
        ) from exception
    except urllib.error.URLError as exception:
        raise lib.ReleaseError(
            f"could not reach GitHub: {exception.reason}"
        ) from exception


def update_changelog(ksa_version):
    """Add or replace the KSA support entry of the unreleased section.

    Returns the version of that section.
    """
    entry = f"  - Support KSA v{ksa_version}"
    lines = ksalib.CHANGELOG.read_text().splitlines()
    sections = ksalib.sections()
    if not sections or not sections[0].unreleased:
        version = ksalib.VERSION
        if any(section.version == version for section in sections):
            raise lib.ReleaseError(
                f"{ksalib.CHANGELOG} already releases v{version}; "
                "run 03-bump-version.py on the release preparation branch"
            )
        start = sections[0].start if sections else len(lines)
        lines[start:start] = [f"## [v{version}] - unreleased", "", "- Mod", entry, ""]
    else:
        section = sections[0]
        version = section.version
        body = range(section.start + 1, section.end)
        existing = [i for i in body if lines[i].startswith("  - Support KSA v")]
        topic = [i for i in body if lines[i] == "- Mod"]
        if existing:
            # Keep the (#NNN) of the pull request the entry was filed under
            suffix = re.search(r" \(#\d+\)$", lines[existing[0]])
            lines[existing[0]] = entry + (suffix.group(0) if suffix else "")
        elif topic:
            lines.insert(topic[0] + 1, entry)
        else:
            lines[section.start + 1 : section.start + 1] = ["", "- Mod", entry]
    ksalib.CHANGELOG.write_text("\n".join(lines) + "\n")
    return version


def main():
    parser = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter
    )
    parser.add_argument("ksa_version", help="the KSA build, as YYYY.MM.DD.NNNN")
    parser.add_argument("--starmap", help="the StarMap release, e.g. 0.5.4")
    arguments = parser.parse_args()
    if not re.fullmatch(r"\d{4}\.\d+\.\d+\.\d+", arguments.ksa_version):
        raise lib.ReleaseError(f"not a KSA build version: {arguments.ksa_version}")

    pins = ksalib.pins()
    lib.banner(f"Moving kRPC for KSA to KSA {arguments.ksa_version}")
    ksalib.set_pin("ksa_version", arguments.ksa_version)
    starmap = arguments.starmap or pins["starmap_version"]
    if arguments.starmap and arguments.starmap != pins["starmap_version"]:
        ksalib.set_pin("starmap_sha256", starmap_sha256(arguments.starmap))
        ksalib.set_pin("starmap_version", arguments.starmap)
    version = update_changelog(arguments.ksa_version)

    lib.banner("Done")
    print(f"Set KSA {arguments.ksa_version} and StarMap {starmap}.")
    print(
        f"Added 'Support KSA v{arguments.ksa_version}' to v{version} in {ksalib.CHANGELOG}."
    )
    print("Add the pull request number to that entry once the pull request is open.")
    print("Next:")
    if arguments.starmap:
        print(" - Check the StarMap.API package the new StarMap needs, and set it with")
        print("   python3 tools/deps/deps.py update StarMap.API --to <version>")
    print(" - bazel build //experimental/ksa/..., and fix what the new build breaks")
    print(' - bazel run //experimental/ksa/tools/game:test-ksa -- -m "not slow"')


if __name__ == "__main__":
    ksalib.main(main)
