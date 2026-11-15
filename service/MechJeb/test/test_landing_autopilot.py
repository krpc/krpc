import unittest

from service.MechJeb.test.mechjeb_test_case import MechJebTestCase


class TestLandingAutopilot(MechJebTestCase):
    def test_settings(self):
        landing = self.mj.landing_autopilot
        self.assert_settings(
            [
                (landing, "touchdown_speed", 1.5),
                (landing, "deploy_gears", False),
                (landing, "limit_gears_stage", 2),
                (landing, "deploy_chutes", False),
                (landing, "limit_chutes_stage", 2),
                (landing, "rcs_adjustment", False),
            ]
        )

    def test_read_only(self):
        self.assertEqual("Off", self.mj.landing_autopilot.status)

    def test_landed(self):
        landing = self.mj.landing_autopilot
        target = self.mj.target_controller
        with self.assertRaisesRegex(RuntimeError, "no position target"):
            landing.land_at_position_target()
        target.set_position_target(self.sc.bodies["Kerbin"], -0.1, -74.5)
        try:
            self.wait_until(lambda: target.position_target_exists)
            with self.assertRaisesRegex(RuntimeError, "landed"):
                landing.land_at_position_target()
        finally:
            self.sc.clear_target()
        with self.assertRaisesRegex(RuntimeError, "landed"):
            landing.land_untargeted()
        self.assertFalse(landing.enabled)


if __name__ == "__main__":
    unittest.main()
