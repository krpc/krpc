"""Checks the changelog fragments: each is named '<name>.md' and parses, and
names only components on the kRPC release version.

Reads the fragment directory under the current directory, which in a Bazel test
is the runfiles tree."""

import sys

import changelog


def main():
    try:
        fragments = changelog.load_fragments(changelog.fragment_paths("."))
    except changelog.ChangelogError as error:
        print(error, file=sys.stderr)
        return 1
    print(f"{len(fragments)} fragment(s) checked")
    return 0


if __name__ == "__main__":
    sys.exit(main())
