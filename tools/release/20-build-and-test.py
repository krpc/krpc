#!/usr/bin/env python3
"""Builds everything and runs the full headless test and lint suites locally.

Pass --expunge to start from a completely clean Bazel state first.
"""

import argparse

import lib


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('--expunge', action='store_true',
                        help='clean the Bazel output tree before building')
    arguments = parser.parse_args()

    lib.require('bazel')

    if arguments.expunge:
        lib.banner('Expunging the Bazel output tree')
        lib.run('bazel', 'clean', '--expunge')

    lib.banner('Building everything')
    lib.run('bazel', 'build', '//...')

    lib.banner('Running the test and lint suites')
    lib.run('bazel', 'test', '//:test', '//:lint')

    lib.banner('Done')
    print('Next: tools/release/30-tag.py')


if __name__ == '__main__':
    lib.main(main)
