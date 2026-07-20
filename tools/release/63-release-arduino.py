#!/usr/bin/env python3
"""Updates the krpc-arduino repository from the C-nano client and tags a new
release of it.

Uses the normal development git setup, which needs push access to
github.com/krpc/krpc-arduino.
"""

import lib


def main():
    lib.require('bazel', 'git')

    lib.banner('Updating the Arduino library repository')
    lib.confirm('Push updated library source to krpc/krpc-arduino?')
    lib.run('tools/update-arduino-library.sh', 'push')

    lib.banner('Tagging the Arduino library release')
    lib.confirm(f'Tag and push krpc-arduino v{lib.VERSION}?')
    lib.run('tools/update-arduino-library.sh', 'release')


if __name__ == '__main__':
    lib.main(main)
