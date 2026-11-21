"""Install kRPC for KSA built from this repository, run KSA and follow its log:
`bazel run //experimental/ksa/tools/game:run-ksa -- [krpc-game run options]`.
"""

import os
import sys

from krpctools.game import cli


def main():
    workspace = os.environ.get("BUILD_WORKSPACE_DIRECTORY")
    if not workspace:
        sys.exit(
            "This tool must be run via `bazel run //experimental/ksa/tools/game:run-ksa`."
        )
    os.chdir(os.environ.get("BUILD_WORKING_DIRECTORY", workspace))
    return cli.main(["run", "--game", "bazel_ksa:BazelKSA", *sys.argv[1:]])


if __name__ == "__main__":
    sys.exit(main())
