#!/usr/bin/env python3
"""Moves the mod to a new KSA build, and optionally a new StarMap release.

Sets the versions in config.bzl, and gives the next release the changelog
entry "Support KSA v<version>", under the Mod topic. The entry replaces the one
a fragment already holds, and otherwise goes in the fragment of the branch. For
a new StarMap, it downloads the release zip to take its sha256.

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
from pathlib import Path

import ksalib
import lib

changelog = ksalib.changelog

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


SUPPORT = "Support KSA v"


def update_changelog(ksa_version):
    """Give the next release the entry supporting the KSA build, in a fragment.

    Returns the path of the fragment.
    """
    if any(s.version == ksalib.VERSION and not s.unreleased for s in ksalib.sections()):
        raise lib.ReleaseError(
            f"{ksalib.CHANGELOG} already releases v{ksalib.VERSION}; "
            "run 03-bump-version.py on the release preparation branch"
        )
    # A release supports one KSA build, so a later update replaces the entry
    for fragment in lib.fragments():
        for topic in fragment.sections.get(changelog.KSA, []):
            for node in [topic, *topic.children]:
                if node.text.startswith(SUPPORT):
                    path = Path(fragment.path).relative_to(lib.ROOT)
                    replace(path, node, ksa_version)
                    return str(path)

    branch = lib.capture("git", "rev-parse", "--abbrev-ref", "HEAD")
    if branch == "HEAD":
        raise lib.ReleaseError("update on a branch, which names the fragment")
    path = Path(changelog.FRAGMENT_DIR, branch.replace("/", "-") + ".md")
    entry = f"  - {SUPPORT}{ksa_version}"
    lines = path.read_text().splitlines() if path.exists() else []
    heading = f"## {changelog.KSA}"
    if heading not in lines:
        lines += ([""] if lines else []) + [heading, "", "- Mod", entry]
    else:
        start = lines.index(heading) + 1
        end = next(
            (i for i in range(start, len(lines)) if lines[i].startswith("## ")),
            len(lines),
        )
        if "- Mod" in lines[start:end]:
            lines.insert(lines.index("- Mod", start, end) + 1, entry)
        else:
            lines[start:start] = ["", "- Mod", entry]
    path.parent.mkdir(exist_ok=True)
    path.write_text("\n".join(lines) + "\n")
    return str(path)


def replace(path, node, ksa_version):
    """Replace an entry supporting a KSA build, keeping its indent and the
    (#NNN) of the pull request it was filed under."""
    lines = path.read_text().splitlines()
    indent = node.lines[0][: node.lines[0].index("- ")]
    suffix = re.search(r" \(#\d+\)$", node.text)
    lines[node.line - 1 : node.line - 1 + len(node.lines)] = [
        f"{indent}- {SUPPORT}{ksa_version}" + (suffix.group(0) if suffix else "")
    ]
    path.write_text("\n".join(lines) + "\n")


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
    fragment = update_changelog(arguments.ksa_version)

    lib.banner("Done")
    print(f"Set KSA {arguments.ksa_version} and StarMap {starmap}.")
    print(f"Added '{SUPPORT}{arguments.ksa_version}' to {fragment}.")
    print("Add the pull request number to the entry once the pull request is open.")
    print("Next:")
    if arguments.starmap:
        print(" - Check the StarMap.API package the new StarMap needs, and set it with")
        print("   python3 tools/deps/deps.py update StarMap.API --to <version>")
    print(" - bazel build //experimental/ksa/..., and fix what the new build breaks")
    print(' - bazel run //experimental/ksa/tools/game:test-ksa -- -m "not slow"')


if __name__ == "__main__":
    ksalib.main(main)
