#!/usr/bin/env python3
"""Builds everything, and runs the full headless test suite and the docs link check locally.

The build starts from a clean output tree, so its log shows every warning, and
every test runs. Pass --expunge to also discard the downloaded toolchains.
"""

import argparse

import lib


def main():
    assert __doc__ is not None
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument(
        "--expunge",
        action="store_true",
        help="expunge the whole Bazel output base, not just the output tree",
    )
    arguments = parser.parse_args()

    lib.require("bazel")

    if arguments.expunge:
        lib.banner("Expunging the Bazel output base")
        lib.run("bazel", "clean", "--expunge")
    else:
        lib.banner("Cleaning the Bazel output tree")
        lib.run("bazel", "clean")

    lib.banner("Building everything")
    lib.run("bazel", "build", "//...")

    lib.banner("Running the test suite and the link check")
    lib.run("bazel", "test", "--nocache_test_results", "//:test", "//doc:linkcheck")

    lib.banner("Done")
    print("Next: tools/release/30-tag.py")


if __name__ == "__main__":
    lib.main(main)
