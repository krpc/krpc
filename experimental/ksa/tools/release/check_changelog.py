"""Check that CHANGELOG.md parses, as the docs and the release scripts read it:
check_changelog.py CHANGELOG.md."""

import sys
from pathlib import Path

import changelog


def main():
    path = Path(sys.argv[1])
    try:
        changelog.parse_changelog(path.read_text(encoding="utf-8"), str(path))
    except changelog.ChangelogError as exception:
        sys.exit(str(exception))


if __name__ == "__main__":
    main()
