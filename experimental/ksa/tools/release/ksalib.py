"""Shared helpers for the kRPC for KSA release scripts. Import this, don't run it.

Builds on the helpers of the kRPC release scripts in tools/release/, and reads
the same credentials file. Importing this moves to the root of the repository,
as importing lib does.
"""

import os
import re
import sys
import urllib.error
import urllib.request
from pathlib import Path

import tomllib

sys.path.insert(0, str(Path(__file__).resolve().parents[4] / "tools" / "release"))

import changelog  # noqa: E402
import lib  # noqa: E402

KSA = Path("experimental/ksa")
CHANGELOG = KSA / "CHANGELOG.md"
# The versions of the kRPC clients, KSA and StarMap, as name = "value" lines
PINS = KSA / "config.bzl"
PUBLISH_DOCS = KSA / "tools" / "release" / "publish-docs.sh"

# The listing of the mod in the Borea content index, and the repository its
# watcher stamps each release into
CONTENT_INDEX = "KSAModding/content-index"
LISTING = "listings/kRPC.toml"
CONTENT_INDEX_RELEASES = "KSAModding/content-index-releases"

SPACEDOCK_MOD = 4747
# Kitten Space Agency, in GET /api/games
SPACEDOCK_GAME = 22409

DOCS = "https://krpc.github.io/krpc/experimental/ksa/"
FORUM_THREAD = (
    "https://forums.ahwoo.com/forums/kitten-space-agency/mod-releases/"
    "krpc-v0-1-0-control-ksa-with-scripts-in-python-c-c-c-java-lua-and-more.1462/"
)

CREDENTIAL_KEYS = ("GITHUB_TOKEN", "SPACEDOCK_USERNAME", "SPACEDOCK_PASSWORD")


def pins():
    """The versions in config.bzl, such as ksa_version, by name."""
    return dict(
        re.findall(r'^([a-z0-9_]+) = "([^"]*)"$', PINS.read_text(), re.MULTILINE)
    )


def set_pin(name, value):
    """Set a version in config.bzl."""
    text, count = re.subn(
        rf'^{name} = ".*"$',
        f'{name} = "{value}"',
        PINS.read_text(),
        flags=re.MULTILINE,
    )
    if count != 1:
        raise lib.ReleaseError(f"no {name} found in {PINS}")
    PINS.write_text(text)


# The version of the ksa train, which the release commit releases
VERSION = lib.VERSIONS["ksa"]
TAG = "ksa-v" + VERSION
# Written by bazel build //experimental/ksa:release
ARCHIVE = Path("bazel-bin") / KSA / f"krpc-ksa-v{VERSION}.zip"


def require_release_commit():
    """Raise unless HEAD is a release commit, whose CHANGELOG.md releases VERSION.
    Above it, the commit of 03-bump-version.py works towards the next version."""
    if not is_released():
        raise lib.ReleaseError(
            f"the top section of {CHANGELOG} does not release v{VERSION}; "
            "run 01-prepare.py, or check out the release commit"
        )


def sections():
    """The sections of CHANGELOG.md, newest first."""
    try:
        return changelog.parse_changelog(CHANGELOG.read_text(), str(CHANGELOG))
    except changelog.ChangelogError as exception:
        raise lib.ReleaseError(str(exception)) from exception


def is_released():
    """Whether the top section of CHANGELOG.md releases VERSION."""
    found = sections()
    return bool(found) and found[0].version == VERSION and not found[0].unreleased


def notes():
    """The release notes: the changelog section of VERSION."""
    for section in sections():
        if section.version == VERSION and not section.unreleased:
            lines = CHANGELOG.read_text().splitlines()[section.start + 1 : section.end]
            return "## Changes\n\n" + "\n".join(lines).strip() + "\n"
    raise lib.ReleaseError(f"{CHANGELOG} has no released section for v{VERSION}")


def load_credentials():
    """Read the credentials the KSA release uses, requiring each of them."""
    credentials = lib.read_credentials_file()
    if credentials is None:
        raise lib.ReleaseError(
            f"{lib.CREDENTIALS_FILE} not found; copy {lib.CREDENTIALS_TEMPLATE} "
            f"to it and fill it in"
        )
    missing = [key for key in CREDENTIAL_KEYS if not credentials.get(key)]
    if missing:
        raise lib.ReleaseError(
            f"{lib.CREDENTIALS_FILE} does not define: {' '.join(missing)}"
        )
    return credentials


def gh_environment(credentials):
    """The environment for gh, acting as the token from the credentials file."""
    return dict(os.environ, GH_TOKEN=credentials["GITHUB_TOKEN"])


def fetch(url):
    """The body of a URL, or None if it is not found."""
    request = urllib.request.Request(url, headers={"User-Agent": "krpc-release-tools"})
    try:
        with urllib.request.urlopen(request, timeout=30) as response:
            return response.read().decode()
    except urllib.error.HTTPError as exception:
        if exception.code == 404:
            return None
        raise lib.ReleaseError(
            f"could not fetch {url} ({exception.code} {exception.reason})"
        ) from exception
    except urllib.error.URLError as exception:
        raise lib.ReleaseError(
            f"could not fetch {url}: {exception.reason}"
        ) from exception


def listing():
    """The text of the mod's listing in the Borea content index."""
    text = fetch(f"https://raw.githubusercontent.com/{CONTENT_INDEX}/main/{LISTING}")
    if text is None:
        raise lib.ReleaseError(f"no {LISTING} in {CONTENT_INDEX}")
    return text


def listing_bounds(text):
    """The game_min and loader min of a listing."""
    document = tomllib.loads(text)
    return (
        document.get("compatibility", {}).get("game_min"),
        document.get("loader", {}).get("min"),
    )


def main(step):
    lib.main(step)
