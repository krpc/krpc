"""Test harness for the KSA integration tests.

Connects to the running game, or launches one through StarMap when no server is
running. A launched game is saved as krpctest straight after loading, and quit when
the test session ends. Each test class loads that save, so starts from the game's
default start state.
"""

import os
import signal
import subprocess
import time
import unittest
from pathlib import Path
from typing import Any

import krpc

ROOT = Path(__file__).resolve().parent.parent
LOG_PATH = ROOT / "bin" / "ksa.log"
SAVE = "krpctest"
# Seconds to wait for a launched game to start its server and load the system
LAUNCH_TIMEOUT = 300

auto_launch = True


class _Game:
    """The shared connection, and the game process when the harness launched it."""

    connection: Any = None
    process: subprocess.Popen | None = None


def connect() -> Any:
    """The connection shared by the tests, opened on first use."""
    if _Game.connection is None:
        _Game.connection = krpc.connect(name="ksatest")
    return _Game.connection


def ensure_game() -> None:
    """Connect to the running game, launching one if no server is running."""
    if _Game.connection is not None:
        return
    try:
        connect()
    except ConnectionRefusedError:
        if not auto_launch:
            raise RuntimeError(
                "No kRPC server is running, and --no-launch is set"
            ) from None
        _launch()


def _launch() -> None:
    LOG_PATH.parent.mkdir(exist_ok=True)
    print(f"ksatest: launching KSA, log in {LOG_PATH}")
    if os.name == "nt":
        script = "run-ksa.bat"
        session: dict[str, Any] = {"creationflags": subprocess.CREATE_NEW_PROCESS_GROUP}
    else:
        script = "run-ksa.sh"
        session = {"start_new_session": True}
    with open(LOG_PATH, "w", encoding="utf-8") as log:
        process = subprocess.Popen(
            [str(ROOT / script)],
            stdout=log,
            stderr=subprocess.STDOUT,
            **session,
        )
    _Game.process = process
    deadline = time.monotonic() + LAUNCH_TIMEOUT
    while True:
        if process.poll() is not None:
            _Game.process = None
            raise RuntimeError(
                f"KSA exited with code {process.returncode}, see {LOG_PATH}"
            )
        if time.monotonic() > deadline:
            stop_game()
            raise RuntimeError(f"KSA did not start a kRPC server, see {LOG_PATH}")
        try:
            conn = connect()
            break
        except ConnectionRefusedError:
            time.sleep(1)
    # The server starts before the game loads the system
    while conn.ksa.active_vehicle is None:
        if time.monotonic() > deadline:
            stop_game()
            raise RuntimeError("KSA did not load a system with a controlled vehicle")
        time.sleep(1)
    conn.testing_tools.suppress_update_popup()
    conn.testing_tools.save_game(SAVE)


def stop_game() -> None:
    """Quit the game, if the harness launched it."""
    process = _Game.process
    if process is None:
        return
    try:
        connect().testing_tools.quit()
    except Exception:  # noqa: BLE001
        pass
    try:
        process.wait(timeout=30)
    except subprocess.TimeoutExpired:
        if os.name == "nt":
            # The script's cmd.exe and the game under it
            subprocess.run(
                ["taskkill", "/f", "/t", "/pid", str(process.pid)],
                stdout=subprocess.DEVNULL,
                stderr=subprocess.DEVNULL,
                check=False,
            )
        else:
            os.killpg(process.pid, signal.SIGTERM)
        process.wait()
    if _Game.connection is not None:
        _Game.connection.close()
    _Game.connection = None
    _Game.process = None


def wait_until(condition, timeout=10.0, interval=0.05) -> None:
    """Wait until condition returns true, failing after timeout seconds."""
    deadline = time.monotonic() + timeout
    while not condition():
        if time.monotonic() > deadline:
            raise AssertionError("Timed out after %.1f s" % timeout)
        time.sleep(interval)


class TestCase(unittest.TestCase):
    """Base class for the tests. Connects to the game before the class runs."""

    conn: Any
    ksa: Any

    @classmethod
    def setUpClass(cls):
        ensure_game()
        cls.conn = connect()
        cls.ksa = cls.conn.ksa

    @classmethod
    def load_save(cls):
        """Load the krpctest save, unpause the game, and let it run for half a second."""
        if not cls.conn.testing_tools.save_exists(SAVE):
            raise RuntimeError(
                f"No {SAVE} save. Run the tests once without --no-launch to create it."
            )
        cls.conn.testing_tools.load_save(SAVE)
        # Engine performance and other solver outputs are filled in by the first steps
        ut = cls.ksa.ut
        cls.ksa.simulation_speed = 1
        wait_until(lambda: cls.ksa.ut > ut + 0.5)

    @classmethod
    def find_vehicle(cls, name):
        """The vehicle with the given name."""
        for vehicle in cls.ksa.vehicles:
            if vehicle.name == name:
                return vehicle
        raise AssertionError(f"No vehicle named {name}")
