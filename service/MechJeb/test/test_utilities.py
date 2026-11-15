import unittest

from service.MechJeb.test.mechjeb_test_case import MechJebTestCase


class TestUtilities(MechJebTestCase):
    def test_settings(self):
        mj = self.mj
        utilities = mj.utilities
        staging = utilities.staging
        thrust = utilities.thrust
        self.assert_settings(
            [
                (utilities.antennas, "auto_deploy", True),
                (utilities.solar_panels, "auto_deploy", True),
                (staging, "enabled", True),
                (staging, "autostage_pre_delay", 1.5),
                (staging, "autostage_post_delay", 1.5),
                (staging, "autostage_limit", 2),
                (staging, "fairing_max_dynamic_pressure", 1234),
                (staging, "fairing_min_altitude", 12345),
                (staging, "clamp_auto_stage_thrust_pct", 0.5),
                (staging, "fairing_max_aerothermal_flux", 1234),
                (staging, "hot_staging", True),
                (staging, "hot_staging_lead_time", 1.5),
                (thrust, "limit_dynamic_pressure", True),
                (thrust, "max_dynamic_pressure", 12345),
                (thrust, "limit_to_prevent_overheats", True),
                (thrust, "limit_acceleration", True),
                (thrust, "max_acceleration", 12),
                (thrust, "limit_throttle", True),
                (thrust, "max_throttle", 0.5),
                (thrust, "limiter_min_throttle", False),
                (thrust, "min_throttle", 0.25),
                (thrust, "limit_to_prevent_flameout", True),
                (thrust, "flameout_safety_pct", 12),
                (thrust, "manage_intakes", True),
                (thrust, "differential_throttle", True),
                (thrust, "electric_throttle", True),
                (thrust, "electric_throttle_lo", 0.25),
                (thrust, "electric_throttle_hi", 0.5),
            ]
        )

    def test_autostage_once(self):
        staging = self.mj.utilities.staging
        self.assertFalse(staging.enabled)
        self.assertFalse(staging.autostaging_once)
        staging.autostage_once()
        try:
            self.assertTrue(staging.enabled)
            self.assertTrue(staging.autostaging_once)
            with self.assertRaisesRegex(RuntimeError, "Autostaging is enabled"):
                staging.autostage_once()
        finally:
            staging.enabled = False
        self.assertFalse(staging.enabled)
        self.assertFalse(staging.autostaging_once)

    def test_differential_throttle_status(self):
        self.assertIsInstance(
            self.mj.utilities.thrust.differential_throttle_status,
            self.mj.DifferentialThrottleStatus,
        )

    def test_solar_panels(self):
        controller = self.mj.utilities.solar_panels
        self.assertTrue(controller.all_retracted())
        controller.extend_all()
        self.wait_until(lambda: not controller.all_retracted())
        controller.retract_all()
        self.wait_until(controller.all_retracted, timeout=60)


if __name__ == "__main__":
    unittest.main()
