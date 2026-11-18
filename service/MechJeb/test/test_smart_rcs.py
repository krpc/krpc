import unittest

from service.MechJeb.test.mechjeb_test_case import MechJebTestCase


class TestSmartRCS(MechJebTestCase):
    def test_settings(self):
        mj = self.mj
        self.assert_settings(
            [
                (mj.smart_rcs, "auto_disable_smart_rcs", False),
                (mj.smart_rcs, "rcs_throttle", False),
                (mj.smart_rcs, "rcs_for_rotation", False),
            ]
        )

    def test_read_only(self):
        mj = self.mj
        self.assertEqual(mj.SmartRCSMode.off, mj.smart_rcs.mode)

    def test_invalid_mode(self):
        mj = self.mj
        with self.assertRaises(mj.MJServiceException):
            mj.smart_rcs.mode = mj.SmartRCSMode.zero_velocity


if __name__ == "__main__":
    unittest.main()
