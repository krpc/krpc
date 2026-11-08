"""krpc-game: install kRPC into a game, run the game, and stop it.

    krpc-game install --mods=RemoteTech
    krpc-game run --load-game=krpctest
    krpc-game stop

``--game`` chooses the game, as it does for the pytest plugin, and defaults to ksp. The
options ``run`` takes beyond the shared ones come from the game.
"""

import argparse
import json
import os
import signal
import subprocess
import sys
import time
import zipfile

from . import game, install, launch, pytest_plugin
from .version import __version__


class _Options:
    """An argparse parser with the addoption method the plugin's options are added with."""

    def __init__(self, parser):
        self.parser = parser

    def addoption(self, *args, **kwargs):
        self.parser.add_argument(*args, **kwargs)


def _state_path(the_game):
    """The file recording the game krpc-game run launched."""
    return os.path.join(install.cache_dir(), "run", the_game.name + ".json")


def _parser(the_game):
    parser = argparse.ArgumentParser(
        prog="krpc-game",
        description="Install kRPC into a game, run the game, and stop it.",
    )
    parser.add_argument(
        "-v", "--version", action="version", version="krpc-game " + __version__
    )
    # Taken before or after the command. The subcommands list it in their help.
    parser.add_argument("--game", help=argparse.SUPPRESS)
    shared = argparse.ArgumentParser(add_help=False)
    pytest_plugin.add_options(_Options(shared), launching=False)
    shared.add_argument(
        "--mods",
        default="",
        metavar="MOD,...",
        help="comma-separated mods to install: %s (default: none)"
        % ", ".join(sorted(the_game.mods)),
    )
    commands = parser.add_subparsers(dest="command", required=True)
    commands.add_parser(
        "install",
        parents=[shared],
        help="install kRPC, the test add-on and the given mods, and remove other mods",
    )
    run = commands.add_parser(
        "run",
        parents=[shared],
        help="install, launch the game and follow its log until it exits",
    )
    the_game.add_run_arguments(run)
    commands.add_parser(
        "stop", parents=[shared], help="stop the game krpc-game run launched"
    )
    return parser


def _runs(pid, executable):
    """Whether the process with the given ID runs the executable. A record left by a run
    that was killed names a process ID that another process may since have taken."""
    if sys.platform == "win32":
        command = ["tasklist", "/fi", "PID eq %d" % pid, "/fo", "csv", "/nh"]
        name = os.path.basename(executable).lower()
    else:
        command = ["ps", "-p", str(pid), "-o", "args="]
        name = executable.lower()
    try:
        output = subprocess.run(command, capture_output=True, text=True, check=False)
    except OSError:
        return False
    return name in output.stdout.lower()


def _stop(the_game):
    path = _state_path(the_game)
    try:
        with open(path, encoding="utf-8") as f:
            state = json.load(f)
    except (OSError, ValueError):
        sys.stderr.write("No game launched by krpc-game run is recorded\n")
        return 1
    os.remove(path)
    pid = state["pid"]
    if not _runs(pid, state["executable"]):
        sys.stderr.write("The game krpc-game run launched is no longer running\n")
        return 1
    if sys.platform == "win32":
        subprocess.call(["taskkill", "/f", "/pid", str(pid)])
        return 0
    try:
        os.kill(pid, signal.SIGTERM)
        deadline = time.time() + 30
        while time.time() < deadline:
            os.kill(pid, 0)
            time.sleep(0.5)
        os.kill(pid, signal.SIGKILL)
    except ProcessLookupError:
        pass
    return 0


def _run(the_game, mods, args):
    launch_args = the_game.run_arguments(args)
    the_game.install(mods)
    log_path = the_game.log_path()
    previous = launch.file_stat(log_path)
    command = the_game.launch_command(launch_args)
    process = subprocess.Popen(command, cwd=the_game.game_dir())
    state = _state_path(the_game)
    os.makedirs(os.path.dirname(state), exist_ok=True)
    with open(state, "w", encoding="utf-8") as f:
        json.dump({"pid": process.pid, "executable": command[0]}, f)
    try:
        launch.follow_log(log_path, the_game.log_filter, process, previous)
    except KeyboardInterrupt:
        pass
    finally:
        launch.terminate(the_game, process)
        if os.path.exists(state):
            os.remove(state)
    return 0


def main(argv=None):
    argv = sys.argv[1:] if argv is None else argv
    # The game adds options, so it is chosen first
    chooser = argparse.ArgumentParser(add_help=False)
    chooser.add_argument("--game", default="ksp")
    spec = chooser.parse_known_args(argv)[0].game
    try:
        the_game = game.load(spec)
    except (ImportError, AttributeError, ValueError) as ex:
        sys.stderr.write("Error: %s\n" % ex)
        return 1
    args = _parser(the_game).parse_args(argv)
    mods = [mod for mod in args.mods.split(",") if mod]
    try:
        options = pytest_plugin.options(lambda name: getattr(args, name, None))
        del options["game"]
        game.apply(the_game, options)
        if args.command == "stop":
            return _stop(the_game)
        if args.command == "install":
            the_game.install(mods)
            return 0
        return _run(the_game, mods, args)
    except (
        ValueError,
        RuntimeError,
        OSError,
        zipfile.BadZipFile,
        subprocess.CalledProcessError,
    ) as ex:
        sys.stderr.write("Error: %s\n" % ex)
        return 1


if __name__ == "__main__":
    sys.exit(main())
