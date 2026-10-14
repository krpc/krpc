"""Runs buildifier over the Bazel files in the source tree.

Usage:
  run_buildifier.py --buildifier=RLOCATION --workspace=RLOCATION

Both arguments are rlocation paths: the buildifier executable, and a file at
the root of the source tree. The test runs unsandboxed, so the real path of
that file is the source tree, and we scan it directly. Bazel cannot glob
across packages, so declaring each Bazel file as an input is impractical.
"""

import argparse
import os
import subprocess
import sys

from python.runfiles import runfiles

# Bazel files by extension, plus the bare BUILD
EXTENSIONS = (".bazel", ".bzl", ".BUILD")

# Files per buildifier invocation, to stay under the Windows command-line limit
BATCH_SIZE = 100


def find_files(root: str) -> list[str]:
    files = []
    for directory, subdirs, names in os.walk(root):
        # Skip the git directory and the bazel output symlinks, which are
        # junctions on Windows that os.walk descends into
        subdirs[:] = sorted(
            x for x in subdirs if x != ".git" and not x.startswith("bazel-")
        )
        for name in sorted(names):
            if name == "BUILD" or name.endswith(EXTENSIONS):
                files.append(os.path.relpath(os.path.join(directory, name), root))
    return files


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--buildifier", required=True)
    parser.add_argument("--workspace", required=True)
    args = parser.parse_args()

    r = runfiles.Create()
    assert r is not None
    buildifier = r.Rlocation(args.buildifier)
    workspace = r.Rlocation(args.workspace)
    assert buildifier is not None and workspace is not None
    root = os.path.dirname(os.path.realpath(workspace))

    files = find_files(root)
    if not files:
        print("no Bazel files found in %s" % root)
        return 1

    # The buildifier wrapper finds its binary relative to the runfiles
    # directory, our working directory, then changes to the source tree
    environment = dict(os.environ, BUILD_WORKSPACE_DIRECTORY=root)

    result = 0
    for i in range(0, len(files), BATCH_SIZE):
        batch = files[i : i + BATCH_SIZE]
        cmd = [buildifier, "-mode=check", "-lint=warn", *batch]
        result = subprocess.call(cmd, env=environment) or result
    print("checked %d files in %s" % (len(files), root))
    return result


if __name__ == "__main__":
    sys.exit(main())
