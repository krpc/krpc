"""Builds a python sdist from a staging tree with hatchling.

  build_sdist.py --staging DIR --build DIR --hatchling PATH --out TARBALL

The staging tree contains symlinks; copy it dereferencing them into the build
dir (hatchling needs real files), run `hatchling build -t sdist` there, and copy
the resulting tarball to OUT.
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

    # Mark the build dir as a VCS boundary. hatchling walks up from here looking
    # for a .gitignore (bounded by .git) and force-includes whatever it finds into
    # the sdist; without a boundary it escapes into the Bazel exec root and bundles
    # a stray .gitignore, which breaks `pip install` of the sdist. An empty .git
    # stops that search at the build dir so no stray .gitignore is packaged.
    os.mkdir(os.path.join(opts.build, ".git"))

    # Capture hatchling's output so its "dist/<name>.tar.gz" success line does
    # not clutter the build log; only surface it if the build fails.
    result = subprocess.run(
        [hatchling, "build", "-t", "sdist"],
        cwd=opts.build,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        text=True,
    )
    if result.returncode != 0:
        sys.stderr.write(result.stdout)
        return result.returncode

    tarballs = glob.glob(os.path.join(opts.build, "dist", "*.tar.gz"))
    if len(tarballs) != 1:
        sys.stderr.write("build_sdist: expected one sdist tarball, got %r\n" % tarballs)
        return 1
    shutil.copy(tarballs[0], opts.out)
    return 0


if __name__ == "__main__":
    sys.exit(main())
