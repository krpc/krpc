"""Runs basedpyright over python sources.

Usage:
  run_pyright.py --config=PYRIGHTCONFIG [--python-version=X.Y]
                 [--disable=RULE]... FILE...

basedpyright is pyright packaged as a wheel, with node from the
nodejs-wheel-binaries wheel, so it runs without a download step.

pyright resolves imports through its search paths and ignores PYTHONPATH. We
generate a project config whose extraPaths are the directories of the scripts
outside a package, for their sibling imports, then the test's dependency roots
on sys.path.
"""

import argparse
import json
import os
import subprocess
import sys


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--config", required=True)
    parser.add_argument("--python-version")
    parser.add_argument("--disable", action="append", default=[])
    parser.add_argument("files", nargs="+")
    args = parser.parse_args()

    with open(args.config) as f:
        config = json.load(f)
    if args.python_version:
        config["pythonVersion"] = args.python_version
    for rule in args.disable:
        config[rule] = "none"
    directories = {os.path.dirname(os.path.abspath(path)) for path in args.files}
    scripts = sorted(
        path
        for path in directories
        if not os.path.exists(os.path.join(path, "__init__.py"))
    )
    config["extraPaths"] = scripts + [path for path in sys.path if os.path.isdir(path)]
    # include entries are relative to the generated project directory
    config["include"] = [os.path.abspath(path) for path in args.files]

    project = os.path.join(os.environ.get("TEST_TMPDIR", "."), "pyright")
    os.makedirs(project, exist_ok=True)
    with open(os.path.join(project, "pyrightconfig.json"), "w") as f:
        json.dump(config, f)

    command = [sys.executable, "-m", "basedpyright", "--project", project]
    print("+ " + " ".join(command))
    return subprocess.call(command)


if __name__ == "__main__":
    sys.exit(main())
