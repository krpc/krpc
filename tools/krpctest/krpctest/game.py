"""KSP process lifecycle and server-connection management for the test framework.

Owns launching KSP with the required mods, connecting to the running kRPC server,
validating the running game's managed mod set, and stopping the game at exit.
``TestCase``'s game-management classmethods (``connect``, ``ensure_game``) delegate
here.

This module holds the process-global state — the connection cache, the owned KSP process
handle, and the record of unsatisfiable mod sets — so that state is shared across every
test class in a run.
"""

import atexit
import logging
import os
import shutil
import subprocess
import time
from importlib.resources import files

import krpc

from krpctest.env import get_ksp_dir

# Progress messages (game loading, mod set in use) go to this logger at INFO. The pytest
# plugin surfaces them live via pytest's log_cli; outside pytest, INFO is dropped by
# default. Named for the package (not __name__) so the logger identity is unchanged from
# when these functions lived in krpctest/__init__.py.
log = logging.getLogger("krpctest")

# Third-party mods the integration tests can require, declared in a test's `mods` list and
# passed to `krpc-install --mods`. The set of installable mods mirrors the registry in
# tools/mods/BUILD.bazel and krpctest/install.py; here each mod also needs a way to tell
# whether it actually loaded in the running game.
#
# Most mods are wrapped by a kRPC service, so their `.available` property reports presence.
_MOD_SERVICES = {
    "RemoteTech": "remote_tech",
    "InfernalRobotics": "infernal_robotics",
    "KerbalAlarmClock": "kerbal_alarm_clock",
}

# Some mods add parts but no dedicated service (RealChute is wrapped by the SpaceCenter
# Parachute class). Detect these by probing the part catalog for a part they contribute,
# via the test-only TestingTools helper. The probe name is the in-game part name, in which
# KSP replaces underscores with periods (config `RC_stack` becomes `RC.stack`).
_MOD_PARTS = {
    "RealChute": "RC.stack",
    "DMagic": "dmagicSensorTest",
}

# Some mods add no part and no service, but patch a part module onto existing parts (Action
# Groups Extended adds a ModuleAGX module to every part via ModuleManager; it is wrapped by the
# SpaceCenter Control class). Detect these by probing the loaded part prefabs for that module,
# via the test-only TestingTools helper.
_MOD_MODULES = {
    "AGExt": "ModuleAGX",
}

# The KSP process this test run launched, or None if KSP was started externally (a
# manually-started game is never killed or reinstalled).
_owned_ksp = None  # pylint: disable=invalid-name

# Mod sets (as frozensets) that were installed and launched but did not become available
# in-game — recorded so ensure_game fails fast instead of relaunching KSP forever.
_unsatisfiable = set()


def _address():
    return os.environ.get("KRPC_ADDRESS", "127.0.0.1")


def connect(use_cached=True):
    if connect.connection is not None and use_cached:
        return connect.connection
    connection = krpc.connect(name="krpctest", address=_address())
    if use_cached:
        connect.connection = connection
    return connection


connect.connection = None


def _auto_launch_enabled():
    return os.environ.get("KRPC_AUTO_LAUNCH", "1") != "0"


def _gamedata_check_enabled():
    return os.environ.get("KRPC_SKIP_GAMEDATA_CHECK", "0") != "1"


def copy_blank_save(name, ksp_dir=None):
    """Copy the bundled blank save into the KSP install's saves/<name>/persistent.sfs."""
    blank_save = str(files("krpctest").joinpath(name + ".sfs"))
    save_path = os.path.join(get_ksp_dir(ksp_dir), "saves", name)
    if not os.path.exists(save_path):
        os.makedirs(save_path)
    shutil.copy(blank_save, os.path.join(save_path, "persistent.sfs"))


def _try_connect():
    """Return a connection to a running server, or None if none is reachable. Reuses the
    cached connection when present (cleared on restart), so repeated calls across test
    classes share one connection as before."""
    if connect.connection is not None:
        return connect.connection
    try:
        connection = krpc.connect(name="krpctest", address=_address())
    except Exception:  # pylint: disable=broad-except
        return None
    connect.connection = connection
    return connection


def _installed_mods(conn):
    """The set of managed mods currently loaded in the running game."""
    installed = {
        name
        for name, service in _MOD_SERVICES.items()
        if getattr(conn, service).available
    }
    installed |= {
        name
        for name, part in _MOD_PARTS.items()
        if conn.testing_tools.part_available(part)
    }
    installed |= {
        name
        for name, module in _MOD_MODULES.items()
        if conn.testing_tools.part_module_available(module)
    }
    return installed


def _validate_mods(required):
    known = set(_MOD_SERVICES) | set(_MOD_PARTS) | set(_MOD_MODULES)
    unknown = set(required) - known
    if unknown:
        raise ValueError(
            "Unknown mod(s) in `mods`: %s (known: %s)"
            % (", ".join(sorted(unknown)), ", ".join(sorted(known)))
        )


def _install_mods(required):
    """Build and install the mod, reconciling GameData to exactly `required` (plus kRPC).
    Imported lazily to avoid a circular import (krpctest.install imports this module).
    """
    from krpctest.install import install  # pylint: disable=import-outside-toplevel

    log.info(
        "building and installing kRPC (mods: %s)",
        ", ".join(sorted(required)) or "none",
    )
    install(mods=sorted(required), validate_gamedata=_gamedata_check_enabled())


def _launch_ksp(required):
    """Install the required mods, launch KSP, and wait for the server to come up. Marks the
    launched game as owned by this run so it may be restarted or stopped."""
    global _owned_ksp  # pylint: disable=global-statement,invalid-name
    _install_mods(required)
    # Ensure the auto-loaded save exists, otherwise KSP stays at the main menu and the
    # server never starts.
    copy_blank_save("krpctest")
    ksp_dir = get_ksp_dir()
    log.info("launching KSP")
    _owned_ksp = subprocess.Popen(  # pylint: disable=consider-using-with
        [os.path.join(ksp_dir, "KSP.x86_64"), "--krpctest-load-game=krpctest"],
        cwd=ksp_dir,
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
    )
    return _wait_for_server()


def _stop_ksp():
    """Stop the KSP process this run launched (no-op if KSP was started externally)."""
    global _owned_ksp  # pylint: disable=global-statement,invalid-name
    if _owned_ksp is None:
        return
    proc = _owned_ksp
    _owned_ksp = None
    connect.connection = None
    log.info("stopping KSP")
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
        # The handle didn't map to the live process; fall back to a name match.
        subprocess.call(["pkill", "-f", "KSP[.]x86_64"])


atexit.register(_stop_ksp)


def _wait_for_server(timeout=300, interval=3):
    """Poll until the server answers (refreshing the cached connection), or time out."""
    log.info("waiting for kRPC server...")
    deadline = time.time() + timeout
    while True:
        connection = _try_connect()
        if connection is not None:
            time.sleep(3)  # wait for game to stabilize
            log.info("kRPC server ready")
            return connection
        if _owned_ksp is not None and _owned_ksp.poll() is not None:
            raise RuntimeError("KSP exited before the kRPC server became available")
        if time.time() > deadline:
            raise RuntimeError(
                "Timed out after %gs waiting for the kRPC server" % timeout
            )
        time.sleep(interval)


def _unsatisfiable_message(required, installed=None):
    detail = ""
    if installed is not None:
        detail = " (the game reports these mods available: %s)" % (
            ", ".join(sorted(installed)) or "none"
        )
    return (
        "Installed and launched KSP with mods %s, but the game does not report them "
        "available%s. The mod may have failed to load — e.g. an incompatible version or a "
        "missing dependency. Not retrying." % (sorted(required), detail)
    )


def _run_ksp_command(mods_arg):
    """The command that starts a game to test against, written the way the reader runs it:
    the bazel target from a checkout, the console script from an installed package."""
    if os.environ.get("BUILD_WORKSPACE_DIRECTORY"):
        prefix = "bazel run //:run-ksp -- "
    else:
        prefix = "krpc-run-ksp "
    return (prefix + "--load-game=krpctest " + mods_arg).strip()


def _run_tests_command():
    """The command that runs the suite, written the way the reader runs it."""
    if os.environ.get("BUILD_WORKSPACE_DIRECTORY"):
        return "bazel run //:test-ingame"
    return "pytest"


def _mismatch_message(required, installed):
    parts = []
    missing = sorted(required - installed)
    extra = sorted(installed - required)
    if missing:
        parts.append("missing required mod(s): " + ", ".join(missing))
    if extra:
        parts.append("unexpected mod(s) installed: " + ", ".join(extra))
    suffix = ("--mods=" + ",".join(sorted(required))) if required else "--mods="
    return (
        "The running KSP has the wrong mods (%s). It was not started by the tests, so it "
        "will not be modified. Restart it with the correct mods, e.g. `%s`, or run the "
        "suite with `%s` and let it manage the game."
        % ("; ".join(parts), _run_ksp_command(suffix), _run_tests_command())
    )


def ensure_game(mods=None):
    """Ensure a KSP server is running with exactly the required managed mods.

    - Server up with the correct mods: return (the fast manual path, no restart).
    - Server up with the wrong mods: if this run started KSP, restart it with the
      correct mods; otherwise raise (never touch a manually-started game).
    - No server: launch KSP with the required mods (unless KRPC_AUTO_LAUNCH=0).
    """
    required = set(mods or [])
    _validate_mods(required)
    # Fail fast if we already tried and failed to make this mod set available, rather
    # than relaunching KSP on every test class forever.
    if frozenset(required) in _unsatisfiable:
        raise RuntimeError(_unsatisfiable_message(required))
    conn = _try_connect()
    if conn is not None:
        installed = _installed_mods(conn)
        if installed == required:
            return
        if _owned_ksp is None:
            raise RuntimeError(_mismatch_message(required, installed))
        # We own this game: stop it and relaunch with the correct mods.
        log.info(
            "required mods changed (%s -> %s), restarting KSP",
            ", ".join(sorted(installed)) or "none",
            ", ".join(sorted(required)) or "none",
        )
        _stop_ksp()
    elif not _auto_launch_enabled():
        raise RuntimeError(
            "No kRPC server is reachable and auto-launch is disabled (--no-launch or "
            "KRPC_AUTO_LAUNCH=0). Start KSP first, e.g. `%s`, and wait for it to load."
            % _run_ksp_command(
                ("--mods=" + ",".join(sorted(required))) if required else ""
            )
        )
    # Launch (from cold, or after stopping a wrong-state game we own) and confirm the
    # required mods actually became available before running any tests against it.
    conn = _launch_ksp(required)
    installed = _installed_mods(conn)
    if installed != required:
        _unsatisfiable.add(frozenset(required))
        _stop_ksp()
        raise RuntimeError(_unsatisfiable_message(required, installed))
