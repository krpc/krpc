import unittest

from service.MechJeb.test.mechjeb_test_case import MechJebTestCase


class TestTargetController(MechJebTestCase):
    def test_position_target(self):
        target = self.mj.target_controller
        target.set_position_target(self.sc.bodies["Kerbin"], -0.1, -74.5)
        try:
            self.wait_until(lambda: target.position_target_exists)
            self.assertNotEqual("", target.get_position_target_string())
            self.assertEqual(3, len(target.get_position_target_position()))
        finally:
            self.sc.clear_target()
            self.wait_while(lambda: target.position_target_exists)

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
