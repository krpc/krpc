import unittest
import krpctest


class TestServo(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("InfernalRobotics")
        cls.remove_other_vessels()
        cls.ir = cls.connect().infernal_robotics
        cls.vessel = cls.connect().space_center.active_vessel

    def test_rotatron(self):
        servo = self.ir.servo_with_name(self.vessel, "Rotatron - Basic")
        self.assertEqual("Rotatron - Basic", servo.name)
        self.assertEqual("Rotatron - Basic", servo.part.title)
        self.assertEqual(0, servo.position)
        self.assertEqual(-360, servo.min_config_position)
        self.assertEqual(360, servo.max_config_position)
        self.assertEqual(-360, servo.min_position)
        self.assertEqual(360, servo.max_position)
        self.assertAlmostEqual(1, servo.config_speed)
        self.assertAlmostEqual(1, servo.speed)
        self.assertAlmostEqual(0, servo.current_speed)
        self.assertAlmostEqual(4, servo.acceleration)
        self.assertFalse(servo.is_moving)
        self.assertFalse(servo.is_free_moving)
        self.assertFalse(servo.is_locked)
        self.assertFalse(servo.is_axis_inverted)

    def test_rail(self):
        servo = self.ir.servo_with_name(self.vessel, "Rail Gantry - Short")
        self.assertEqual("Rail Gantry - Short", servo.name)
        self.assertEqual("Rail Gantry - Short", servo.part.title)
        self.assertAlmostEqual(0, servo.position, places=3)
        self.assertEqual(-1.25, servo.min_config_position)
        self.assertEqual(1.25, servo.max_config_position)
        self.assertEqual(-1.25, servo.min_position)
        self.assertEqual(1.25, servo.max_position)
        self.assertAlmostEqual(1, servo.config_speed)
        self.assertAlmostEqual(1, servo.speed)
        self.assertAlmostEqual(0, servo.current_speed)
        self.assertAlmostEqual(4, servo.acceleration)
        self.assertFalse(servo.is_moving)
        self.assertFalse(servo.is_free_moving)
        self.assertFalse(servo.is_locked)
        self.assertFalse(servo.is_axis_inverted)

    def test_move(self):
        servo = self.ir.servo_with_name(self.vessel, "Rail Gantry - Short")
        self.assertFalse(servo.is_moving)
        servo.move_right()
        self.wait()
        while servo.is_moving:
            self.wait()
        self.assertFalse(servo.is_moving)
        servo.move_left()
        self.wait()
        while servo.is_moving:
            self.wait()
        self.assertFalse(servo.is_moving)

    def test_stop(self):
        servo = self.ir.servo_with_name(self.vessel, "Rail Gantry - Short")
        self.assertFalse(servo.is_moving)
        servo.move_right()
        self.wait()
        self.assertTrue(servo.is_moving)
        servo.stop()
        self.wait()
        self.assertFalse(servo.is_moving)
        self.wait()
        servo.move_left()
        while servo.is_moving:
            self.wait()
        self.assertFalse(servo.is_moving)


if __name__ == "__main__":
    unittest.main()
