"""Build the kRPC mod and install it into a KSP GameData directory.

Builds the ``//:krpc`` release archive with Bazel and extracts its ``GameData`` tree
into the KSP install (from ``KSP_DIR`` or ``--ksp-dir``) - exactly as a user
installs a release - then adds the test-only bits the public release omits (the
``TestingTools`` add-on, the ``KRPC.Benchmark`` service and the test ``settings.cfg``).
The optional set of managed
third-party mods used by some service tests is downloaded from the archives pinned in
``krpctest.mods`` and reconciled so GameData contains exactly the requested set. Finally GameData is
validated against the known-valid set, so an unexpected mod left over from an earlier
session is reported up front instead of silently stopping the server from starting; a dev
deliberately running an unmanaged mod can opt out with ``--skip-gamedata-check``.

Run it from the repository root, either as the ``krpc-install`` console script or with
``python -m krpctest.install``. It is also called directly by the test framework (see
``krpctest.game._install_mods``).
"""

import argparse
import glob
import hashlib
import http.client
import os
import shutil
import subprocess
import sys
import tempfile
import time
import urllib.error
import urllib.parse
import urllib.request
import zipfile
from importlib.resources import files

from krpctest.env import get_ksp_dir, get_repo_root
from krpctest.mods import ARCHIVES, MODS
from krpctest.version import __version__

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


def cache_dir():
    """The directory downloads are cached in."""
    if sys.platform == "win32":
        base = os.environ.get("LOCALAPPDATA") or os.path.expanduser("~")
    elif sys.platform == "darwin":
        base = os.path.expanduser("~/Library/Caches")
    else:
        base = os.environ.get("XDG_CACHE_HOME") or os.path.expanduser("~/.cache")
    return os.path.join(base, "krpctools")


def _sha256(path):
    digest = hashlib.sha256()
    with open(path, "rb") as f:
        for block in iter(lambda: f.read(1 << 20), b""):
            digest.update(block)
    return digest.hexdigest()


# The errors a read can fail with partway through, besides those urllib raises
_DROPPED = (TimeoutError, ConnectionError, http.client.IncompleteRead)


def with_retries(action, description, attempts=4):
    """The result of action(), retried after a server error, a failed connection or a
    dropped read. description names what action does, for the error raised after the last
    attempt."""
    for attempt in range(attempts):
        try:
            return action()
        except (urllib.error.URLError, *_DROPPED) as ex:
            transient = not isinstance(ex, urllib.error.HTTPError) or ex.code >= 500
            if not transient or attempt == attempts - 1:
                raise RuntimeError("Failed to %s: %s" % (description, ex)) from ex
            time.sleep(2**attempt)
    raise AssertionError("unreachable")


def _copy_url(request, f):
    """Copy the response to request into f."""

    def copy():
        f.seek(0)
        f.truncate()
        with urllib.request.urlopen(request, timeout=60) as response:
            shutil.copyfileobj(response, f)

    with_retries(copy, "download " + request.full_url)


def download(url, sha256):
    """The path of the file at url, downloaded into the cache unless it is there already.
    The cache is keyed by the file's SHA-256, so each archive is downloaded once."""
    name = os.path.basename(urllib.parse.urlparse(url).path) or "download"
    directory = os.path.join(cache_dir(), sha256)
    path = os.path.join(directory, urllib.parse.unquote(name))
    if os.path.exists(path):
        return path
    os.makedirs(directory, exist_ok=True)
    request = urllib.request.Request(
        url, headers={"User-Agent": "krpctest/" + __version__}
    )
    with tempfile.NamedTemporaryFile(dir=directory, delete=False) as f:
        partial = f.name
        try:
            _copy_url(request, f)
        except BaseException:
            f.close()
            os.remove(partial)
            raise
    if _sha256(partial) != sha256:
        os.remove(partial)
        raise RuntimeError(
            "Downloaded %s does not match its SHA-256 %s" % (url, sha256)
        )
    os.replace(partial, path)
    return path


def extract_folder(archive, folder, dst):
    """Extract GameData/<folder>, at any depth in the zip archive, to dst."""
    with zipfile.ZipFile(archive) as f:
        found = False
        for member in f.infolist():
            parts = member.filename.split("/")
            if ".." in parts or member.is_dir():
                continue
            for i in range(len(parts) - 2):
                if parts[i] == "GameData" and parts[i + 1] == folder:
                    target = os.path.join(dst, *parts[i + 2 :])
                    os.makedirs(os.path.dirname(target), exist_ok=True)
                    with f.open(member) as src, open(target, "wb") as out:
                        shutil.copyfileobj(src, out)
                    found = True
                    break
    if not found:
        raise RuntimeError("%s has no GameData/%s" % (archive, folder))


def _mod_config_overlay(subdir):
    """Directory of config files layered onto a freshly-installed mod's GameData subdir.

    Mod archives ship no settings, so some mods open a first-run window that blocks an
    unattended launch (RealChute's ClickThroughBlocker and ToolbarControl dependencies both
    do). krpctest/mods/<subdir>/ holds the minimal config that marks those windows already
    dismissed, and is copied over the subdir after install. Absent for mods that need no
    overlay."""
    return str(files("krpctest").joinpath("mods").joinpath(subdir))


def _release_zip(root):
    """Path to the release archive built by ``//:krpc``. Its filename embeds the version,
    so ask Bazel for the output rather than reconstructing the name."""
    output = subprocess.check_output(
        ["bazel", "cquery", "--output=files", "//:krpc"], cwd=root, text=True
    )
    files = [line for line in output.splitlines() if line.strip()]
    return os.path.join(root, files[-1])


# The test-only assemblies the public release omits, installed into GameData on top of it.
# TestingTools is the add-on the tests drive the game with; KRPC.Benchmark is the benchmark
# service, which TestServer also loads, so an in-game measurement and a game-less one are the
# same measurement.
_TEST_ONLY_TARGETS = [
    "//tools/TestingTools",
    "//tools/benchmarks:KRPC.Benchmark.net472",
]


def _target_files(root, target):
    """Paths to a Bazel target's outputs. rules_dotnet writes these to a
    configuration-specific output directory rather than under the ``bazel-bin``
    convenience symlink, so resolve them with cquery rather than hardcoding paths."""
    output = subprocess.check_output(
        ["bazel", "cquery", "--output=files", target],
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


def install(mods=(), ksp_dir=None, validate_gamedata=True, skip_mod_install=False):
    """Build the mod and install it (plus exactly the requested managed mods) into the KSP
    GameData directory. mods is an iterable of names from MODS; ksp_dir defaults to the
    KSP install given by KSP_DIR. Set validate_gamedata=False to skip the check that
    GameData holds only the known-valid set, for a dev deliberately running an unmanaged
    mod alongside the tests. Set skip_mod_install=True to leave the managed third-party
    mods in GameData exactly as they are — neither installing the requested ones nor
    removing the others — so a locally built or hand-placed mod assembly is preserved
    across a launch; the requested mods are still used to validate GameData, so name
    whatever is installed."""
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

    # Build the release archive and the test-only assemblies (which are not part of the
    # public release, so they are not in the zip).
    subprocess.check_call(["bazel", "build", "//:krpc"] + _TEST_ONLY_TARGETS, cwd=root)

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

    # Add the test-only pieces the public release omits: the assemblies above, and a
    # settings.cfg that starts the server automatically (the release ships a blank one).
    for target in _TEST_ONLY_TARGETS:
        for src in _target_files(root, target):
            shutil.copy(src, gamedata)
    shutil.copy(
        os.path.join(root, "tools", "settings.cfg"),
        os.path.join(gamedata, "PluginData", "settings.cfg"),
    )

    _normalize_permissions(gamedata)
    for module_manager in glob.glob(os.path.join(gamedata_root, "ModuleManager*.dll")):
        os.chmod(module_manager, 0o644)

    if not skip_mod_install:
        _reconcile_mods(mods, gamedata_root)
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


def _reconcile_mods(mods, gamedata_root):
    """Make the managed mods in gamedata_root exactly the requested set."""
    requested = [comp for mod in mods for comp in MODS[mod]]
    requested_subdirs = _requested_mod_subdirs(mods)

    # Remove any managed mod that is not requested.
    for subdir in _ALL_MOD_SUBDIRS:
        if subdir not in requested_subdirs:
            shutil.rmtree(os.path.join(gamedata_root, subdir), ignore_errors=True)

    # Install (or refresh) each requested mod (and its dependencies) from its downloaded
    # archive, then lay any bundled config overlay on top (see _mod_config_overlay).
    for archive, subdir in requested:
        dst = os.path.join(gamedata_root, subdir)
        if os.path.exists(dst):
            shutil.rmtree(dst)
        extract_folder(download(*ARCHIVES[archive]), subdir, dst)
        overlay = _mod_config_overlay(subdir)
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
    parser.add_argument(
        "--skip-mod-install",
        action="store_true",
        default=False,
        help="leave the managed third-party mods in GameData untouched — neither "
        "installing the requested ones nor removing others — to preserve a locally "
        "built mod assembly",
    )
    args = parser.parse_args()
    mods = [m for m in args.mods.split(",") if m]
    try:
        install(
            mods=mods,
            ksp_dir=args.ksp_dir,
            validate_gamedata=not args.skip_gamedata_check,
            skip_mod_install=args.skip_mod_install,
        )
    except (ValueError, RuntimeError) as ex:
        sys.stderr.write("Error: %s\n" % ex)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
