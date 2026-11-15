import unittest

from service.MechJeb.test.mechjeb_test_case import MechJebTestCase


class TestAscentGuidance(MechJebTestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.ascent = cls.mj.ascent_guidance

    def test_settings(self):
        ascent = self.ascent
        settings = ascent.settings
        classic = ascent.classic_path
        pvg = ascent.pvg_settings
        self.assert_settings(
            [
                (ascent, "desired_orbit_altitude", 123000),
                (ascent, "desired_inclination", 12),
                (settings, "corrective_steering", True),
                (settings, "corrective_steering_gain", 1.5),
                (settings, "force_roll", False),
                (settings, "vertical_roll", 12),
                (settings, "turn_roll", 12),
                (settings, "autodeploy_solar_panels", False),
                (settings, "auto_deploy_antennas", False),
                (settings, "skip_circularization", True),
                (settings, "autostage", False),
                (settings, "limit_angle_of_attack", False),
                (settings, "max_angle_of_attack", 12),
                (settings, "angle_of_attack_limit_fadeout_pressure", 1234),
                (settings, "autowarp", False),
                (settings, "roll_altitude", 1234),
                (ascent, "desired_lan", 12),
                (ascent, "desired_fpa", 2.5),
                (ascent, "override_warp_to_plane", True),
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
                (ascent, "desired_apoapsis", 123000),
                (ascent, "attach_alt_flag", True),
                (ascent, "desired_attach_alt", 123000),
                (pvg, "dynamic_pressure_trigger", 1234),
                (pvg, "staging_trigger", 2),
                (pvg, "staging_trigger_flag", True),
                (pvg, "fixed_coast", False),
                (pvg, "fixed_coast_length", 12),
                (pvg, "min_delta_v", 12.5),
                (pvg, "last_stage", 2),
                (pvg, "optimize_stage_flag", False),
                (pvg, "optimize_stage", 2),
                (pvg, "unguided_stages_flag", True),
                (pvg, "unguided_stages", [1, 2]),
                (pvg, "coast_stage_flag", True),
                (pvg, "coast_stage", 2),
                (pvg, "coast_before_flag", True),
                (pvg, "ullage_lead_time", 12),
                (pvg, "spinup_stage_flag", False),
                (pvg, "spinup_stage", 2),
                (pvg, "spinup_angular_velocity", 0.5),
                (pvg, "spinup_lead_time", 12),
                (pvg, "limit_qa", 1234.5),
            ]
        )

    def test_read_only(self):
        ascent = self.ascent
        classic = ascent.classic_path
        self.assertEqual("Off", ascent.status)
        self.assertFalse(ascent.enabled)
        self.assertEqual(self.mj.AscentLaunchMode.normal, ascent.launch_mode)
        self.assertGreater(classic.auto_turn_start_altitude, 0)
        self.assertGreater(classic.auto_turn_start_velocity, 0)
        self.assertGreater(classic.auto_turn_end_altitude, 0)
        self.assertFalse(ascent.settings.limiting_angle_of_attack)
        self.assertIsInstance(ascent.settings.current_max_angle_of_attack, float)

    def test_ascent_type(self):
        ascent = self.ascent
        types = self.mj.AscentType
        self.assertEqual(types.classic, ascent.ascent_type)
        ascent.ascent_type = types.pvg
        self.assertEqual(types.pvg, ascent.ascent_type)
        ascent.ascent_type = types.classic
        self.assertEqual(types.classic, ascent.ascent_type)

    def test_same_objects(self):
        settings = self.ascent.settings
        self.assertEqual(self.mj.utilities.staging, settings.staging)
        self.assertEqual(self.mj.utilities.thrust, settings.thrust)

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
            ascent.ascent_type = self.mj.AscentType.pvg
            ascent.enabled = False
            ascent.ascent_type = self.mj.AscentType.classic
            self.assertFalse(ascent.enabled)
        finally:
            ascent.enabled = False
            ascent.ascent_type = self.mj.AscentType.classic

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

            ascent.ascent_type = self.mj.AscentType.pvg
            with self.assertRaises(RuntimeError):
                ascent.launch_to_rendezvous()
        finally:
            ascent.ascent_type = self.mj.AscentType.classic
            self.sc.clear_target()

    def test_unguided_stages(self):
        pvg = self.ascent.pvg_settings
        old = pvg.unguided_stages
        try:
            pvg.unguided_stages = [3, 1]
            self.assertEqual([3, 1], pvg.unguided_stages)
            with self.assertRaisesRegex(ValueError, "negative"):
                pvg.unguided_stages = [-1]
            self.assertEqual([3, 1], pvg.unguided_stages)
        finally:
            pvg.unguided_stages = old

    def test_lan_launches(self):
        ascent = self.ascent
        modes = self.mj.AscentLaunchMode
        with self.assertRaisesRegex(RuntimeError, "PVG"):
            ascent.launch_to_lan()
        ascent.ascent_type = self.mj.AscentType.pvg
        self.sc.target_body = self.sc.bodies["Mun"]
        try:
            ascent.launch_to_lan()
            self.assertEqual(modes.lan, ascent.launch_mode)
            ascent.abort_timed_launch()
            self.assertEqual(modes.normal, ascent.launch_mode)

            target = self.mj.target_controller
            self.wait_until(lambda: target.normal_target_exists)
            ascent.launch_to_match_lan()
            self.assertEqual(modes.match_lan, ascent.launch_mode)
            ascent.abort_timed_launch()
            self.assertEqual(modes.normal, ascent.launch_mode)
        finally:
            ascent.ascent_type = self.mj.AscentType.classic
            self.sc.clear_target()


if __name__ == "__main__":
    unittest.main()
