"""Run the kRPC for KSA integration tests:
`bazel run //experimental/ksa/tools/game:test-ksa`.

Arguments are passed straight through to pytest, so

    bazel run //experimental/ksa/tools/game:test-ksa -- \
        experimental/ksa/service/KSA/test/test_orbit.py -v

behaves like that pytest invocation. With no arguments every test in experimental/ksa runs.
The game is bazel_ksa.BazelKSA, which builds the mod and installs it for KSA_DIR.
"""

import os
import sys

import pytest


def is_test_path(arg, ksa):
    """Whether the argument names a file or folder of tests under the ksa folder."""
    path = os.path.realpath(arg.split("::")[0])
    ksa = os.path.realpath(ksa)
    return os.path.exists(path) and os.path.commonpath([path, ksa]) == ksa


def main():
    workspace = os.environ.get("BUILD_WORKSPACE_DIRECTORY")
    if not workspace:
        sys.exit(
            "This tool must be run via `bazel run //experimental/ksa/tools/game:test-ksa`."
        )
    # Test paths on the command line mean what they would where bazel was run
    os.chdir(os.environ.get("BUILD_WORKING_DIRECTORY", workspace))
    ksa = os.path.join(workspace, "experimental", "ksa")
    args = sys.argv[1:]
    # pytest reads testpaths only when run from its rootdir, so with no test paths given we
    # name the test directories. A test path is under experimental/ksa, unlike an option value
    # such as the folder of --basetemp.
    if not any(is_test_path(arg, ksa) for arg in args):
        args += [
            os.path.join(ksa, "server", "test"),
            os.path.join(ksa, "service", "KSA", "test"),
        ]
    return pytest.main(
        [
            "-c",
            os.path.join(ksa, "pytest.ini"),
            "--rootdir",
            ksa,
            "-p",
            "krpctools.game.pytest_plugin",
            "--game",
            "bazel_ksa:BazelKSA",
            *args,
        ]
    )


if __name__ == "__main__":
    sys.exit(main())
