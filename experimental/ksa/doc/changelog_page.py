"""Render the changelog page of kRPC for KSA, with the fragments' entries as the
unreleased section of the version: changelog_page.py OUTPUT CHANGELOG VERSION
[FRAGMENT ...]"""

import sys

import changelog
import changeloggen


def main() -> None:
    output, path, version, *fragments = sys.argv[1:]
    versions, unreleased = changeloggen.parse_component(
        path, version, changelog.load_fragments(fragments)
    )
    with open(output, "w", encoding="utf-8") as page:
        page.write(changeloggen.render([("kRPC for KSA", versions)], unreleased))


if __name__ == "__main__":
    main()
