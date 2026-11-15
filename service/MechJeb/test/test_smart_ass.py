import math
import unittest

from krpctools.game.geometry import dot, norm
from service.MechJeb.test.mechjeb_test_case import (
    MechJebInOrbitTestCase,
    MechJebTestCase,
)


class TestSmartASS(MechJebTestCase):
    def test_settings(self):
        smart_ass = self.mj.smart_ass
        self.assert_settings(
            [
                (smart_ass, "interface_mode", self.mj.SmartASSInterfaceMode.target),
                (smart_ass, "force_yaw", False),
                (smart_ass, "force_pitch", False),
                (smart_ass, "force_roll", True),
                (smart_ass, "surface_heading", 12),
                (smart_ass, "surface_pitch", 12),
                (smart_ass, "surface_roll", 12),
                (smart_ass, "surface_vel_yaw", 12),
                (smart_ass, "surface_vel_pitch", 12),
                (smart_ass, "surface_vel_roll", 12),
                (
                    smart_ass,
                    "advanced_reference",
                    self.mj.AttitudeReference.surface_horizontal,
                ),
                (smart_ass, "advanced_direction", self.mj.Direction.left),
            ]
        )

    def test_read_only(self):
        self.assertEqual(
            self.mj.SmartASSAutopilotMode.off, self.mj.smart_ass.autopilot_mode
        )

    def test_invalid_mode(self):
        with self.assertRaises(self.mj.MJServiceException):
            self.mj.smart_ass.autopilot_mode = self.mj.SmartASSAutopilotMode.automatic


class TestSmartASSInOrbit(MechJebInOrbitTestCase):
    def tearDown(self):
        smart_ass = self.mj.smart_ass
        smart_ass.autopilot_mode = self.mj.SmartASSAutopilotMode.off
        smart_ass.update(False)
        smart_ass.force_pitch = True
        smart_ass.force_yaw = True
        smart_ass.force_roll = False
        super().tearDown()

    def test_normal_plus(self):
        smart_ass = self.mj.smart_ass
        smart_ass.autopilot_mode = self.mj.SmartASSAutopilotMode.normal_plus
        smart_ass.update(False)
        frame = self.vessel.orbital_reference_frame
        self.wait_until(
            lambda: (
                dot(self.vessel.direction(frame), (0, 0, 1)) > math.cos(math.radians(5))
            ),
            timeout=60,
        )
        self.assertAlmostEqual(1, norm(self.vessel.direction(frame)), places=3)


if __name__ == "__main__":
    unittest.main()
