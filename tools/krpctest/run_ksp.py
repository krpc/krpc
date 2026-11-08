"""Install kRPC built from this repository, run KSP and follow its log:
`bazel run //:run-ksp -- [krpc-run-ksp options]`.
"""

import os
import sys

from krpctest import run_ksp


def main():
    if not os.environ.get("BUILD_WORKSPACE_DIRECTORY"):
        sys.exit("This tool must be run via `bazel run //:run-ksp`.")
    # Under `bazel run` the process starts in the runfiles tree. Paths given on the command
    # line mean what they would where bazel was run.
    os.chdir(
        os.environ.get(
            "BUILD_WORKING_DIRECTORY", os.environ["BUILD_WORKSPACE_DIRECTORY"]
        )
    )
    return run_ksp.main(["--game", "bazel_ksp:BazelKSP", *sys.argv[1:]])


if __name__ == "__main__":
    sys.exit(main())
