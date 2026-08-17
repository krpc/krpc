"""The command line and the output every TestServer-based benchmark runner shares."""

import argparse
import os

from tools.benchmarks.report import table, write_json


def arguments(description):
    parser = argparse.ArgumentParser(description=description)
    parser.add_argument(
        "--server",
        metavar="PATH",
        default=None,
        help="the TestServer executable to run (the bazel target supplies this)",
    )
    parser.add_argument(
        "--json",
        metavar="PATH",
        default=None,
        help="write the results to PATH, to compare against another run",
    )
    return parser.parse_args()


def report(results, suite, environment, json_path):
    """Print the table, and write the results out when asked for."""
    for line in table(results, suite, environment):
        print(line)
    if json_path:
        written = path(json_path)
        write_json(results, written, suite, environment)
        print("  written to %s" % written)


def path(name):
    """Resolve a path on the command line the way whoever typed it meant it.

    A py_binary under `bazel run` starts in the runfiles tree, so a relative path would
    otherwise be read from, or written into, a directory of symlinks that the next build
    replaces.
    """
    if os.path.isabs(name):
        return name
    return os.path.join(os.environ.get("BUILD_WORKING_DIRECTORY", os.getcwd()), name)
