#!/usr/bin/env python3
"""Publishes the C++ client's vcpkg port by opening a PR to microsoft/vcpkg.

Requires git, gh, a GITHUB_TOKEN, and a vcpkg git checkout at VCPKG_ROOT. Run
after the GitHub release is published: the port is pinned to the SHA-512 of the
archive attached to it.
"""

from pathlib import Path

import lib
import vcpkg


def main():
    credentials = lib.load_credentials()
    vcpkg.publish('krpc', Path('client/cpp/vcpkg-port'), credentials)


if __name__ == '__main__':
    lib.main(main)
