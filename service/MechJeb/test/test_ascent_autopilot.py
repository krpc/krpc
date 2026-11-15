import unittest

from service.MechJeb.test.mechjeb_test_case import MechJebTestCase


class TestAscentAutopilot(MechJebTestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.ascent = cls.mj.ascent_autopilot

    def test_settings(self):
        ascent = self.ascent
        classic = ascent.ascent_path_classic
        pvg = ascent.ascent_path_pvg
        self.assert_settings(
            [
                (ascent, "desired_orbit_altitude", 123000),
                (ascent, "desired_inclination", 12),
                (ascent, "corrective_steering", True),
                (ascent, "corrective_steering_gain", 1.5),
                (ascent, "force_roll", False),
                (ascent, "vertical_roll", 12),
                (ascent, "turn_roll", 12),
                (ascent, "autodeploy_solar_panels", False),
                (ascent, "auto_deploy_antennas", False),
                (ascent, "skip_circularization", True),
                (ascent, "autostage", False),
                (ascent, "limit_angle_of_attack", False),
                (ascent, "max_angle_of_attack", 12),
                (ascent, "angle_of_attack_limit_fadeout_pressure", 1234),
                (ascent, "launch_phase_angle", 12),
                (ascent, "launch_lan_difference", 12),
                (ascent, "warp_count_down", 12),
                (classic, "turn_start_altitude", 1234),
                (classic, "turn_start_velocity", 123),
                (classic, "turn_end_altitude", 12345),
                (classic, "turn_end_angle", 12),
                (classic, "turn_shape_exponent", 0.5),
                (classic, "auto_path", False),
                (classic, "auto_turn_percent", 0.5),
                (classic, "auto_turn_speed_factor", 12),
                (pvg, "pitch_start_velocity", 12),
                (pvg, "pitch_rate", 0.75),
                (pvg, "desired_apoapsis", 123000),
                (pvg, "attach_alt_flag", True),
                (pvg, "desired_attach_alt", 123000),
                (pvg, "dynamic_pressure_trigger", 1234),
                (pvg, "staging_trigger", 2),
                (pvg, "staging_trigger_flag", True),
                (pvg, "fixed_coast", False),
                (pvg, "fixed_coast_length", 12),
            ]
        )

    def test_read_only(self):
        ascent = self.ascent
        classic = ascent.ascent_path_classic
        self.assertEqual("Off", ascent.status)
        self.assertFalse(ascent.enabled)
        self.assertEqual(self.mj.AscentLaunchMode.normal, ascent.launch_mode)
        self.assertGreater(classic.auto_turn_start_altitude, 0)
        self.assertGreater(classic.auto_turn_start_velocity, 0)
        self.assertGreater(classic.auto_turn_end_altitude, 0)

    def test_ascent_path_index(self):
        ascent = self.ascent
        self.assertEqual(0, ascent.ascent_path_index)
        ascent.ascent_path_index = 2
        self.assertEqual(2, ascent.ascent_path_index)
        ascent.ascent_path_index = 0
        self.assertEqual(0, ascent.ascent_path_index)
        for index in (1, 3, -1):
            with self.assertRaises(ValueError):
                ascent.ascent_path_index = index
        self.assertEqual(0, ascent.ascent_path_index)

    def test_enable(self):
        ascent = self.ascent
        ascent.enabled = True
        try:
            self.assertTrue(ascent.enabled)
            self.wait_until(lambda: ascent.status != "Off")
        finally:
            ascent.enabled = False
        self.assertFalse(ascent.enabled)

    def test_disable_after_path_change(self):
        ascent = self.ascent
        ascent.enabled = True
        try:
            ascent.ascent_path_index = 2
            ascent.enabled = False
            ascent.ascent_path_index = 0
            self.assertFalse(ascent.enabled)
        finally:
            ascent.enabled = False
            ascent.ascent_path_index = 0

    def test_timed_launch(self):
        ascent = self.ascent
        modes = self.mj.AscentLaunchMode
        self.sc.target_body = self.sc.bodies["Mun"]
        try:
            target = self.mj.target_controller
            self.wait_until(lambda: target.normal_target_exists)
            self.assertAlmostEqual(
                self.sc.bodies["Mun"].orbit.semi_major_axis,
                target.target_orbit.semi_major_axis,
                delta=1,
            )

            ascent.launch_to_target_plane()
            self.assertEqual(modes.target_plane, ascent.launch_mode)
            ascent.abort_timed_launch()
            self.assertEqual(modes.normal, ascent.launch_mode)

            ascent.launch_to_rendezvous()
            self.assertEqual(modes.rendezvous, ascent.launch_mode)
            ascent.abort_timed_launch()
            self.assertEqual(modes.normal, ascent.launch_mode)

            ascent.ascent_path_index = 2
            with self.assertRaises(RuntimeError):
                ascent.launch_to_rendezvous()
        finally:
            ascent.ascent_path_index = 0
            self.sc.clear_target()


if __name__ == "__main__":
    unittest.main()
