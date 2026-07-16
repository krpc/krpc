"""Runs protoc into a scratch directory and copies/rewrites the generated files.

Drives the protobuf codegen rules' file orchestration portably, in one step. All
paths are relative to the Bazel exec root, which is the action's working
directory.

Steps run in this fixed order:
  --mkdir DIR       recreate DIR (rm -rf then mkdir -p)      (repeatable)
  --stage SRC=DST   copy SRC into DST (a directory)          (repeatable)
  --protoc PATH -- A ...   run `PATH A ...` (protoc args follow the `--`)
  --copy GLOB=DST   copy the single file matching GLOB to DST (repeatable)
  --rewrite DST=PATTERN=REPL   regex-substitute in place in DST (repeatable)
"""

import argparse
import glob
import os
import re
import shutil
import subprocess
import sys


def main():
    argv = sys.argv[1:]
    # Everything after a `--` sentinel is passed verbatim to protoc; keeping it
    # out of argparse avoids protoc flags (e.g. --csharp_out=...) being mistaken
    # for this script's own options.
    if "--" in argv:
        split = argv.index("--")
        pre_args, protoc_args = argv[:split], argv[split + 1:]
    else:
        pre_args, protoc_args = argv, []

    parser = argparse.ArgumentParser()
    parser.add_argument("--protoc", required=True)
    parser.add_argument("--mkdir", action="append", default=[])
    parser.add_argument("--stage", action="append", default=[])
    parser.add_argument("--copy", action="append", default=[])
    parser.add_argument("--rewrite", action="append", default=[])
    opts = parser.parse_args(pre_args)

    for directory in opts.mkdir:
        if os.path.exists(directory):
            shutil.rmtree(directory)
        os.makedirs(directory)

    for spec in opts.stage:
        src, dst = spec.split("=", 1)
        shutil.copy(src, dst)

    result = subprocess.run([opts.protoc] + protoc_args)
    if result.returncode != 0:
        return result.returncode

    for spec in opts.copy:
        pattern, dst = spec.split("=", 1)
        matches = glob.glob(pattern, recursive=True)
        if len(matches) != 1:
            sys.stderr.write(
                "run_protoc: expected exactly one file matching %r, got %r\n"
                % (pattern, matches)
            )
            return 1
        shutil.copy(matches[0], dst)

    for spec in opts.rewrite:
        dst, pattern, repl = spec.split("=", 2)
        with open(dst) as handle:
            text = handle.read()
        text = re.sub(pattern, repl, text)
        with open(dst, "w") as handle:
            handle.write(text)

    return 0


if __name__ == "__main__":
    sys.exit(main())
