#!/usr/bin/env python3
"""Folds the changelog fragments into the CHANGELOG.md files, and marks the
version released.

The first step of the release preparation branch. Each fragment's entries are
written into the unreleased section of its components, the fragments are
deleted, and the ' - unreleased' suffix is dropped. The changes are left
uncommitted, to be reordered and edited by hand before committing.
"""

import shutil
from pathlib import Path

import changelog
import lib


def main():
    lib.require("git")
    lib.require_clean_tree()
    branch = lib.capture("git", "rev-parse", "--abbrev-ref", "HEAD")
    if branch in ("main", "HEAD"):
        raise lib.ReleaseError("fold on the release preparation branch")

    fragments = lib.fragments()
    lib.banner(f"Folding {len(fragments)} fragment(s) into the {lib.TAG} changelogs")
    # Every file is folded before any is written
    changed = {}
    try:
        for component in changelog.COMPONENTS:
            path = Path(component, "CHANGELOG.md")
            text = path.read_text()
            folded = changelog.fold(text, path, lib.VERSION, fragments, component)
            check(text, folded, path, fragments, component)
            released = changelog.strip_unreleased(folded, lib.VERSION)
            if released != text:
                changed[path] = released
    except changelog.ChangelogError as exception:
        raise lib.ReleaseError(str(exception)) from exception
    for path, text in changed.items():
        path.write_text(text)
        print(path)
    shutil.rmtree(changelog.FRAGMENT_DIR, ignore_errors=True)

    lib.banner("Done")
    print(
        "Reorder and edit the entries, then commit the changelogs and the "
        f"removal of {changelog.FRAGMENT_DIR}/ as one commit."
    )
    print("Next: tools/release/02-bump-version.py")


def check(text, folded, path, fragments, component):
    """Check the folded file holds the entries the docs page renders."""

    def texts(nodes):
        return [(node.text, texts(node.children)) for node in nodes]

    section = changelog.unreleased_section(
        changelog.parse_changelog(text, path), lib.VERSION, path
    )
    expected = changelog.merge(
        section.nodes if section else [], changelog.additions(fragments, component)
    )
    actual = changelog.unreleased_section(
        changelog.parse_changelog(folded, path), lib.VERSION, path
    )
    if texts(expected) != texts(actual.nodes if actual else []):
        raise lib.ReleaseError(
            f"{path}: the folded entries differ from the merged ones"
        )


if __name__ == "__main__":
    lib.main(main)
