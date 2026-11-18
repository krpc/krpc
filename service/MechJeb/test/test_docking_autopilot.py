import unittest

from service.MechJeb.test.mechjeb_test_case import MechJebTestCase


class TestDockingAutopilot(MechJebTestCase):
    def test_settings(self):
        docking = self.mj.docking_autopilot
        self.assert_settings(
            [
                (docking, "speed_limit", 1.5),
                (docking, "force_roll", True),
                (docking, "roll", 12),
                (docking, "override_safe_distance", True),
                (docking, "overriden_safe_distance", 12),
                (docking, "override_target_size", True),
                (docking, "overriden_target_size", 12),
            ]
        )

    def test_read_only(self):
        docking = self.mj.docking_autopilot
        self.assertIsInstance(docking.status, str)
        self.assertGreater(docking.safe_distance, 0)
        self.assertGreater(docking.target_size, 0)


if __name__ == "__main__":
    unittest.main()
