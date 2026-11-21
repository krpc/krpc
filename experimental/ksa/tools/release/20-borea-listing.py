#!/usr/bin/env python3
"""Updates the KSA and StarMap bounds of the mod's Borea listing.

The Borea watcher stamps a release with the bounds in the listing at the time,
so they are updated before the release is published. game_min becomes the KSA
build the release is built against, and the StarMap min the pinned StarMap,
unless --game-min or --loader-min give others.

The listing changes through a pull request to the content index, from a fork on
the account of GITHUB_TOKEN. It merges itself once its checks pass, as the
repository topic of krpc/krpc proves ownership, and the step waits for that.
Re-running it re-checks the listing, and waits on a pull request already open.

    20-borea-listing.py
    20-borea-listing.py --loader-min 0.4.8
"""

import argparse
import json
import re
import tempfile
import time
from pathlib import Path

import ksalib
import lib

POLL_SECONDS = 30
POLL_MINUTES = 30


def set_value(text, table, key, value):
    """Set a key of a table in TOML text, keeping the rest as written."""
    lines = text.splitlines(keepends=True)
    current = None
    for index, line in enumerate(lines):
        header = re.match(r"^\[([^\]]+)\]\s*$", line)
        if header:
            current = header.group(1)
        elif current == table and re.match(rf"^{key}\s*=", line):
            lines[index] = f'{key} = "{value}"\n'
            return "".join(lines)
    raise lib.ReleaseError(f"no {key} in [{table}] of {ksalib.LISTING}")


def open_pull_request(login, environment):
    """The URL of an open pull request from the fork, or None."""
    pulls = json.loads(
        lib.capture(
            "gh",
            "pr",
            "list",
            "-R",
            ksalib.CONTENT_INDEX,
            "--author",
            login,
            "--state",
            "open",
            "--json",
            "url,headRefName",
            env=environment,
        )
    )
    for pull in pulls:
        if pull["headRefName"] == branch_name():
            return pull["url"]
    return None


def branch_name():
    return f"krpc-ksa-v{ksalib.VERSION}"


def create_pull_request(login, text, game_min, loader_min, environment):
    fork = f"git@github.com:{login}/content-index.git"
    branch = branch_name()
    with tempfile.TemporaryDirectory() as directory:
        clone = Path(directory) / "content-index"
        lib.run(
            "git",
            "clone",
            "-q",
            "--depth",
            "1",
            f"https://github.com/{ksalib.CONTENT_INDEX}.git",
            clone,
        )
        lib.run("git", "-C", clone, "checkout", "-q", "-b", branch)
        (clone / ksalib.LISTING).write_text(text)
        lib.run("git", "-C", clone, "add", ksalib.LISTING)
        lib.run(
            "git",
            "-C",
            clone,
            "commit",
            "-q",
            "-m",
            f"Update kRPC to KSA {game_min} and StarMap {loader_min}",
        )
        # Idempotent: prints that the fork already exists and exits zero if so
        lib.run(
            "gh",
            "repo",
            "fork",
            ksalib.CONTENT_INDEX,
            "--clone=false",
            env=environment,
        )
        # Forced, so re-running the step after a fix replaces the previous attempt
        lib.run("git", "-C", clone, "push", "-q", "--force", fork, branch)
    body = (
        f"Updates the bounds of kRPC for its release {ksalib.VERSION}: "
        f"KSA {game_min} or later, and StarMap {loader_min} or later."
    )
    return lib.capture(
        "gh",
        "pr",
        "create",
        "-R",
        ksalib.CONTENT_INDEX,
        "--head",
        f"{login}:{branch}",
        "--title",
        f"Update kRPC to KSA {game_min} and StarMap {loader_min}",
        "--body",
        body,
        env=environment,
    )


def wait_for_merge(url, environment):
    lib.banner("Waiting for the pull request to merge")
    print(url)
    deadline = time.monotonic() + POLL_MINUTES * 60
    while True:
        state = json.loads(
            lib.capture("gh", "pr", "view", url, "--json", "state", env=environment)
        )["state"]
        if state == "MERGED":
            return
        if state == "CLOSED":
            raise lib.ReleaseError(f"{url} was closed without merging")
        if time.monotonic() + POLL_SECONDS > deadline:
            raise lib.ReleaseError(
                f"{url} has not merged after {POLL_MINUTES} minutes; check its "
                f"checks, then re-run this step"
            )
        print(f"{lib.DIM}not merged yet; checking again in {POLL_SECONDS}s{lib.RESET}")
        time.sleep(POLL_SECONDS)


def main():
    parser = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter
    )
    parser.add_argument("--game-min", help="the oldest KSA build the release runs on")
    parser.add_argument("--loader-min", help="the oldest StarMap the release runs on")
    arguments = parser.parse_args()

    lib.require("git", "gh")
    credentials = ksalib.load_credentials()
    environment = ksalib.gh_environment(credentials)
    login, _ = lib.github_user(credentials["GITHUB_TOKEN"])

    pins = ksalib.pins()
    game_min = arguments.game_min or pins["ksa_version"]
    loader_min = arguments.loader_min or pins["starmap_version"]
    text = ksalib.listing()
    if ksalib.listing_bounds(text) == (game_min, loader_min):
        lib.banner("Done")
        print(f"The listing has KSA {game_min} and StarMap {loader_min} as its mins.")
        return

    url = open_pull_request(login, environment)
    if url is None:
        current = ksalib.listing_bounds(text)
        updated = set_value(text, "compatibility", "game_min", game_min)
        updated = set_value(updated, "loader", "min", loader_min)
        if ksalib.listing_bounds(updated) != (game_min, loader_min):
            raise lib.ReleaseError(f"could not set the bounds in {ksalib.LISTING}")

        lib.banner(f"Updating {ksalib.LISTING} in {ksalib.CONTENT_INDEX}")
        print(f"game_min:    {current[0]} -> {game_min}")
        print(f"StarMap min: {current[1]} -> {loader_min}")
        lib.confirm(f"Push to {login}/content-index and open a pull request?")
        url = create_pull_request(login, updated, game_min, loader_min, environment)

    wait_for_merge(url, environment)
    lib.banner("Done")
    print(f"Merged: {url}")
    print("Next: experimental/ksa/tools/release/30-build.py")


if __name__ == "__main__":
    ksalib.main(main)
