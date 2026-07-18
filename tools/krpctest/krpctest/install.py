"""Build the kRPC mod and install it into a KSP GameData directory.

Builds the ``//:krpc`` release archive with Bazel and extracts its ``GameData`` tree
into the KSP install (from ``KSP_DIR`` or ``--ksp-dir``) - exactly as a user
installs a release - then adds the test-only bits the public release omits (the
``TestingTools`` add-on and the test ``settings.cfg``). The optional set of managed
third-party mods (RemoteTech, InfernalRobotics, KerbalAlarmClock) used by some service
tests is reconciled so GameData contains exactly the requested set. Finally GameData is
validated against the known-valid set, so an unexpected mod left over from an earlier
session is reported up front instead of silently stopping the server from starting; a dev
deliberately running an unmanaged mod can opt out with ``--skip-gamedata-check``.

Run it from the repository root, either as the ``krpc-install`` console script or with
``python -m krpctest.install``. It is also called directly by the test framework (see
``krpctest.game._install_mods``).
"""

import argparse
import glob
import os
import shutil
import subprocess
import sys
import zipfile

from krpctest.env import get_ksp_dir, get_repo_root
from krpctest.version import __version__

# Managed third-party mods: name accepted by --mods -> list of components it installs, each a
# (Bazel target under //tools/mods, GameData subdir it installs as) pair. Most mods are a
# single component; RealChute and AGExt also pull in the shared ClickThroughBlocker /
# ToolbarControl / Harmony dependencies, whose assemblies their plugins hard-depend on.
MODS = {
    "RemoteTech": [("remotetech", "RemoteTech")],
    "InfernalRobotics": [("infernal_robotics", "MagicSmokeIndustries")],
    "KerbalAlarmClock": [("kerbal_alarm_clock", "TriggerTech")],
    "RealChute": [
        ("realchute", "RealChute"),
        ("harmony", "000_Harmony"),
        ("clickthroughblocker", "000_ClickThroughBlocker"),
        ("toolbarcontrol", "001_ToolbarControl"),
    ],
    "DMagic": [("dmagic_science_animate", "DMagicScienceAnimate")],
    "AGExt": [
        ("agext", "Diazo"),
        ("harmony", "000_Harmony"),
        ("clickthroughblocker", "000_ClickThroughBlocker"),
        ("toolbarcontrol", "001_ToolbarControl"),
        ("spacetuxlibrary", "SpaceTuxLibrary"),
    ],
}

# All managed GameData subdirs, every one a candidate for removal during reconcile.
_ALL_MOD_SUBDIRS = [subdir for comps in MODS.values() for _, subdir in comps]

# GameData entries a valid test install always permits, on top of the managed-mod subdirs
# and whatever mods a test requests. Stock KSP ships Squad and SquadExpansion; install()
# lays down kRPC and the ModuleManager assembly; ModuleManager writes these cache files
# into GameData at runtime. The versioned ModuleManager.<ver>.dll is matched separately
# (see _is_module_manager_dll).
_BASELINE_GAMEDATA = frozenset(
    {
        "Squad",
        "SquadExpansion",
        "kRPC",
        "ModuleManager.ConfigCache",
        "ModuleManager.ConfigSHA",
        "ModuleManager.Physics",
        "ModuleManager.TechTree",
    }
)


def _is_module_manager_dll(entry):
    """Whether entry is the ModuleManager assembly, whose filename embeds its version."""
    return entry.startswith("ModuleManager") and entry.endswith(".dll")


def _requested_mod_subdirs(mods):
    """GameData subdir names the given managed mods install (including dependencies)."""
    return {subdir for mod in mods for _, subdir in MODS[mod]}


def _mod_archive_src(target, subdir):
    return os.path.join(
        "bazel-krpc",
        "external",
        "+http_archive+" + target,
        "GameData",
        subdir,
    )


def _mod_config_overlay(root, subdir):
    """Directory of config files layered onto a freshly-installed mod's GameData subdir.

    Mod archives ship no settings, so some mods pop a first-run window that blocks an
    unattended launch (RealChute's ClickThroughBlocker and ToolbarControl dependencies both
    do). tools/mods/config/<subdir>/ holds the minimal config that marks those windows
    already-dismissed; it is copied over the subdir after install. Absent for mods that need
    no overlay."""
    return os.path.join(root, "tools", "mods", "config", subdir)


def _release_zip(root):
    """Path to the release archive built by ``//:krpc``. Its filename embeds the version,
    so ask Bazel for the output rather than reconstructing the name."""
    output = subprocess.check_output(
        ["bazel", "cquery", "--output=files", "//:krpc"], cwd=root, text=True
    )
    files = [line for line in output.splitlines() if line.strip()]
    return os.path.join(root, files[-1])


def _testingtools_files(root):
    """Paths to the TestingTools add-on outputs. rules_dotnet writes these to a
    configuration-specific output directory rather than under the ``bazel-bin``
    convenience symlink, so resolve them with cquery rather than hardcoding paths."""
    output = subprocess.check_output(
        ["bazel", "cquery", "--output=files", "//tools/TestingTools"],
        cwd=root,
        text=True,
    )
    return [os.path.join(root, line) for line in output.splitlines() if line.strip()]


def _normalize_permissions(path):
    """Set files to 0o644 and directories to 0o755 under path (inclusive)"""
    os.chmod(path, 0o755)
    for root, dirs, filenames in os.walk(path):
        for name in dirs:
            os.chmod(os.path.join(root, name), 0o755)
        for name in filenames:
            os.chmod(os.path.join(root, name), 0o644)


def install(mods=(), ksp_dir=None, validate_gamedata=True):
    """Build the mod and install it (plus exactly the requested managed mods) into the KSP
    GameData directory. mods is an iterable of names from MODS; ksp_dir defaults to the
    KSP install given by KSP_DIR. Set validate_gamedata=False to skip the check that
    GameData holds only the known-valid set, for a dev deliberately running an unmanaged
    mod alongside the tests."""
    mods = list(mods)
    unknown = [m for m in mods if m not in MODS]
    if unknown:
        raise ValueError(
            "Unknown mod(s): %s (known: %s)"
            % (", ".join(unknown), ", ".join(sorted(MODS)))
        )

    root = get_repo_root()
    ksp_dir = get_ksp_dir(ksp_dir)
    gamedata_root = os.path.join(ksp_dir, "GameData")
    gamedata = os.path.join(gamedata_root, "kRPC")

    # Build the release archive and the test-only TestingTools add-on (the latter is not
    # part of the public release, so it is not in the zip).
    subprocess.check_call(
        ["bazel", "build", "//:krpc", "//tools/TestingTools"], cwd=root
    )

    # Wipe any previous kRPC install and extract the freshly built release zip's GameData
    # tree (GameData/kRPC and GameData/ModuleManager*.dll) into the KSP install, exactly as
    # a user would unzip a release. Other mods' GameData subdirs are left untouched.
    if os.path.exists(gamedata):
        shutil.rmtree(gamedata)
    for stale in glob.glob(os.path.join(gamedata_root, "ModuleManager*.dll")):
        os.remove(stale)
    with zipfile.ZipFile(_release_zip(root)) as archive:
        members = [n for n in archive.namelist() if n.startswith("GameData/")]
        archive.extractall(ksp_dir, members)

    # Add the test-only pieces the public release omits: the TestingTools add-on, and a
    # settings.cfg that starts the server automatically (the release ships a blank one).
    for src in _testingtools_files(root):
        shutil.copy(src, gamedata)
    shutil.copy(
        os.path.join(root, "tools", "settings.cfg"),
        os.path.join(gamedata, "PluginData", "settings.cfg"),
    )

    _normalize_permissions(gamedata)
    for module_manager in glob.glob(os.path.join(gamedata_root, "ModuleManager*.dll")):
        os.chmod(module_manager, 0o644)

    _reconcile_mods(mods, root, gamedata_root)
    if validate_gamedata:
        _validate_gamedata(gamedata_root, _requested_mod_subdirs(mods))


def _validate_gamedata(gamedata_root, requested_subdirs):
    """Fail if GameData contains anything outside the known-valid test install.

    A valid install is the stock game plus kRPC, ModuleManager, and exactly the managed
    mods a test requested. Anything else — a mod left behind by an earlier session, a stray
    manual install — is reported rather than ignored: an unexpected assembly that fails to
    load (for example one that needs a Harmony build which is not installed) stops the kRPC
    server from starting, and KSP then sits at the space center while the test waits forever
    for a server that never comes up, with nothing pointing at the cause."""
    allowed = _BASELINE_GAMEDATA | set(requested_subdirs)
    unexpected = [
        entry
        for entry in sorted(os.listdir(gamedata_root))
        if entry not in allowed and not _is_module_manager_dll(entry)
    ]
    if unexpected:
        raise RuntimeError(
            "Unexpected entries in %s: %s. The integration tests require a known-clean "
            "GameData: the stock game plus kRPC, ModuleManager and only the mods a test "
            "requests. Remove the listed entries and re-run."
            % (gamedata_root, ", ".join(unexpected))
        )


def _reconcile_mods(mods, root, gamedata_root):
    """Make the managed mods in gamedata_root exactly the requested set."""
    requested = [comp for mod in mods for comp in MODS[mod]]
    requested_subdirs = _requested_mod_subdirs(mods)

    # Remove any managed mod that is not requested.
    for subdir in _ALL_MOD_SUBDIRS:
        if subdir not in requested_subdirs:
            shutil.rmtree(os.path.join(gamedata_root, subdir), ignore_errors=True)

    # Install (or refresh) each requested mod (and its dependencies) from its Bazel-fetched
    # archive, then lay any bundled config overlay on top (see _mod_config_overlay).
    for target, subdir in requested:
        subprocess.check_call(["bazel", "build", "//tools/mods:" + target], cwd=root)
        dst = os.path.join(gamedata_root, subdir)
        if os.path.exists(dst):
            shutil.rmtree(dst)
        shutil.copytree(os.path.join(root, _mod_archive_src(target, subdir)), dst)
        overlay = _mod_config_overlay(root, subdir)
        if os.path.isdir(overlay):
            shutil.copytree(overlay, dst, dirs_exist_ok=True)
        _normalize_permissions(dst)


def main():
    parser = argparse.ArgumentParser(
        prog="krpc-install",
        description=(
            "Build the kRPC mod and install it into KSP's GameData directory "
            "(from KSP_DIR or --ksp-dir). Run from the repository root."
        ),
    )
    parser.add_argument(
        "-v",
        "--version",
        action="version",
        version="%s version %s" % ("krpc-install", __version__),
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
        help="skip the check that GameData holds only the known-valid set, to allow "
        "an unmanaged mod to remain installed",
    )
    args = parser.parse_args()
    mods = [m for m in args.mods.split(",") if m]
    try:
        install(
            mods=mods,
            ksp_dir=args.ksp_dir,
            validate_gamedata=not args.skip_gamedata_check,
        )
    except (ValueError, RuntimeError) as ex:
        sys.stderr.write("Error: %s\n" % ex)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
