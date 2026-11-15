import unittest

from service.MechJeb.test.mechjeb_test_case import MechJebTestCase


class TestAirplaneAutopilot(MechJebTestCase):
    def test_settings(self):
        airplane = self.mj.airplane_autopilot
        self.assert_settings(
            [
                (airplane, "heading_hold_enabled", True),
                (airplane, "altitude_hold_enabled", True),
                (airplane, "vert_speed_hold_enabled", True),
                (airplane, "roll_hold_enabled", True),
                (airplane, "speed_hold_enabled", True),
                (airplane, "altitude_target", 1234),
                (airplane, "heading_target", 123),
                (airplane, "roll_target", 12),
                (airplane, "speed_target", 123),
                (airplane, "vert_speed_target", 12),
                (airplane, "bank_angle", 12),
                (airplane, "acc_kp", 0.12),
                (airplane, "acc_ki", 0.12),
                (airplane, "acc_kd", 0.12),
                (airplane, "pit_kp", 0.12),
                (airplane, "pit_ki", 0.12),
                (airplane, "pit_kd", 0.12),
                (airplane, "rol_kp", 0.12),
                (airplane, "rol_ki", 0.12),
                (airplane, "rol_kd", 0.12),
                (airplane, "yaw_kp", 0.12),
                (airplane, "yaw_ki", 0.12),
                (airplane, "yaw_kd", 0.12),
                (airplane, "yaw_limit", 12),
                (airplane, "roll_limit", 12),
                (airplane, "pitch_up_limit", 12),
                (airplane, "pitch_down_limit", 12),
            ]
        )


if __name__ == "__main__":
    unittest.main()
