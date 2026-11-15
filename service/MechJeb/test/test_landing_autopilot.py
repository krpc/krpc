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


if __name__ == "__main__":
    unittest.main()
