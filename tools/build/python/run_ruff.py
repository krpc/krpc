"""Runs ruff format --check and ruff check over python sources.

Usage:
  run_ruff.py --ruff-whl=WHEEL --config=RUFF_TOML FILE...

The ruff executable ships in the wheel as a data script. rules_python exposes
no target for it, so we extract it from the wheel.
"""

import argparse
import os
import stat
import subprocess
import sys
import zipfile


def extract_ruff(wheel: str) -> str:
    out_dir = os.path.join(os.environ.get("TEST_TMPDIR", "."), "ruff-bin")
    os.makedirs(out_dir, exist_ok=True)
    with zipfile.ZipFile(wheel) as whl:
        # The executable is at <name>-<version>.data/scripts/ruff[.exe]
        members = [
            name
            for name in whl.namelist()
            if "/scripts/" in name and os.path.basename(name) in ("ruff", "ruff.exe")
        ]
        if not members:
            raise RuntimeError("no ruff executable found in %s" % wheel)
        dest = os.path.join(out_dir, os.path.basename(members[0]))
        with whl.open(members[0]) as src, open(dest, "wb") as out:
            out.write(src.read())
    mode = os.stat(dest).st_mode
    os.chmod(dest, mode | stat.S_IXUSR | stat.S_IXGRP | stat.S_IXOTH)
    return dest


def run(args) -> int:
    print("+ " + " ".join(args))
    return subprocess.call(args)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--ruff-whl", required=True)
    parser.add_argument("--config", required=True)
    parser.add_argument("files", nargs="+")
    args = parser.parse_args()

    ruff = extract_ruff(args.ruff_whl)

    common = ["--config", os.path.abspath(args.config), "--no-cache"]

    result = run([ruff, "format", "--check", "--diff", *common, *args.files])
    result = run([ruff, "check", *common, *args.files]) or result
    return result


if __name__ == "__main__":
    sys.exit(main())
