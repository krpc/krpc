#!/usr/bin/env python3
"""Publishes the mod on SpaceDock.

Uploads assets/krpc-<version>.zip as a new version of the kRPC mod, for the KSP
version the mod is built for, and notifies its followers. Its changelog is a
link to the changelog page of the docs, which the docs workflow froze when the
tag was pushed (30-tag.py).

Run after 40-build-assets.py. The email to followers cannot be recalled, and a
version can only be deleted by hand on the site, so the step asks first.
Re-running it after a successful upload reports the version already there.
"""

import re
import urllib.error
import urllib.request
from pathlib import Path

import lib
import spacedock

MOD = 69
# Kerbal Space Program, in GET /api/games
GAME = 3102

CHANGELOG = "https://krpc.github.io/krpc/{version}/changelog.html"


def ksp_version():
    """The KSP version the mod is built for, as SpaceDock names it.

    SpaceDock names KSP versions by major, minor and patch, so the build
    number of ksp_version_max in config.bzl is dropped.
    """
    match = re.search(
        r'^ksp_version_max\s*=\s*"(\d+\.\d+\.\d+)(\.\d+)?"$',
        Path("config.bzl").read_text(),
        re.MULTILINE,
    )
    if match is None:
        raise lib.ReleaseError("no ksp_version_max found in config.bzl")
    return match.group(1)


def check_changelog(url):
    """Fail if the changelog page the version links to is missing."""
    request = urllib.request.Request(url, method="HEAD")
    try:
        urllib.request.urlopen(request, timeout=30)
    except urllib.error.HTTPError as exception:
        raise lib.ReleaseError(
            f"{url} is not there ({exception.code} {exception.reason}); "
            f"wait for the docs workflow of {lib.TAG} to finish"
        ) from exception
    except urllib.error.URLError as exception:
        raise lib.ReleaseError(
            f"could not reach {url}: {exception.reason}"
        ) from exception


def main():
    credentials = lib.load_credentials()
    archive = Path(f"assets/krpc-{lib.VERSION}.zip")
    if not archive.is_file():
        raise lib.ReleaseError(f"{archive} not found; run 40-build-assets.py first")

    session = spacedock.Session()
    page = f"{spacedock.SITE}/mod/{MOD}"
    if lib.VERSION in session.versions(MOD):
        lib.banner("Done")
        print(f"SpaceDock already has kRPC {lib.VERSION}: {page}")
        return

    game_version = ksp_version()
    if game_version not in session.game_versions(GAME):
        raise lib.ReleaseError(f"SpaceDock does not list KSP {game_version}")
    changelog = CHANGELOG.format(version=lib.VERSION)
    check_changelog(changelog)

    lib.banner(f"Publishing kRPC {lib.VERSION} on SpaceDock")
    print(f"Mod:          {page}")
    print(f"Archive:      {archive}")
    print(f"KSP version:  {game_version}")
    print(f"Changelog:    {changelog}")
    lib.confirm("Upload it and email its followers?")

    session.login(credentials["SPACEDOCK_USERNAME"], credentials["SPACEDOCK_PASSWORD"])
    url = session.upload(MOD, lib.VERSION, game_version, changelog, archive)
    lib.banner("Done")
    print(f"Published kRPC {lib.VERSION} on SpaceDock: {url}")


if __name__ == "__main__":
    lib.main(main)
