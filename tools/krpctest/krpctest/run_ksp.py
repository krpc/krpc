"""Install the kRPC mod, launch KSP, and stream its kRPC log lines to the terminal.

This is the Python port of the former ``tools/run-ksp.sh``. It is a developer convenience
for interactive runs: it installs the mod (reconciling any ``--mods``), starts KSP, follows
``KSP.log`` printing lines that mention kRPC, and stops KSP when it exits.

Run it from the repository root, either as the ``krpc-run-ksp`` console script or with
``python -m krpctest.run_ksp``.
"""

import argparse
import os
import subprocess
import sys
import time
from importlib.resources import files

from krpctest.env import get_ksp_dir
from krpctest.game import copy_blank_save
from krpctest.install import MODS, install
from krpctest.version import __version__

# The Linux binary name, log location and pkill fallback below are inherited from the
# existing test framework (krpctest.__init__). They are the natural seam for future
# cross-platform (Windows/macOS) support.
_KSP_BINARY = "KSP.x86_64"
_KSP_LOG = "KSP.log"


def _stage_blank_save(name, ksp_dir):
    """Create the save --load-game asked for, when it is one of the saves bundled with the
    package (krpctest, krpctest_career) and the KSP install does not have it yet. The test
    framework stages these when it launches the game itself, so without this a manual run
    against an install that has never run the tests reaches the main menu with nothing to
    load. An existing save is left alone: it is the state being iterated on."""
    if not files("krpctest").joinpath(name + ".sfs").is_file():
        return
    if os.path.exists(os.path.join(ksp_dir, "saves", name)):
        return
    copy_blank_save(name, ksp_dir)


def _terminate(proc):
    """Stop the launched KSP process, mirroring the bash `trap 'kill' EXIT`."""
    if proc.poll() is not None:
        return
    try:
        proc.terminate()
        proc.wait(timeout=30)
    except Exception:  # pylint: disable=broad-except
        try:
            proc.kill()
            proc.wait(timeout=10)
        except Exception:  # pylint: disable=broad-except
            pass
    if proc.poll() is None:
        subprocess.call(["pkill", "-f", "KSP[.]x86_64"])


def _follow_log(path, needle, proc):
    """Print appended lines of the log at path that contain needle (case-insensitive),
    a small stand-in for `tail -f path | grep -i needle`. Returns when KSP exits."""
    needle = needle.lower()
    # Wait for KSP to create the log; give up if it exits first.
    while not os.path.exists(path):
        if proc.poll() is not None:
            return
        time.sleep(0.5)
    with open(path, encoding="utf-8", errors="replace") as log:
        while True:
            line = log.readline()
            if line:
                if needle in line.lower():
                    sys.stdout.write(line)
                    sys.stdout.flush()
                continue
            if proc.poll() is not None:
                return
            time.sleep(0.1)


# Runner --load-* options mapped to the TestingTools --krpctest-load-* arguments they
# are forwarded to. Every option shows up in --help and nothing is passed to KSP
# blind. Order here is the order the arguments are forwarded in.
_LOAD_OPTIONS = (
    ("load_game", "--krpctest-load-game"),
    ("load_save", "--krpctest-load-save"),
    ("load_vessel", "--krpctest-load-vessel"),
    ("load_craft", "--krpctest-load-craft"),
    ("load_craft_directory", "--krpctest-load-craft-directory"),
    ("load_craft_fixture_dir", "--krpctest-load-craft-fixture-dir"),
    ("load_launch_site", "--krpctest-load-launch-site"),
)


def main():
    parser = argparse.ArgumentParser(
        prog="krpc-run-ksp",
        description=(
            "Install the kRPC mod, launch KSP, and stream its kRPC log lines. "
            "Run from the repository root."
        ),
        epilog=(
            "The --load-* options configure the TestingTools add-on; each is "
            "forwarded to KSP as the matching --krpctest-load-* argument. Auto-load "
            "happens only when at least one --load-* option is given; with none, "
            "KSP stays at the main menu. See tools/krpctest/README.md for details."
        ),
    )
    parser.add_argument(
        "-v",
        "--version",
        action="version",
        version="%s version %s" % ("krpc-run-ksp", __version__),
    )
    parser.add_argument(
        "--mods",
        default="",
        metavar="MOD,...",
        help="comma-separated managed mods to install: %s "
        "(default: none)" % ", ".join(sorted(MODS)),
    )
    parser.add_argument(
        "--ksp-dir",
        default=None,
        metavar="DIR",
        help="path to the KSP install (defaults to $KSP_DIR)",
    )
    parser.add_argument(
        "--skip-gamedata-check",
        action="store_true",
        default=False,
        help="skip the check that GameData holds only the known-valid set, to launch "
        "with an unmanaged mod installed",
    )

    autoload = parser.add_argument_group(
        "auto-load options",
        "Configure the TestingTools auto-load. Passing any one of these makes KSP "
        "load a save on reaching the main menu; with none, KSP stays at the main menu.",
    )
    autoload.add_argument(
        "--load-game",
        metavar="FOLDER",
        help="save folder under saves/ to load (TestingTools default: default)",
    )
    autoload.add_argument(
        "--load-save",
        metavar="NAME",
        help="save file name without .sfs (TestingTools default: persistent)",
    )
    autoload.add_argument(
        "--load-vessel",
        metavar="INDEX",
        type=int,
        help="vessel index to focus after loading the save",
    )
    autoload.add_argument(
        "--load-craft",
        metavar="NAME",
        help="craft to launch after loading the save (e.g. Parts or Parts.craft)",
    )
    autoload.add_argument(
        "--load-craft-directory",
        metavar="VAB|SPH",
        choices=["VAB", "SPH"],
        help="KSP craft directory and launch facility (default: inferred)",
    )
    autoload.add_argument(
        "--load-craft-fixture-dir",
        metavar="PATH",
        help="source directory of fixture .craft files to stage into the save",
    )
    autoload.add_argument(
        "--load-launch-site",
        metavar="SITE",
        help="launch site, for example LaunchPad or Runway "
        "(default: by craft directory)",
    )

    args = parser.parse_args()
    mods = [m for m in args.mods.split(",") if m]

    # Build the KSP argument list explicitly from the parsed options; only forward
    # the auto-load arguments that were actually supplied, so an unspecified option
    # never reaches KSP and never triggers auto-load on its own.
    ksp_args = []
    for dest, flag in _LOAD_OPTIONS:
        value = getattr(args, dest)
        if value is not None:
            ksp_args.append("%s=%s" % (flag, value))

    ksp_dir = get_ksp_dir(args.ksp_dir)
    install(mods=mods, ksp_dir=ksp_dir, validate_gamedata=not args.skip_gamedata_check)
    if args.load_game is not None:
        _stage_blank_save(args.load_game, ksp_dir)

    proc = subprocess.Popen(  # pylint: disable=consider-using-with
        [os.path.join(ksp_dir, _KSP_BINARY), *ksp_args], cwd=ksp_dir
    )
    try:
        _follow_log(os.path.join(ksp_dir, _KSP_LOG), "krpc", proc)
    except KeyboardInterrupt:
        pass
    finally:
        _terminate(proc)
    return 0


if __name__ == "__main__":
    sys.exit(main())
