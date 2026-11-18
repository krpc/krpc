import unittest

from service.MechJeb.test.mechjeb_test_case import MechJebTestCase


class TestLandingGuidance(MechJebTestCase):
    def test_settings(self):
        landing = self.mj.landing_guidance
        self.assert_settings(
            [
                (landing, "touchdown_speed", 1.5),
                (landing, "deploy_gears", False),
                (landing, "limit_gears_stage", 2),
                (landing, "deploy_chutes", False),
                (landing, "limit_chutes_stage", 2),
                (landing, "rcs_adjustment", False),
                (landing, "autowarp", False),
            ]
        )

    def test_read_only(self):
        self.assertEqual("Off", self.mj.landing_guidance.status)

    def test_target(self):
        landing = self.mj.landing_guidance
        target = self.mj.target_controller
        landing.set_target(self.sc.bodies["Kerbin"], -0.1, -74.5)
        try:
            self.wait_until(lambda: target.position_target_exists)
            self.assertNotEqual("", landing.get_target_string())
            self.assertEqual(3, len(landing.get_target_position()))
        finally:
            self.sc.clear_target()
            self.wait_while(lambda: target.position_target_exists)
        with self.assertRaisesRegex(RuntimeError, "no landing target"):
            landing.get_target_position()

    def test_landed(self):
        landing = self.mj.landing_guidance
        target = self.mj.target_controller
        with self.assertRaisesRegex(RuntimeError, "no landing target"):
            landing.land_at_target()
        landing.set_target(self.sc.bodies["Kerbin"], -0.1, -74.5)
        try:
            self.wait_until(lambda: target.position_target_exists)
            with self.assertRaisesRegex(RuntimeError, "landed"):
                landing.land_at_target()
        finally:
            self.sc.clear_target()
        with self.assertRaisesRegex(RuntimeError, "landed"):
            landing.land_somewhere()
        self.assertFalse(landing.enabled)


if __name__ == "__main__":
    unittest.main()
