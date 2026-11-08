"""pytest plugin for tests against a live game.

Opt-in: load it with ``pytest -p krpctest.pytest_plugin``. It adds the options that choose
the game and its install, and orders the collected tests by their required mods, so the game
restarts at most once per mod set.

The game comes from ``--game``, or from a suite's ``conftest.py``, which builds the game
object and passes it to :func:`krpctest.game.register`. The test classes are
``krpctest.testcase.TestCase`` subclasses, which launch the game from ``setUpClass``.
"""

import os

from . import game


def pytest_addoption(parser):
    group = parser.getgroup("krpctest", "tests against a live game")
    group.addoption(
        "--game",
        metavar="GAME",
        help="the game to test against: %s, or module:object for a game defined in "
        "Python code" % ", ".join(sorted(game.GAMES)),
    )
    group.addoption(
        "--krpc",
        metavar="SOURCE",
        help="the kRPC archive to install, as a path or a URL",
    )
    group.addoption(
        "--ksp-dir",
        metavar="DIR",
        help="path to the KSP install (defaults to $KSP_DIR)",
    )
    group.addoption(
        "--no-launch",
        action="store_true",
        default=False,
        help="require an already-running game rather than launching one",
    )
    group.addoption(
        "--skip-gamedata-check",
        action="store_true",
        default=False,
        help="skip the check that GameData holds only the known-valid set, to launch "
        "with an unmanaged mod installed",
    )


def pytest_configure(config):
    # The game reads these variables when it is built, which is on first use
    ksp_dir = config.getoption("--ksp-dir")
    if ksp_dir is not None:
        os.environ["KSP_DIR"] = ksp_dir
    if config.getoption("--no-launch"):
        os.environ["KRPC_TEST_AUTO_LAUNCH"] = "0"
    if config.getoption("--skip-gamedata-check"):
        os.environ["KRPC_TEST_SKIP_GAMEDATA_CHECK"] = "1"
    game.configure(game=config.getoption("--game"), krpc=config.getoption("--krpc"))


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
