import unittest

from service.MechJeb.test.mechjeb_test_case import MechJebTestCase


class TestTranslatron(MechJebTestCase):
    def test_settings(self):
        self.assert_settings([(self.mj.translatron, "kill_horizontal_speed", True)])

    def test_read_only(self):
        self.assertEqual(self.mj.TranslatronMode.off, self.mj.translatron.mode)

    def test_invalid_mode(self):
        mj = self.mj
        with self.assertRaises(mj.MJServiceException):
            mj.translatron.mode = mj.TranslatronMode.direct

    def test_keep_vertical(self):
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
