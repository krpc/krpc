#!/usr/bin/env python3
"""Checks the repository, tools, credentials and listings before a release.

Read-only: builds nothing and publishes nothing.
"""

import os
import shutil
from pathlib import Path

import ksalib
import lib
import spacedock


class Report:
    """The check report, one line per check."""

    def __init__(self):
        self.failures = 0

    def ok(self, message):
        print(f" {lib.GREEN}✓{lib.RESET} {message}")

    def warn(self, message):
        print(f" {lib.YELLOW}!{lib.RESET} {message}")

    def fail(self, message):
        print(f" {lib.RED}✗ {message}{lib.RESET}")
        self.failures += 1


def check_repository(report):
    lib.banner("Repository state")
    if lib.capture("git", "status", "--porcelain"):
        report.fail("working tree has uncommitted changes")
    else:
        report.ok("working tree is clean")

    if not lib.succeeds("git", "fetch", "-q", "origin", "main"):
        report.fail("could not fetch origin/main to compare against")
    elif lib.succeeds("git", "merge-base", "--is-ancestor", "HEAD", "origin/main"):
        report.ok("HEAD is on origin/main")
    else:
        report.fail("HEAD is not on origin/main")

    if ksalib.is_released():
        report.ok(f"{ksalib.CHANGELOG} releases v{ksalib.VERSION}")
    else:
        report.fail(
            f"the top section of {ksalib.CHANGELOG} does not release "
            f"v{ksalib.VERSION} (run 02-prepare.py)"
        )

    if lib.succeeds("git", "rev-parse", "-q", "--verify", f"refs/tags/{ksalib.TAG}"):
        if lib.capture("git", "rev-parse", f"{ksalib.TAG}^{{commit}}") == lib.capture(
            "git", "rev-parse", "HEAD"
        ):
            report.ok(f"tag {ksalib.TAG} exists and points at HEAD")
        else:
            report.fail(f"tag {ksalib.TAG} exists but does not point at HEAD")
    else:
        report.ok(f"tag {ksalib.TAG} not created yet (40-tag.py creates it)")


def check_tools(report):
    lib.banner("Tools")
    for tool in ("git", "gh", "bazel", "unzip"):
        if shutil.which(tool):
            report.ok(tool)
        else:
            report.fail(f"{tool} not found")


def check_game(report, pins):
    lib.banner("KSA install")
    if not os.environ.get("KSA_DIR"):
        report.fail("KSA_DIR is not set")
        return
    directory = Path(os.environ["KSA_DIR"]).resolve()
    if not (directory / "KSA.dll").is_file():
        report.fail(f"no KSA.dll in {directory} (set KSA_DIR)")
    elif directory.name == f"v{pins['ksa_version']}":
        report.ok(f"KSA {pins['ksa_version']} in {directory}")
    else:
        report.warn(
            f"cannot tell whether {directory} holds KSA {pins['ksa_version']}, "
            f"the version the release is built against"
        )


def check_credentials(report):
    lib.banner("Credentials")
    credentials = lib.read_credentials_file()
    if credentials is None:
        report.fail(f"{lib.CREDENTIALS_FILE} not found")
        return
    for key in ksalib.CREDENTIAL_KEYS:
        if credentials.get(key):
            report.ok(f"{key} is set")
        else:
            report.fail(f"{key} is not set in {lib.CREDENTIALS_FILE}")

    if credentials.get("GITHUB_TOKEN"):
        try:
            login, _ = lib.github_user(credentials["GITHUB_TOKEN"])
            report.ok(f"GITHUB_TOKEN authenticates as {login}")
        except lib.ReleaseError as exception:
            report.fail(f"GITHUB_TOKEN: {exception}")

    if credentials.get("SPACEDOCK_USERNAME") and credentials.get("SPACEDOCK_PASSWORD"):
        try:
            spacedock.Session().login(
                credentials["SPACEDOCK_USERNAME"], credentials["SPACEDOCK_PASSWORD"]
            )
            report.ok("SpaceDock accepts the SPACEDOCK_* login")
        except lib.ReleaseError as exception:
            report.fail(f"SPACEDOCK_*: {exception}")


def check_spacedock(report):
    lib.banner("SpaceDock")
    session = spacedock.Session()
    try:
        if ksalib.VERSION in session.versions(ksalib.SPACEDOCK_MOD):
            report.fail(f"SpaceDock already has kRPC {ksalib.VERSION}")
        else:
            report.ok(f"SpaceDock does not have kRPC {ksalib.VERSION} yet")
    except lib.ReleaseError as exception:
        report.fail(str(exception))


def check_listing(report, pins):
    lib.banner("Borea listing")
    try:
        game_min, loader_min = ksalib.listing_bounds(ksalib.listing())
    except lib.ReleaseError as exception:
        report.fail(str(exception))
        return
    if game_min == pins["ksa_version"]:
        report.ok(f"game_min is {game_min}, the KSA build the release is built against")
    else:
        report.warn(
            f"game_min is {game_min}, and the release is built against "
            f"{pins['ksa_version']} (20-borea-listing.py updates it)"
        )
    if loader_min == pins["starmap_version"]:
        report.ok(f"the StarMap min is {loader_min}, the pinned StarMap")
    else:
        report.warn(
            f"the StarMap min is {loader_min}, and the pinned StarMap is "
            f"{pins['starmap_version']}; check the min is the oldest StarMap the "
            f"release runs on"
        )


def main():
    lib.banner(f"Releasing kRPC for KSA {ksalib.VERSION} (tag {ksalib.TAG})")
    pins = ksalib.pins()
    report = Report()
    check_repository(report)
    check_tools(report)
    check_game(report, pins)
    check_credentials(report)
    check_spacedock(report)
    check_listing(report, pins)

    lib.banner("Result")
    if report.failures == 0:
        print(
            f"{lib.GREEN}{lib.BOLD}All checks passed.{lib.RESET} "
            f"Next: experimental/ksa/tools/release/20-borea-listing.py"
        )
    else:
        print(f"{lib.RED}{lib.BOLD}{report.failures} check(s) failed.{lib.RESET}")
        raise SystemExit(1)


if __name__ == "__main__":
    ksalib.main(main)
