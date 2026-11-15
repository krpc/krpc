import unittest

from krpctools.game.ksp import TestCase
from service.MechJeb.test.mechjeb_test_case import MechJebTestCase


class TestMechJeb(MechJebTestCase):
    def test_ready(self):
        self.assertTrue(self.mj.ready)

    def test_autowarp(self):
        mj = self.mj
        windows = [
            mj.maneuver_planner,
            mj.ascent_guidance.settings,
            mj.landing_guidance,
            mj.rendezvous_autopilot,
        ]
        old = mj.maneuver_planner.autowarp
        try:
            for window in windows:
                window.autowarp = not old
                self.assertEqual([not old] * 4, [w.autowarp for w in windows])
                window.autowarp = old
        finally:
            mj.maneuver_planner.autowarp = old


class TestMechJebNotInstalled(TestCase):
    def test_not_installed(self):
        conn = self.connect()
        with self.assertRaisesRegex(
            RuntimeError, "MechJeb is not available: not installed"
        ):
            _ = conn.mech_jeb.smart_ass


if __name__ == "__main__":
    unittest.main()
