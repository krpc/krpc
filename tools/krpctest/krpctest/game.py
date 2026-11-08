"""The base classes a game and its mods extend, and the game a run tests against.

A ``Game`` installs kRPC, the game's test add-on and the requested mods, launches the game
and connects to its server. A game subclass overrides the methods that differ between games:
where mods install, the command that launches it, the log it writes and how to tell a mod is
present in the running game. Launching and stopping are in :mod:`krpctest.launch`, and
installing in :mod:`krpctest.install`.
"""

import importlib
import os

from . import install, launch


class Mod:
    """A mod a test can require, by name in its ``mods`` list.

    ``source`` is where the mod comes from: a directory, an archive, or the URL of an archive.
    A subclass sets the pinned ``source`` and its ``sha256``, and an instance given a source
    installs that one in their place. ``requires`` names the mods installed alongside it.
    """

    name = ""
    source = None
    sha256 = None
    requires = ()

    def __init__(self, source=None, sha256=None):
        if source is not None:
            self.source = source
            self.sha256 = sha256

    def install(self, game, path):
        """Install the mod from its fetched source at path."""
        game.install_mod(self, path)

    def remove(self, game):
        """Remove the mod from the game's install."""
        game.remove_mod(self)

    def present(self, game, conn):
        """Whether the running game has the mod loaded, or None when that cannot be told."""
        return game.mod_present(self, conn)

    def __repr__(self):
        return "%s(%r)" % (type(self).__name__, self.source)


class Game:
    """A game that kRPC runs in, and how to install, launch and stop it.

    The constructor arguments override the defaults a subclass sets: the install directory,
    the kRPC archive and the mods. Each mod replaces the game's mod of the same name.
    """

    # The name --game takes for this game
    name = ""

    # The environment variable naming the install directory
    directory_variable = ""

    # The name the tests connect with
    client_name = "krpctest"

    def __init__(self, directory=None, krpc=None, mods=(), address=None):
        self.directory = directory
        self.krpc = krpc
        self.mods = {mod.name: mod for mod in self.default_mods()}
        for mod in mods:
            self.mods[mod.name] = mod
        self.address = address or os.environ.get("KRPC_TEST_ADDRESS", "127.0.0.1")
        # Whether ensure launches a game when no server is reachable
        self.auto_launch = os.environ.get("KRPC_TEST_AUTO_LAUNCH", "1") != "0"
        # Whether install checks that the install holds only the known set
        self.check_install = os.environ.get("KRPC_TEST_SKIP_GAMEDATA_CHECK", "0") != "1"
        # Whether install reconciles the mods, or leaves them as they are
        self.install_mods = os.environ.get("KRPC_TEST_SKIP_MOD_INSTALL", "0") != "1"
        # The process this run launched, or None when the game was started elsewhere
        self.process = None
        self.connection = None
        # Mod sets that were launched but did not load, so ensure fails fast on a retry
        self.unsatisfiable = set()
        # Whether stop runs at exit
        self.stops_at_exit = False

    # Locations

    def game_dir(self):
        """The install directory, from the constructor or the game's environment variable."""
        directory = self.directory or os.environ.get(self.directory_variable)
        if not directory:
            raise RuntimeError(
                "No %s install specified. Set %s, or pass --ksp-dir, to the path of the "
                "install." % (self.title(), self.directory_variable)
            )
        if not os.path.isdir(directory):
            raise RuntimeError("%s install not found at %s" % (self.title(), directory))
        # The launch runs the game from here, so a relative path would resolve twice
        return os.path.abspath(directory)

    def title(self):
        """The game's name in messages."""
        return self.name.upper()

    def log_path(self):
        """The log the game writes, followed by krpc-run-ksp."""
        raise NotImplementedError

    # Installing

    def default_mods(self):
        """The mods a test can require without the suite defining them."""
        return []

    def krpc_source(self):
        """The kRPC archive to install: a path or a URL."""
        if self.krpc is None:
            raise RuntimeError("No kRPC archive to install. Pass --krpc.")
        return self.krpc

    def install(self, mods=()):
        """Install kRPC, the test add-on and exactly the given mods, by name."""
        install.install(self, mods)

    def install_krpc(self, path):
        """Install the kRPC archive at path, replacing any earlier install."""
        raise NotImplementedError

    def install_addon(self):
        """Install the game's test add-on, and the server settings the tests run with."""
        raise NotImplementedError

    def install_mod(self, mod, path):
        """Install a mod from its fetched source at path. The default for Mod.install."""
        raise NotImplementedError

    def remove_mod(self, mod):
        """Remove a mod from the install. The default for Mod.remove."""
        raise NotImplementedError

    def validate_install(self, mods):
        """Check that the install holds nothing beyond the game, kRPC and the given mods."""

    # Launching

    def launch_command(self, args=()):
        """The command that launches the game, with game-specific arguments."""
        raise NotImplementedError

    def test_arguments(self):
        """The arguments a launch for the tests passes, after preparing what they need."""
        return []

    def run_command(self, mods):
        """The command that starts a game for the tests, as messages give it."""
        return "krpc-run-ksp --game %s%s" % (self.name, self._mods_option(mods))

    @staticmethod
    def _mods_option(mods):
        return (" --mods=" + ",".join(sorted(mods))) if mods else ""

    def kill_all(self):
        """Kill every running copy of the game, by name."""

    def add_run_arguments(self, parser):
        """Add the game's own options to krpc-run-ksp."""

    def run_arguments(self, args):
        """The launch arguments for the parsed krpc-run-ksp options."""
        return []

    def log_filter(self, line) -> bool:
        """Whether krpc-run-ksp prints the log line."""
        return True

    # The running game

    def mod_present(self, mod, conn):
        """Whether the running game has the mod loaded. The default for Mod.present."""
        return None

    def connect(self, use_cached=True):
        """A connection to the running game's server."""
        return launch.connect(self, use_cached)

    def ensure(self, mods=()):
        """Ensure a game is running with exactly the given mods, launching one if needed."""
        launch.ensure(self, mods)

    def stop(self):
        """Stop the game this run launched."""
        launch.stop(self)


# Built-in games, by the name --game takes
GAMES = {
    "ksp": "krpctest.ksp:KSP",
}

# The game the tests run against, and the options that override what it sets
_game = None
_options = {}


def load(spec):
    """The game named by spec: a built-in name, or module:object for a game in Python code.
    An object that is a class is constructed with no arguments."""
    spec = GAMES.get(spec) or spec
    module, _, attribute = spec.partition(":")
    if not attribute:
        raise ValueError(
            "Unknown game %r. Give one of %s, or module:object."
            % (spec, ", ".join(sorted(GAMES)))
        )
    game = getattr(importlib.import_module(module), attribute)
    if isinstance(game, type):
        game = game()
    return game


def register(game):
    """Make game the one the tests run against. A suite's conftest.py calls this."""
    global _game  # noqa: PLW0603
    _game = game


def configure(**options):
    """Set the options that override the registered game, applied on first use."""
    _options.update(options)


def current():
    """The game the tests run against, with the options applied."""
    global _game  # noqa: PLW0603
    spec = _options.pop("game", None)
    if spec is not None:
        _game = load(spec)
    if _game is None:
        raise RuntimeError(
            "No game to test against. Pass --game, or register one from conftest.py."
        )
    apply(_game, _options)
    _options.clear()
    return _game


def apply(game, options):
    """Override what game sets with the options that have a value."""
    for name, value in options.items():
        if value is not None:
            setattr(game, name, value)
