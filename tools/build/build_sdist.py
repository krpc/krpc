"""Builds a python sdist from a staging tree with hatchling.

  build_sdist.py --staging DIR --build DIR --hatchling PATH --out TARBALL

The staging tree contains symlinks; copy it dereferencing them into the build
dir (hatchling needs real files), run `hatchling build -t sdist` there, and copy
the resulting tarball to OUT. Cross-platform replacement for the sdist rule's
`cp -rL` / `cd` / hatchling shell pipeline.
"""

import argparse
import glob
import os
import shutil
import subprocess
import sys


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--staging", required=True)
    parser.add_argument("--build", required=True)
    parser.add_argument("--hatchling", required=True)
    parser.add_argument("--out", required=True)
    opts = parser.parse_args()

    hatchling = os.path.abspath(opts.hatchling)

    if os.path.exists(opts.build):
        shutil.rmtree(opts.build)
    # symlinks=False dereferences the staged symlinks into real files.
    shutil.copytree(opts.staging, opts.build, symlinks=False)

    result = subprocess.run([hatchling, "build", "-t", "sdist"], cwd=opts.build)
    if result.returncode != 0:
        return result.returncode

    tarballs = glob.glob(os.path.join(opts.build, "dist", "*.tar.gz"))
    if len(tarballs) != 1:
        sys.stderr.write("build_sdist: expected one sdist tarball, got %r\n" % tarballs)
        return 1
    shutil.copy(tarballs[0], opts.out)
    return 0


if __name__ == "__main__":
    sys.exit(main())
