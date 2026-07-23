"""Checks that cog-generated sources are up to date, writing a stamp on success.

Some C/C++ client sources embed cog (https://cog.readthedocs.io) generator blocks
that expand into repetitive code committed alongside the generators. The committed
output is cog's expansion reformatted with clang-format, so this reproduces both
steps -- regenerate with cog, then reformat with clang-format -- in a scratch copy
and compares the result against the checked-in file. A mismatch means a generator
block was edited without regenerating.

Usage:

    run_cog_check.py <stamp> <clang_format> <config> <src>...
"""

import difflib
import os
import shutil
import subprocess
import sys
import tempfile

from cogapp import Cog


def regenerate(clang_format, config, src):
    """Return src as its generator blocks would produce it, cog then clang-format."""
    with tempfile.TemporaryDirectory() as tmp:
        copy = os.path.join(tmp, os.path.basename(src))
        shutil.copyfile(src, copy)
        Cog().main(["cog", "-r", copy])
        subprocess.run(
            [clang_format, "-i", "--style=file:" + config, copy],
            check=True,
        )
        with open(copy) as regenerated:
            return regenerated.read()


def main():
    stamp, clang_format, config = sys.argv[1:4]
    srcs = sys.argv[4:]
    stale = []
    for src in srcs:
        with open(src) as committed:
            current = committed.read()
        expected = regenerate(clang_format, config, src)
        if current != expected:
            stale.append(src)
            sys.stderr.writelines(
                difflib.unified_diff(
                    current.splitlines(keepends=True),
                    expected.splitlines(keepends=True),
                    fromfile=src + " (committed)",
                    tofile=src + " (regenerated)",
                )
            )
    if stale:
        sys.stderr.write(
            "\nThe above sources are out of date with their cog generator blocks. "
            "Regenerate each with `cog -r <file>` and reformat it with clang-format.\n"
        )
        return 1
    with open(stamp, "w") as stamp_file:
        stamp_file.write("ok\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
