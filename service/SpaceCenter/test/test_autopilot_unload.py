"""Tests the auto-pilot against a vessel that the game unloads.

A vessel that leaves physics range is unloaded: its parts are destroyed and it flies on
rails. The control loop has no rate to measure and no actuator to command until the game
loads it again, and a loop that runs anyway throws once per physics tick.
"""

import os

import krpctest
from krpctest.env import get_ksp_dir


class TestAutoPilotUnload(krpctest.TestCase):
    """Both halves of Multi.craft are undocked into two vessels sharing an orbit, the
    auto-pilot is engaged on the one that is not active, and the active vessel is
    teleported onto a much higher orbit to take the other one out of range."""

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("Multi")
        cls.remove_other_vessels()
        cls.set_circular_orbit("Kerbin", 100000)
        cls.space_center = cls.connect().space_center
        next(iter(cls.space_center.active_vessel.parts.docking_ports)).undock()
        cls.wait(1)
        cls.vessel = cls.space_center.active_vessel
        cls.other = next(v for v in cls.space_center.vessels if v != cls.vessel)

    @staticmethod
    def log_position():
        return os.path.getsize(os.path.join(get_ksp_dir(), "KSP.log"))

    @staticmethod
    def log_since(position):
        """The text the game has logged since the given position in its log file."""
        with open(
            os.path.join(get_ksp_dir(), "KSP.log"), encoding="utf-8", errors="replace"
        ) as log:
            log.seek(position)
            return log.read()

    def test_unloaded_vessel_is_not_flown(self):
        auto_pilot = self.other.auto_pilot
        auto_pilot.reference_frame = self.other.surface_reference_frame
        auto_pilot.target_pitch_and_heading(0, 90)
        auto_pilot.engaged = True
        self.wait(1)
        self.assertTrue(self.other.loaded)

        position = self.log_position()
        self.set_circular_orbit("Kerbin", 200000)
        self.wait_until(
            lambda: not self.other.loaded, message="the other vessel to unload"
        )
        self.wait(3)

        self.assertTrue(auto_pilot.engaged, "the auto-pilot stays engaged")
        # Report the first failure rather than the whole log, which runs to megabytes.
        errors = [
            line
            for line in self.log_since(position).splitlines()
            if "Auto-pilot failed" in line
        ]
        self.assertEqual(
            [], errors[:1], "the auto-pilot ran against the unloaded vessel"
        )
