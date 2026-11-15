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


if __name__ == "__main__":
    unittest.main()
