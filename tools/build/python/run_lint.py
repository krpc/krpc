"""Runs black --check and pylint over python sources.

Usage:
  run_lint.py [--sdist=TARBALL --pkg=NAME]
              [--black-exclude=REGEX] [--pylint-rcfile=PATH] [FILE]...

Either lints the package NAME inside an extracted sdist, or the given list
of files. Dependencies needed by pylint to resolve imports must be provided
as deps of the py_test target so they are already importable.
"""

import os
import subprocess
import sys
import tarfile


def run(args) -> int:
    print("+ " + " ".join(args))
    return subprocess.call([sys.executable, "-m"] + args)


def main() -> int:
    sdist = None
    pkg = None
    black_exclude = None
    pylint_rcfile = None
    files = []
    for arg in sys.argv[1:]:
        if arg.startswith("--sdist="):
            sdist = arg.split("=", 1)[1]
        elif arg.startswith("--pkg="):
            pkg = arg.split("=", 1)[1]
        elif arg.startswith("--black-exclude="):
            black_exclude = arg.split("=", 1)[1]
        elif arg.startswith("--pylint-rcfile="):
            pylint_rcfile = arg.split("=", 1)[1]
        else:
            files.append(arg)

    black_args = ["black", "--check"]
    if black_exclude:
        black_args.extend(["--extend-exclude", black_exclude])
    pylint_args = ["pylint"]
    if pylint_rcfile:
        pylint_args.append("--rcfile=%s" % os.path.abspath(pylint_rcfile))

    if sdist:
        extract_dir = os.path.join(os.environ.get("TEST_TMPDIR", "."), "sdist")
        with tarfile.open(sdist) as tar:
            tar.extractall(extract_dir)
        # An sdist contains a single root directory, <name>-<version>
        pkg_root = os.path.join(extract_dir, os.listdir(extract_dir)[0])
        # Make the package under lint importable, so pylint can resolve it
        sys.path.insert(0, pkg_root)
        os.environ["PYTHONPATH"] = os.pathsep.join(sys.path)
        black_args.append(os.path.join(pkg_root, pkg))
        pylint_args.append(pkg)
    else:
        os.environ["PYTHONPATH"] = os.pathsep.join(sys.path)
        black_args.extend(files)
        pylint_args.extend(files)

    os.environ.setdefault("PYLINTHOME", os.path.join(os.environ.get("TEST_TMPDIR", "."), "pylint"))

    result = run(black_args)
    result = run(pylint_args) or result
    return result


if __name__ == "__main__":
    sys.exit(main())
