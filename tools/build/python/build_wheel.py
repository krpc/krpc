"""Builds a python wheel from a staging tree with hatchling.

  build_wheel.py --staging DIR --build DIR --hatchling PATH --out WHEEL

The staging tree contains symlinks; copy it dereferencing them into the build
dir (hatchling needs real files), run `hatchling build -t wheel` there, and copy
the resulting wheel to OUT.
"""

import argparse
import glob
import os
import shutil
import stat
import subprocess
import sys


def _remove_readonly(func, path, _exception):
    """Clear the read-only attribute and try again. The build dir is copied out of the
    build's own output tree, which is read-only, and Windows refuses to delete a read-only
    file; elsewhere only the containing directory has to be writable, so this never fires."""
    os.chmod(path, stat.S_IWRITE)
    func(path)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--staging", required=True)
    parser.add_argument("--build", required=True)
    parser.add_argument("--hatchling", required=True)
    parser.add_argument("--out", required=True)
    opts = parser.parse_args()

    hatchling = os.path.abspath(opts.hatchling)

    if os.path.exists(opts.build):
        shutil.rmtree(opts.build, onexc=_remove_readonly)
    # symlinks=False dereferences the staged symlinks into real files.
    shutil.copytree(opts.staging, opts.build, symlinks=False)

    # Mark the build dir as a VCS boundary. hatchling walks up from here looking
    # for a .gitignore (bounded by .git) and leaves out the files it matches. Without
    # a boundary the search escapes into the Bazel exec root. An empty .git stops
    # that search at the build dir.
    os.mkdir(os.path.join(opts.build, ".git"))

    # Capture hatchling's output so its "dist/<name>.whl" success line does
    # not clutter the build log; only surface it if the build fails.
    result = subprocess.run(
        [hatchling, "build", "-t", "wheel"],
        cwd=opts.build,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        text=True,
        check=False,
    )
    if result.returncode != 0:
        sys.stderr.write(result.stdout)
        return result.returncode

    wheels = glob.glob(os.path.join(opts.build, "dist", "*.whl"))
    if len(wheels) != 1:
        sys.stderr.write("build_wheel: expected one wheel, got %r\n" % wheels)
        return 1
    shutil.copy(wheels[0], opts.out)
    return 0


if __name__ == "__main__":
    sys.exit(main())
