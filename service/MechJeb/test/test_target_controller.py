import unittest

from service.MechJeb.test.mechjeb_test_case import MechJebTestCase


class TestTargetController(MechJebTestCase):
    def test_without_target(self):
        target = self.mj.target_controller
        self.assertFalse(target.normal_target_exists)
        self.assertFalse(target.position_target_exists)
        for name in (
            "can_align",
            "target_orbit",
            "position",
            "distance",
            "relative_velocity",
            "relative_position",
            "docking_axis",
        ):
            with self.subTest(member=name):
                with self.assertRaisesRegex(RuntimeError, "There is no target"):
                    getattr(target, name)


if __name__ == "__main__":
    unittest.main()
