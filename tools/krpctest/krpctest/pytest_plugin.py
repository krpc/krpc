"""pytest plugin for the kRPC integration tests.

Registered via the ``pytest11`` entry point (see pyproject.toml), so it loads automatically
whenever pytest runs with krpctest installed. It adds a few small pieces of behaviour:

 * a ``--ksp-dir`` option that points the framework at a KSP install,
 * a ``--no-launch`` option for running against a game started separately,
 * a ``--skip-gamedata-check`` option for launching with an unmanaged mod installed, and
 * ordering the collected tests by their required mods so KSP is restarted at most once per
   distinct mod set (stock tests first).

The test classes themselves are plain ``krpctest.TestCase`` (``unittest.TestCase``)
subclasses, which pytest collects and runs natively; the framework launches/stops KSP from
``TestCase.setUpClass`` (via ``ensure_game``), so no session fixture is needed here.
"""

import os


def pytest_addoption(parser):
    parser.addoption(
        "--ksp-dir",
        action="store",
        default=None,
        metavar="DIR",
        help="path to the KSP install (defaults to $KSP_DIR)",
    )
    parser.addoption(
        "--no-launch",
        action="store_true",
        default=False,
        help="require an already-running game rather than launching one",
    )
    parser.addoption(
        "--skip-gamedata-check",
        action="store_true",
        default=False,
        help="skip the check that GameData holds only the known-valid set, to launch "
        "with an unmanaged mod installed",
    )


def pytest_configure(config):
    # The framework resolves the KSP install from KSP_DIR; --ksp-dir just supplies it.
    ksp_dir = config.getoption("--ksp-dir")
    if ksp_dir is not None:
        os.environ["KSP_DIR"] = ksp_dir
    # Likewise, auto-launch is off when KRPC_AUTO_LAUNCH is 0. Use this when running against
    # a game started separately: a game that is still loading has no server to connect to
    # yet, and without it the framework would launch a second game and reinstall the mod
    # underneath the first.
    if config.getoption("--no-launch"):
        os.environ["KRPC_AUTO_LAUNCH"] = "0"
    # Likewise, the GameData validity check is skipped when KRPC_SKIP_GAMEDATA_CHECK is 1.
    # Use this to auto-launch with an unmanaged mod deliberately installed alongside kRPC.
    if config.getoption("--skip-gamedata-check"):
        os.environ["KRPC_SKIP_GAMEDATA_CHECK"] = "1"


def _mods_key(item):
    """Sort key grouping items by their test class's required mods: stock (no-mod) first,
    then each distinct mod set. Returns a value that sorts empty-mods ahead of the rest.
    """
    cls = getattr(item, "cls", None)
    mods = sorted(getattr(cls, "mods", []) or [])
    return (bool(mods), mods)


def pytest_collection_modifyitems(items):
    # Stable sort keeps definition order within each mod group, so tests are reordered only
    # as much as needed to keep each mod set contiguous (minimising KSP restarts).
    items.sort(key=_mods_key)


def _mods_label(item):
    cls = getattr(item, "cls", None)
    mods = sorted(getattr(cls, "mods", []) or [])
    return ", ".join(mods) if mods else "(no mods)"


def pytest_report_collectionfinish(items):
    # After "collected N items", summarise the mod groups and their counts in run order, so
    # it is clear up front which mod sets will be exercised (and how many KSP loads that is).
    counts = {}
    for item in items:
        label = _mods_label(item)
        counts[label] = counts.get(label, 0) + 1
    if not counts:
        return None
    summary = ", ".join("%s: %d" % (label, count) for label, count in counts.items())
    return "krpctest: test plan — " + summary
