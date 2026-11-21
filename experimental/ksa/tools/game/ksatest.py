"""Test harness for the KSA integration tests, on krpctools.game.

The game is bazel_ksa.BazelKSA, which `bazel run //experimental/ksa/tools/game:test-ksa`
passes. It launches KSA when no server is running, saves the loaded game as krpctest, and
quits the game when the session ends. Each test class loads that save, so starts from the game's default
start state.
"""

import os
import sys
import time
from typing import Any

import bazel_ksa

from krpctools.game import testcase

# The kRPC for KSA folder of the repository
ROOT = bazel_ksa.repo_root() / "experimental" / "ksa"
SAVE = "krpctest"


def wait_until(condition, timeout=10.0, interval=0.05) -> None:
    """Wait until condition returns true, failing after timeout seconds."""
    deadline = time.monotonic() + timeout
    while not condition():
        if time.monotonic() > deadline:
            raise AssertionError("Timed out after %.1f s" % timeout)
        time.sleep(interval)


def python_env() -> dict[str, str]:
    """The environment for a Python subprocess, which finds the modules this process does."""
    return dict(os.environ, PYTHONPATH=os.pathsep.join(sys.path))


class TestCase(testcase.TestCase):
    """Base class for the tests. The game runs, and the class has its connection, before
    setUpClass."""

    conn: Any
    ksa: Any

    @classmethod
    def ensure_game(cls):
        super().ensure_game()
        cls.conn = cls.connect()
        cls.ksa = cls.conn.ksa

    @classmethod
    def load_save(cls):
        """Load the krpctest save, unpause the game, and let it run for half a second."""
        if not cls.conn.ksa.save_exists(SAVE):
            raise RuntimeError(
                f"No {SAVE} save. Run the tests once without --no-launch to create it."
            )
        cls.conn.ksa.load(SAVE)
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
