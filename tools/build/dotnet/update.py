"""Regenerates paket.lock and the paket.*.bzl files from paket.dependencies.

Usage:
  bazel run //tools/build/dotnet:update

The paket and paket2bazel arguments are rlocation paths.
"""

import argparse
import os
import subprocess
from pathlib import Path

from python.runfiles import runfiles


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--paket", required=True)
    parser.add_argument("--paket2bazel", required=True)
    args = parser.parse_args()

    r = runfiles.Create()
    if r is None:
        raise RuntimeError("runfiles not found")
    paket = r.Rlocation(args.paket)
    paket2bazel = r.Rlocation(args.paket2bazel)
    if paket is None or paket2bazel is None:
        raise RuntimeError("paket or paket2bazel not found in the runfiles")
    package = Path(os.environ["BUILD_WORKSPACE_DIRECTORY"]) / "tools/build/dotnet"

    environment = dict(os.environ, **r.EnvVars())
    subprocess.run(
        [paket, "update", "--no-install"],
        check=True,
        cwd=package,
        env=environment,
    )
    subprocess.run(
        [
            paket2bazel,
            "--dependencies-file",
            str(package / "paket.dependencies"),
            "--output-folder",
            str(package),
        ],
        check=True,
        env=environment,
    )


if __name__ == "__main__":
    main()
