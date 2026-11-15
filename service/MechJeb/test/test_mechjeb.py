import unittest

from krpctools.game.ksp import TestCase
from service.MechJeb.test.mechjeb_test_case import MechJebTestCase


class TestMechJeb(MechJebTestCase):
    def test_ready(self):
        self.assertTrue(self.mj.ready)

    def test_same_object(self):
        mj = self.mj
        self.assertEqual(mj.thrust_controller, mj.ascent_autopilot.thrust_controller)
        self.assertEqual(mj.staging_controller, mj.ascent_autopilot.staging_controller)
        self.assertEqual(mj.rcs_controller, mj.smart_rcs.rcs_controller)


class TestMechJebNotInstalled(TestCase):
    def test_not_installed(self):
        conn = self.connect()
        with self.assertRaisesRegex(
            RuntimeError, "MechJeb is not available: not installed"
        ):
            _ = conn.mech_jeb.smart_ass


if __name__ == "__main__":
    unittest.main()
