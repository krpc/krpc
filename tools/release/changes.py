#!/usr/bin/env python3

# Prints the changelog for a release, formatted as the GitHub release notes.
# Covers every component on the kRPC release version train.

import argparse
import re
import sys
from pathlib import Path

import changelog


def current_version():
    version = None
    with open("config.bzl") as config:
        for line in config.readlines():
            m = re.search(r'^version\s*=\s*"(.+)"$', line)
            if m:
                version = m.group(1)
    if version is None:
        print("Failed to get version from config.bzl", file=sys.stderr)
        sys.exit(1)
    return version


def render_nodes(out, nodes, level):
    """Append markdown bullets for nodes and their nested sub-items, indenting
    sub-lists by two spaces per level."""
    for node in nodes:
        out.append("  " * level + "* " + node.text)
        render_nodes(out, node.children, level + 1)


def render(version):
    """Return the changelog for a version, formatted as the GitHub release
    notes."""
    fragments = changelog.load_fragments(changelog.fragment_paths("."))
    changelist = []
    for component, name in changelog.shown_components():
        path = f"{component}/CHANGELOG.md"
        changes = get_changes(path, current_version(), fragments)
        nodes = changes.get(version)
        if nodes and (len(nodes) > 1 or nodes[0].text != "None"):
            changelist.append((name, nodes))

    out = []
    with open("tools/release/github-changes.tmpl") as tmpl:
        out.append("".join(tmpl.readlines()).replace("%VERSION%", version))
    out.append("### Changes\n")
    for name, nodes in changelist:
        out.append("#### " + name + "\n")
        render_nodes(out, nodes, 0)
        out.append("")
    return "\n".join(out)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("version", nargs="?", default=current_version())
    args = parser.parse_args()
    try:
        print(render(args.version))
    except changelog.ChangelogError as error:
        print(error, file=sys.stderr)
        sys.exit(1)


def get_changes(path, version, fragments):
    """Parse a CHANGELOG.md into {version: [node, ...]}, merging the fragment
    entries into the unreleased section of the current version. Versions are
    keyed by their 'X.Y.Z', so a '.postN' release adds to its version."""
    sections = changelog.parse_changelog(Path(path).read_text(), path)
    changes = {}
    for section in sections:
        changes.setdefault(_release(section.version), []).extend(section.nodes)
    additions = changelog.additions(fragments, str(Path(path).parent))
    if additions:
        section = changelog.unreleased_section(sections, version, path)
        changes[version] = changelog.merge(section.nodes if section else [], additions)
    return changes


def _release(version):
    match = re.match(r"\d+\.\d+\.\d+", version)
    return match.group(0) if match else version


if __name__ == "__main__":
    main()
