"""Checks that the NuGet versions written by hand match the locked versions.

The locked versions are the ones in paket.main.bzl, generated from paket.lock. The other
files name a package version in a repository name, such as nuget.google.protobuf.v3.36.2,
in an import_library version, or in the dependencies of a nuget_package.

Usage:
  check_versions.py paket.main.bzl FILE...
"""

import re
import sys
from pathlib import Path

LOCKED = re.compile(r'\{"name": "[^"]+", "id": "([^"]+)", "version": "([^"]+)"')
REPOSITORY = re.compile(r"\bnuget\.([a-z0-9._-]+?)\.v(\d[0-9a-z.+-]*?)(?=//|\")")
IMPORT_LIBRARY = re.compile(r"^import_library\((.*?)^\)", re.MULTILINE | re.DOTALL)
NUGET_PACKAGE = re.compile(r"^nuget_package\((.*?)^\)", re.MULTILINE | re.DOTALL)
LIBRARY_NAME = re.compile(r'\blibrary_name = "([^"]+)"')
VERSION = re.compile(r'^    version = "([^"]+)"', re.MULTILINE)
DEPS = re.compile(r"\bdeps = \{(.*?)\}", re.DOTALL)
DEP = re.compile(r'"([^"]+)": "([^"]+)"')


def check(text: str, locked: dict[str, str]) -> list[str]:
    """Returns the mismatches in one file. Package ids compare lowercase."""
    errors = []

    def compare(package: str, version: str, where: str) -> None:
        expected = locked.get(package.lower())
        if expected is None:
            errors.append(f"{where}: {package} is not in paket.lock")
        elif version != expected:
            errors.append(f"{where}: {version}, locked {expected}")

    for match in REPOSITORY.finditer(text):
        compare(match[1], match[2], f"repository {match[0]}")
    for match in IMPORT_LIBRARY.finditer(text):
        name = LIBRARY_NAME.search(match[1])
        version = VERSION.search(match[1])
        if name and version and name[1].lower() in locked:
            compare(name[1], version[1], f"import_library {name[1]}")
    for match in NUGET_PACKAGE.finditer(text):
        deps = DEPS.search(match[1])
        for dep in DEP.finditer(deps[1] if deps else ""):
            compare(dep[1], dep[2], f"nuget_package dependency {dep[1]}")
    return errors


def main() -> None:
    lock, *paths = sys.argv[1:]
    locked = {
        package.lower(): version
        for package, version in LOCKED.findall(Path(lock).read_text())
    }
    if not locked:
        sys.exit(f"{lock}: no packages found")
    errors = [
        f"{path}: {error}"
        for path in paths
        for error in check(Path(path).read_text(), locked)
    ]
    if errors:
        print("\n".join(errors))
        print("Update these to the versions in paket.lock.")
        sys.exit(1)


if __name__ == "__main__":
    main()
