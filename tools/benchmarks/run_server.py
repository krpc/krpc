"""Run the in-game server benchmarks: `bazel run //tools/benchmarks:server`.

Arguments are passed straight through to pytest, so a single scenario or a single case can be
named the way any other test is:

    bazel run //tools/benchmarks:server -- tools/benchmarks/server/test_station.py
    bazel run //tools/benchmarks:server -- -k stream

and `--json PATH` writes the results for `//tools/benchmarks:compare` to read.

This launches KSP, loads a save and puts a craft in orbit before it measures anything, so a run
takes minutes. The measuring itself happens server side, in the TestingTools benchmark RPCs;
these scripts set up the scene, size the chunks and aggregate the samples.
"""

import argparse
import os
import sys

import pytest

# The benchmark scripts, relative to the repository root. pytest.ini points a bare run at the
# service test directories, so the suite is only collected when it is named - which is right
# for one that takes minutes and needs a craft in orbit.
BENCHMARKS = os.path.join("tools", "benchmarks", "server")


def main():
    """Run pytest against the benchmark scripts in the repository."""
    # Under `bazel run` the process starts in the runfiles tree. Work in the directory bazel
    # was invoked from instead, so that a path given on the command line means what it would
    # for a bare pytest, and so that the framework's nested bazel calls and the scripts' craft
    # fixtures resolve against the repository rather than a tree of symlinks.
    workspace = os.environ.get("BUILD_WORKSPACE_DIRECTORY")
    if not workspace:
        sys.exit("This tool must be run via `bazel run //tools/benchmarks:server`.")
    working = os.environ.get("BUILD_WORKING_DIRECTORY", workspace)
    os.chdir(working)

    options, forwarded = arguments()
    # -s so the table reaches the terminal: pytest captures output by default, and the report
    # is the point of the run rather than a detail of a failure.
    args = ["-p", "krpctest.pytest_plugin", "-s"]
    if options.json:
        args += ["--json", options.json]
    collect, named = _targets(forwarded, workspace, working)
    args += collect
    if not named:
        args.append(os.path.join(workspace, BENCHMARKS))
    return pytest.main(args)


def arguments():
    """This tool's own options, and everything else for pytest to make sense of.

    The suite is a pytest run, so the useful thing to do with an unrecognized argument is to
    hand it over: `-k`, `-v`, `-x` and whatever they take mean there what they mean anywhere
    else. What is taken here is what has to be understood before pytest sees it, which is the
    path a result file is written to, since a run that names one has not named a script.
    """
    parser = argparse.ArgumentParser(
        description=__doc__.splitlines()[0], allow_abbrev=False
    )
    parser.add_argument(
        "--json",
        metavar="PATH",
        default=None,
        help="write the benchmark results to PATH, to compare against another run",
    )
    return parser.parse_known_args()


def _targets(args, workspace, working):
    """Resolve any benchmark script named on the command line, and say whether one was named.

    A script can be named relative to the repository root, as the other runners in this
    repository take one, or relative to the directory bazel was run from; either way pytest is
    given the absolute path, since the two are only the same when a run starts at the root.
    Naming none of them runs the whole suite.
    """
    named = False
    resolved = []
    for arg in args:
        script = _script(arg, workspace, working)
        named = named or script is not None
        resolved.append(script or arg)
    return resolved, named


def _script(arg, workspace, working):
    """The path of the benchmark script an argument names, if it names one.

    A collection target may carry a `::TestClass::test_method` selector, which stays on the end
    of the path it belongs to.
    """
    target, selector, case = arg.partition("::")
    benchmarks = os.path.join(workspace, BENCHMARKS)
    for base in (working, workspace):
        path = os.path.normpath(os.path.join(base, target))
        if path.startswith(benchmarks) and os.path.exists(path):
            return path + selector + case
    return None


if __name__ == "__main__":
    sys.exit(main())
