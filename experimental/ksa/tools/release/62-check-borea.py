#!/usr/bin/env python3
"""Confirms the release reached Borea.

The Borea watcher stamps each published ksa-v* release of krpc/krpc into the
content-index-releases repository, with the bounds the listing has at the time.
This step waits for that release file, and shows the bounds it was stamped
with. It reads public endpoints only, and re-running it re-checks.
"""

import json
import time

import ksalib
import lib

POLL_SECONDS = 30
POLL_MINUTES = 30


def main():
    url = (
        f"https://raw.githubusercontent.com/{ksalib.CONTENT_INDEX_RELEASES}/main/"
        f"releases/kRPC/{ksalib.VERSION}.json"
    )
    lib.banner(f"Waiting for Borea to stamp {ksalib.VERSION}")
    print(f"Watching {url}")
    deadline = time.monotonic() + POLL_MINUTES * 60
    while True:
        text = ksalib.fetch(url)
        if text is not None:
            release = json.loads(text)
            lib.banner("Done")
            print(f"Borea has kRPC {ksalib.VERSION}:")
            print(f" KSA:     {release.get('game_min')} or later")
            print(f" StarMap: {release.get('loader', {}).get('min')} or later")
            print("Next: experimental/ksa/tools/release/70-announcements.py")
            return
        if time.monotonic() + POLL_SECONDS > deadline:
            break
        print(f"{lib.DIM}not stamped yet; checking again in {POLL_SECONDS}s{lib.RESET}")
        time.sleep(POLL_SECONDS)

    lib.warning(f"{ksalib.VERSION} has not been stamped after {POLL_MINUTES} minutes")
    print("Check the GitHub release is published, then re-run this step.")


if __name__ == "__main__":
    ksalib.main(main)
