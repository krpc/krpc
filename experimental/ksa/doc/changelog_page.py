"""Render the changelog page of kRPC for KSA: changelog_page.py OUTPUT CHANGELOG"""

import sys

import changeloggen


def main() -> None:
    output, path = sys.argv[1:]
    versions, unreleased = changeloggen.parse_component(path, None, [])
    with open(output, "w", encoding="utf-8") as page:
        page.write(changeloggen.render([("kRPC for KSA", versions)], unreleased))


if __name__ == "__main__":
    main()
