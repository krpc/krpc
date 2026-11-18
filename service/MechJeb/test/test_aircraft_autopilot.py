import unittest

from service.MechJeb.test.mechjeb_test_case import MechJebTestCase


class TestAircraftAutopilot(MechJebTestCase):
    def test_settings(self):
        aircraft = self.mj.aircraft_autopilot
        self.assert_settings(
            [
                (aircraft, "heading_hold_enabled", True),
                (aircraft, "altitude_hold_enabled", True),
                (aircraft, "vert_speed_hold_enabled", True),
                (aircraft, "roll_hold_enabled", True),
                (aircraft, "speed_hold_enabled", True),
                (aircraft, "altitude_target", 1234),
                (aircraft, "heading_target", 123),
                (aircraft, "roll_target", 12),
                (aircraft, "speed_target", 123),
                (aircraft, "vert_speed_target", 12),
                (aircraft, "bank_angle", 12),
                (aircraft, "acc_kp", 0.12),
                (aircraft, "acc_ki", 0.12),
                (aircraft, "acc_kd", 0.12),
                (aircraft, "pit_kp", 0.12),
                (aircraft, "pit_ki", 0.12),
                (aircraft, "pit_kd", 0.12),
                (aircraft, "rol_kp", 0.12),
                (aircraft, "rol_ki", 0.12),
                (aircraft, "rol_kd", 0.12),
                (aircraft, "yaw_kp", 0.12),
                (aircraft, "yaw_ki", 0.12),
                (aircraft, "yaw_kd", 0.12),
                (aircraft, "yaw_limit", 12),
                (aircraft, "roll_limit", 12),
                (aircraft, "pitch_up_limit", 12),
                (aircraft, "pitch_down_limit", 12),
            ]
        )


if __name__ == "__main__":
    unittest.main()
