"""Reads and writes the versions in config.bzl.

The one reader of config.bzl's versions outside Bazel, for the release scripts,
tools/version.py and the docs index, and the x.y.z arithmetic of the release
scripts.
"""

import re
from pathlib import Path

CONFIG = Path(__file__).resolve().parents[2] / "config.bzl"


class VersionError(Exception):
    """A version or config.bzl the release scripts cannot use."""


# The versions of config.bzl, and a train's entry in them
VERSIONS_RE = re.compile(r"^versions = \{\n(.*?)^\}$", re.MULTILINE | re.DOTALL)
TRAIN_RE = re.compile(r'^    "(\w+)": "([^"]+)",$', re.MULTILINE)


def versions_block(text, config):
    """The span of the body of the versions in the text of config.bzl."""
    match = VERSIONS_RE.search(text)
    if match is None:
        raise VersionError(f"no versions found in {config}")
    return match.span(1)


def trains(config=CONFIG):
    """The version of each release train, by train."""
    text = config.read_text()
    start, end = versions_block(text, config)
    found = dict(TRAIN_RE.findall(text, start, end))
    if not found:
        raise VersionError(f"no train versions found in {config}")
    return found


def set_trains(names, version, config=CONFIG):
    """Set the version of the named trains."""
    text = config.read_text()
    start, end = versions_block(text, config)
    missing = set(names) - set(trains(config))
    if missing:
        raise VersionError(f"no version in {config} for {', '.join(sorted(missing))}")

    def entry(match):
        train = match.group(1)
        if train not in names:
            return match.group(0)
        return f'    "{train}": "{version}",'

    config.write_text(text[:start] + TRAIN_RE.sub(entry, text[start:end]) + text[end:])


def parse(version):
    """The (x, y, z) of an x.y.z version."""
    match = re.fullmatch(r"(\d+)\.(\d+)\.(\d+)", version)
    if match is None:
        raise VersionError(f"'{version}' is not an x.y.z version")
    return tuple(int(part) for part in match.groups())


def bump(version, part):
    """The next major, minor or patch version after an x.y.z version."""
    major, minor, patch = parse(version)
    return {
        "major": f"{major + 1}.0.0",
        "minor": f"{major}.{minor + 1}.0",
        "patch": f"{major}.{minor}.{patch + 1}",
    }[part]
