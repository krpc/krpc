#!/usr/bin/env python3
"""Publishes the mod on SpaceDock.

Uploads the release archive as a new version of the kRPC for KSA mod, for the
KSA build in config.bzl, and emails its followers. Its changelog is a link
to the changelog page of the docs, so the step checks the page lists the
version.

The email to followers cannot be recalled, and a version can only be deleted by
hand on the site, so the step asks first. Re-running it after a successful
upload reports the version already there.
"""

import ksalib
import lib
import spacedock


def main():
    credentials = ksalib.load_credentials()
    if not ksalib.ARCHIVE.is_file():
        raise lib.ReleaseError(f"{ksalib.ARCHIVE} not found (run 30-build.py first)")

    session = spacedock.Session()
    page = f"{spacedock.SITE}/mod/{ksalib.SPACEDOCK_MOD}"
    if ksalib.VERSION in session.versions(ksalib.SPACEDOCK_MOD):
        lib.banner("Done")
        print(f"SpaceDock already has kRPC {ksalib.VERSION}: {page}")
        return

    game_version = ksalib.pins()["ksa_version"]
    if game_version not in session.game_versions(ksalib.SPACEDOCK_GAME):
        raise lib.ReleaseError(f"SpaceDock does not list KSA {game_version}")
    changelog = f"{ksalib.DOCS}changelog.html"
    if f"v{ksalib.VERSION}" not in (ksalib.fetch(changelog) or ""):
        raise lib.ReleaseError(
            f"{changelog} does not list v{ksalib.VERSION} (run 60-publish-docs.py)"
        )

    lib.banner(f"Publishing kRPC for KSA {ksalib.VERSION} on SpaceDock")
    print(f"Mod:          {page}")
    print(f"Archive:      {ksalib.ARCHIVE}")
    print(f"KSA version:  {game_version}")
    print(f"Changelog:    {changelog}")
    lib.confirm("Upload it and email its followers?")

    session.login(credentials["SPACEDOCK_USERNAME"], credentials["SPACEDOCK_PASSWORD"])
    url = session.upload(
        ksalib.SPACEDOCK_MOD, ksalib.VERSION, game_version, changelog, ksalib.ARCHIVE
    )
    lib.banner("Done")
    print(f"Published kRPC for KSA {ksalib.VERSION} on SpaceDock: {url}")
    print("Next: experimental/ksa/tools/release/62-check-borea.py")


if __name__ == "__main__":
    ksalib.main(main)
