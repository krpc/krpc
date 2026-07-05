"""Runs a command and, on success, writes a stamp file.

Cross-platform replacement for the `<tool> ... && touch <stamp>` shell idiom
used by the clang-tidy / clang-format lint rules. Usage:

    run_and_stamp.py <stamp> <tool> [tool args...]

Runs `<tool> [tool args...]`; if it exits zero, creates <stamp> and exits zero,
otherwise forwards the tool's non-zero exit code.
"""

import subprocess
import sys


def main():
    stamp = sys.argv[1]
    command = sys.argv[2:]
    result = subprocess.run(command)
    if result.returncode != 0:
        return result.returncode
    with open(stamp, "w") as stamp_file:
        stamp_file.write("ok\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
