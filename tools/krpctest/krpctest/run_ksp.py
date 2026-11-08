"""Install kRPC, launch the game, and stream its kRPC log lines to the terminal.

A developer convenience for interactive runs: it installs the mod and the ``--mods``, starts
the game, follows its log printing lines that mention kRPC, and stops the game when it exits.
Run it as the ``krpc-run-ksp`` console script or with ``python -m krpctest.run_ksp``.
"""

import argparse
import subprocess
import sys

from krpctest import game, install, launch
from krpctest.version import __version__


def add_install_arguments(parser):
    """Add the options krpc-run-ksp and krpc-install share."""
    parser.add_argument(
        "-v",
        "--version",
        action="version",
        version="%s version %s" % (parser.prog, __version__),
    )
    parser.add_argument(
        "--game",
        default="ksp",
        metavar="GAME",
        help="the game: %s, or module:object for a game defined in Python code "
        "(default: ksp)" % ", ".join(sorted(game.GAMES)),
    )
    parser.add_argument(
        "--krpc", metavar="SOURCE", help="the kRPC archive to install, a path or a URL"
    )
    parser.add_argument(
        "--mods",
        default="",
        metavar="MOD,...",
        help="comma-separated managed mods to install (default: none)",
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
    parser.add_argument(
        "--skip-mod-install",
        action="store_true",
        default=False,
        help="leave the managed third-party mods in GameData untouched, to launch with "
        "a locally built mod assembly",
    )


def load_game(argv):
    """The game --game names in argv. It adds options, so it is chosen first."""
    chooser = argparse.ArgumentParser(add_help=False)
    chooser.add_argument("--game", default="ksp")
    return game.load(chooser.parse_known_args(argv)[0].game)


def apply_install_arguments(the_game, args):
    """Apply the shared options to the game, and return the mods to install."""
    game.apply(
        the_game,
        {
            "directory": args.ksp_dir,
            "krpc": args.krpc,
            "check_install": False if args.skip_gamedata_check else None,
            "install_mods": False if args.skip_mod_install else None,
        },
    )
    return [mod for mod in args.mods.split(",") if mod]


def main(argv=None):
    argv = sys.argv[1:] if argv is None else argv
    the_game = load_game(argv)
    parser = argparse.ArgumentParser(
        prog="krpc-run-ksp",
        description="Install the kRPC mod, launch the game, and stream its kRPC log lines.",
    )
    add_install_arguments(parser)
    the_game.add_run_arguments(parser)
    args = parser.parse_args(argv)
    try:
        mods = apply_install_arguments(the_game, args)
        launch_args = the_game.run_arguments(args)
        install.install(the_game, mods)
    except (ValueError, RuntimeError) as ex:
        sys.stderr.write("Error: %s\n" % ex)
        return 1
    log_path = the_game.log_path()
    previous = launch.file_stat(log_path)
    process = subprocess.Popen(
        the_game.launch_command(launch_args), cwd=the_game.game_dir()
    )
    try:
        launch.follow_log(log_path, the_game.log_filter, process, previous)
    except KeyboardInterrupt:
        pass
    finally:
        launch.terminate(the_game, process)
    return 0


if __name__ == "__main__":
    sys.exit(main())
