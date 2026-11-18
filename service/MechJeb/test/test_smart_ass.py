import math
import unittest

from krpctools.game.geometry import angle_between, dot, norm
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
                (smart_ass, "roll", 12),
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

    def test_roll(self):
        smart_ass = self.mj.smart_ass
        frame = self.vessel.orbital_reference_frame

        def top():
            return self.sc.transform_direction(
                (0, 0, -1), self.vessel.reference_frame, frame
            )

        def settled():
            aligned = angle_between(self.vessel.direction(frame), (0, 1, 0)) < 2
            return aligned and norm(self.vessel.angular_velocity(frame)) < 0.01

        smart_ass.autopilot_mode = self.mj.SmartASSAutopilotMode.prograde
        smart_ass.force_roll = True
        smart_ass.roll = 0
        smart_ass.update(False)
        self.wait_until(settled, timeout=60)
        start = top()
        smart_ass.roll = 90
        smart_ass.update(False)
        self.wait_until(lambda: abs(angle_between(start, top()) - 90) < 5, timeout=60)

    def test_surface_axes(self):
        smart_ass = self.mj.smart_ass
        flight = self.vessel.flight(self.vessel.surface_reference_frame)
        self.set_pitch_heading_roll(0, 90, 0)
        smart_ass.autopilot_mode = self.mj.SmartASSAutopilotMode.surface
        smart_ass.surface_heading = 90
        smart_ass.surface_pitch = 60
        smart_ass.surface_roll = 0
        smart_ass.force_pitch = False
        smart_ass.update(False)
        # Pitch is left alone
        ut = self.sc.ut
        self.wait_until(lambda: self.sc.ut > ut + 10)
        self.assertLess(abs(flight.pitch), 15)
        smart_ass.force_pitch = True
        smart_ass.update(False)
        self.wait_until(lambda: flight.pitch > 55, timeout=60)


if __name__ == "__main__":
    unittest.main()
