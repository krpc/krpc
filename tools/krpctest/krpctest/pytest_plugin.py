"""pytest plugin for tests against a live game.

Opt-in: load it with ``pytest -p krpctest.pytest_plugin``. It adds the options that choose
the game and override what it sets, and orders the collected tests by their required mods,
so the game restarts at most once per mod set.

The game comes from ``--game``, or from a suite's ``conftest.py``, which builds the game
object and passes it to :func:`krpctest.game.register`. The test classes are
``krpctest.testcase.TestCase`` subclasses, which launch the game from ``setUpClass``.
"""

import pytest

from . import game


def _mod_source(value):
    name, separator, source = value.partition("=")
    if not separator or not name or not source:
        raise ValueError("--mod takes NAME=SOURCE, got %r" % value)
    return name, source


def add_options(group, launching=True):
    """Add the options that choose a game and override what it sets. launching adds those
    for a run that launches the game itself."""
    group.addoption(
        "--game",
        metavar="GAME",
        help="the game to test against: %s, or module:object for a game defined in "
        "Python code" % ", ".join(sorted(game.GAMES)),
    )
    group.addoption(
        "--game-dir",
        metavar="DIR",
        help="the game's install directory, in place of its environment variable",
    )
    group.addoption(
        "--krpc",
        metavar="SOURCE",
        help="the kRPC archive to install, as a path or a URL",
    )
    group.addoption(
        "--mod",
        action="append",
        default=[],
        metavar="NAME=SOURCE",
        help="install the named mod from a path or a URL, in place of its pinned archive",
    )
    group.addoption(
        "--skip-install-check",
        action="store_true",
        default=False,
        help="skip the check that the install holds only the game, kRPC and the requested "
        "mods, to launch with an unmanaged mod installed",
    )
    group.addoption(
        "--skip-mod-install",
        action="store_true",
        default=False,
        help="leave the installed mods as they are, to launch with a locally built mod",
    )
    if not launching:
        return
    group.addoption(
        "--no-launch",
        action="store_true",
        default=False,
        help="require an already-running game, in place of launching one",
    )
    group.addoption(
        "--address",
        metavar="ADDRESS",
        help="the address of the game's server (default: 127.0.0.1)",
    )


def options(get):
    """The game options from the command line, given get(name) for each option."""
    return {
        "game": get("game"),
        "directory": get("game_dir"),
        "krpc": get("krpc"),
        "mod_sources": dict(_mod_source(value) for value in get("mod") or []),
        "auto_launch": False if get("no_launch") else None,
        "check_install": False if get("skip_install_check") else None,
        "install_mods": False if get("skip_mod_install") else None,
        "address": get("address"),
    }


def pytest_addoption(parser):
    add_options(parser.getgroup("krpctest", "tests against a live game"))


def pytest_configure(config):
    try:
        game.configure(**options(config.getoption))
    except ValueError as ex:
        raise pytest.UsageError(str(ex)) from ex


def pytest_collection_finish(session):
    # Every conftest.py has registered its game by now. Resolving the game here reports a bad
    # option once, in place of an error in each test class.
    if not any(_needs_game(item) for item in session.items):
        return
    try:
        game.current()
    except (ValueError, RuntimeError, ImportError, AttributeError) as ex:
        raise pytest.UsageError(str(ex)) from ex


def _needs_game(item):
    return getattr(getattr(item, "cls", None), "game_required", False)


def _mods(item):
    cls = getattr(item, "cls", None)
    return sorted(getattr(cls, "mods", []) or [])


def pytest_collection_modifyitems(items):
    # Stock tests first, then each distinct mod set. The sort is stable, so tests move only
    # as much as needed to keep each mod set together.
    items.sort(key=lambda item: (bool(_mods(item)), _mods(item)))


def pytest_report_collectionfinish(items):
    # After "collected N items", each mod set in run order with its test count. Each set is
    # one game load.
    counts = {}
    for item in items:
        label = ", ".join(_mods(item)) or "(no mods)"
        counts[label] = counts.get(label, 0) + 1
    if not counts:
        return None
    summary = ", ".join("%s: %d" % (label, count) for label, count in counts.items())
    return "krpctest: test plan - " + summary
