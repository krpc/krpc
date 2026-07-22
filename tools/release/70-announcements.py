#!/usr/bin/env python3
"""Prints the checklist of remaining manual publishing and announcement steps.
"""

import lib

STEPS = """\
Publish the mod (upload assets/krpc-{version}.zip; for the changelog, link to
https://krpc.github.io/krpc/{version}/changelog.html):
 - [ ] CurseForge: https://www.curseforge.com/kerbal/ksp-mods/krpc-control-the-game-using-c-c-java-lua-python
 - [ ] SpaceDock: https://spacedock.info/mod/69/kRPC
 - [ ] KSP-AVC: bump the version at https://ksp-avc.cybutek.net/

Announce the release:
 - [ ] Forum release thread (update the post + post an update notice)
 - [ ] Forum development thread
 - [ ] Discord
 - [ ] Reddit\
"""


def main():
    lib.banner(f'Remaining manual steps for {lib.TAG}')
    print(STEPS.format(version=lib.VERSION))
    print()
    print('Next, once the release is announced: tools/release/80-bump-version.py')


if __name__ == '__main__':
    lib.main(main)
