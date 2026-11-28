#!/usr/bin/env python3
"""Prints the version of a release train in config.bzl, such as cpp.

Prints the readable form, which names the archives: x.y.z, or x.y.z-dev unless
KRPC_RELEASE is 1, as for a Bazel build.
"""

import argparse
import os
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent / "release"))

import versions  # noqa: E402


def main():
    # As readable_version in config.bzl writes it
    suffix = "" if os.environ.get("KRPC_RELEASE") == "1" else "-dev"
    trains = {train: version + suffix for train, version in versions.trains().items()}
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("train", choices=trains, help="the release train")
    args = parser.parse_args()
    print(trains[args.train])


if __name__ == "__main__":
    main()
