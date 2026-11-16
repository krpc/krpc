#!/usr/bin/env python3
"""Prints the checklist of announcements."""

import lib

STEPS = """\
Announce the release:
 - [ ] Forum release thread (update the post + post an update notice)
 - [ ] Discord
 - [ ] Reddit\
"""


def main():
    lib.banner(f"Announcements for {lib.TAG}")
    print(STEPS)


if __name__ == "__main__":
    lib.main(main)
