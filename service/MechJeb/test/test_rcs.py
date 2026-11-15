import unittest

from service.MechJeb.test.mechjeb_test_case import MechJebTestCase


class TestRCS(MechJebTestCase):
    def test_settings(self):
        mj = self.mj
        self.assert_settings(
            [
                (mj.smart_rcs, "auto_disable_smart_rcs", False),
                (mj.translatron, "kill_horizontal_speed", True),
                (mj.rcs_controller, "rcs_throttle", False),
                (mj.rcs_controller, "rcs_for_rotation", False),
            ]
        )

    def test_read_only(self):
        mj = self.mj
        self.assertEqual(mj.TranslatronMode.off, mj.translatron.mode)
        self.assertEqual(mj.SmartRCSMode.off, mj.smart_rcs.mode)

    def test_invalid_modes(self):
        mj = self.mj
        with self.assertRaises(mj.MJServiceException):
            mj.smart_rcs.mode = mj.SmartRCSMode.zero_velocity
        with self.assertRaises(mj.MJServiceException):
            mj.translatron.mode = mj.TranslatronMode.direct

    def test_translatron(self):
        translatron = self.mj.translatron
        translatron.mode = self.mj.TranslatronMode.keep_vertical
        try:
            self.assertEqual(self.mj.TranslatronMode.keep_vertical, translatron.mode)
            translatron.translation_speed = 0
            self.assertEqual(0, translatron.translation_speed)
        finally:
            translatron.mode = self.mj.TranslatronMode.off
        self.assertEqual(self.mj.TranslatronMode.off, translatron.mode)


if __name__ == "__main__":
    unittest.main()
