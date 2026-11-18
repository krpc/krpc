import unittest

from service.MechJeb.test.mechjeb_test_case import MechJebTestCase


class TestAttitudeAdjustment(MechJebTestCase):
    def test_settings(self):
        adjustment = self.mj.attitude_adjustment
        self.assert_settings(
            [
                (adjustment, "rcs_auto", True),
                (adjustment, "controller", self.mj.AttitudeController.mj),
            ]
        )

    def test_default_controller(self):
        self.assertEqual(
            self.mj.AttitudeController.better,
            self.mj.attitude_adjustment.controller,
        )


if __name__ == "__main__":
    unittest.main()
