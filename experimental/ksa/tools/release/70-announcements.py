#!/usr/bin/env python3
"""Prints the forum post for the release, and the remaining manual steps."""

import ksalib
import lib


def main():
    pins = ksalib.pins()
    release = f"https://github.com/krpc/krpc/releases/tag/{ksalib.TAG}"
    spacedock = f"https://spacedock.info/mod/{ksalib.SPACEDOCK_MOD}/kRPC"
    changes = ksalib.notes().removeprefix("## Changes\n\n")

    lib.banner("Forum post")
    print(f"kRPC for KSA v{ksalib.VERSION} is out, for KSA {pins['ksa_version']}")
    print(f"and StarMap {pins['starmap_version']} or later.")
    print()
    print("Install it with the Borea mod manager, or download it from")
    print(f"GitHub ({release}) or SpaceDock ({spacedock}).")
    print(f"The documentation is at {ksalib.DOCS}")
    print()
    print("Changes:")
    print()
    print(changes)

    lib.banner(f"Remaining manual steps for {ksalib.TAG}")
    print(f" - [ ] Post the above on the forum thread: {ksalib.FORUM_THREAD}")
    print("       and update its first post")


if __name__ == "__main__":
    ksalib.main(main)
