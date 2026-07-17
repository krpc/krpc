"""Runs a command and, on success, writes a stamp file.

Used by the clang-tidy / clang-format lint rules to gate a stamp file on the
tool succeeding, portably across platforms. Usage:

    run_and_stamp.py <stamp> <tool> [tool args...]

Runs `<tool> [tool args...]`; if it exits zero, creates <stamp> and exits zero,
otherwise forwards the tool's non-zero exit code.

The tool's output is captured and only replayed on failure. On success it is
discarded -- clang-tidy prints per-file progress and a cumulative "N warnings
generated." summary (counting diagnostics in filtered-out headers) even when
the check passes, which Bazel would otherwise echo into the build log.
"""

import subprocess
import sys


def main():
    stamp = sys.argv[1]
    command = sys.argv[2:]
    result = subprocess.run(
        command,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        text=True,
    )
    if result.returncode != 0:
        sys.stderr.write(result.stdout)
        return result.returncode
    with open(stamp, "w") as stamp_file:
        stamp_file.write("ok\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
