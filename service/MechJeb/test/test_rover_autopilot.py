import unittest

from service.MechJeb.test.mechjeb_test_case import MechJebTestCase


class TestRoverAutopilot(MechJebTestCase):
    def tearDown(self):
        rover = self.mj.rover_autopilot
        rover.stop()
        rover.clear_waypoints()

    def test_settings(self):
        rover = self.mj.rover_autopilot
        self.assert_settings(
            [
                (rover, "control_heading", True),
                (rover, "heading", 12),
                (rover, "control_speed", True),
                (rover, "speed", 12),
                (rover, "stability_control", True),
                (rover, "brake_on_eject", True),
                (rover, "brake_on_energy_depletion", True),
                (rover, "warp_to_daylight", True),
                (rover, "loop_waypoints", True),
            ]
        )

    def test_waypoints(self):
        rover = self.mj.rover_autopilot
        self.assertEqual([], rover.waypoints)
        self.assertEqual(-1, rover.waypoint_index)
        first = rover.add_waypoint(-0.1, -74.5)
        second = rover.add_waypoint(-0.2, -74.6)
        self.assertEqual([first, second], rover.waypoints)
        self.assertAlmostEqual(-0.1, first.latitude, places=6)
        self.assertAlmostEqual(-74.6, second.longitude, places=6)
        self.assertEqual("", first.name)
        self.assertEqual(5, first.radius)
        self.assertEqual(0, first.min_speed)
        self.assertEqual(0, first.max_speed)
        rover.clear_waypoints()
        self.assertEqual([], rover.waypoints)
        with self.assertRaises(self.conn.krpc.ObjectDestroyedException):
            _ = first.latitude

    def test_waypoint_settings(self):
        waypoint = self.mj.rover_autopilot.add_waypoint(-0.1, -74.5)
        self.assert_settings(
            [
                (waypoint, "latitude", -0.2),
                (waypoint, "longitude", -74.6),
                (waypoint, "radius", 12),
                (waypoint, "name", "Rock"),
                (waypoint, "min_speed", 2),
                (waypoint, "max_speed", 8),
            ]
        )

    def test_drive(self):
        rover = self.mj.rover_autopilot
        with self.assertRaisesRegex(RuntimeError, "no waypoints"):
            rover.drive()
        rover.add_waypoint(-0.1, -74.5)
        rover.drive()
        self.assertTrue(rover.control_heading)
        self.assertTrue(rover.control_speed)
        self.assertEqual(0, rover.waypoint_index)
        rover.stop()
        self.assertFalse(rover.control_heading)
        self.assertFalse(rover.control_speed)

    def test_drive_to_target(self):
        rover = self.mj.rover_autopilot
        with self.assertRaisesRegex(RuntimeError, "no target"):
            rover.drive_to_target()
        target = self.mj.target_controller
        self.mj.landing_guidance.set_target(self.sc.bodies["Kerbin"], -0.1, -74.5)
        try:
            self.wait_until(lambda: target.position_target_exists)
            rover.drive_to_target()
            self.assertEqual(1, len(rover.waypoints))
            self.assertAlmostEqual(-0.1, rover.waypoints[0].latitude, places=3)
            self.assertTrue(rover.control_heading)
        finally:
            self.sc.clear_target()


if __name__ == "__main__":
    unittest.main()
