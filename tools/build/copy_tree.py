"""Copies files into a directory, at their paths relative to a base directory.

  copy_tree.py --out OUT --base BASE FILE...

Arguments can be read from a file, one per line, given as @PATH.
"""

import argparse
import os
import shutil
import sys


def main():
    parser = argparse.ArgumentParser(fromfile_prefix_chars="@")
    parser.add_argument("--out", required=True)
    parser.add_argument("--base", required=True)
    parser.add_argument("files", nargs="*")
    opts = parser.parse_args()

    for src in opts.files:
        dst = os.path.join(opts.out, os.path.relpath(src, opts.base))
        os.makedirs(os.path.dirname(dst), exist_ok=True)
        shutil.copyfile(src, dst)
    return 0


if __name__ == "__main__":
    sys.exit(main())
