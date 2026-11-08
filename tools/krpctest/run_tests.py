"""Run the kRPC integration tests that need the game: `bazel run //:test-ingame`.

Arguments are passed straight through to pytest, so

    bazel run //:test-ingame -- service/SpaceCenter/test/test_camera.py -v

behaves like that pytest invocation. With no arguments the whole suite runs, as pytest.ini
points pytest at every service's test directory.

The mod, the test framework and the generated client stubs are all dependencies of this
binary, so each run tests the current sources with nothing to install first. The game is
bazel_ksp.BazelKSP, which builds kRPC and installs it into KSP_DIR.
"""

import os
import sys

import pytest


def main():
    """Run pytest against the tests in the repository."""
    # Under `bazel run` the process starts in the runfiles tree. Work in the directory bazel
    # was invoked from instead, so that test paths given on the command line mean what they
    # would for a bare pytest, and so that the framework's nested bazel calls and the tests'
    # craft fixtures resolve against the repository rather than a tree of symlinks. pytest
    # finds pytest.ini by searching upwards, so its settings apply from any subdirectory.
    workspace = os.environ.get("BUILD_WORKSPACE_DIRECTORY")
    if not workspace:
        sys.exit("This tool must be run via `bazel run //:test-ingame`.")
    os.chdir(os.environ.get("BUILD_WORKING_DIRECTORY", workspace))

    # The plugin is opt-in, so it stays out of other pytest runs where krpctest is installed
    return pytest.main(
        ["-p", "krpctest.pytest_plugin", "--game", "bazel_ksp:BazelKSP", *sys.argv[1:]]
    )


if __name__ == "__main__":
    sys.exit(main())
