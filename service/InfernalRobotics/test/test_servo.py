import unittest
import time
import krpctest

class TestServo(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        krpctest.new_save()
        krpctest.launch_vessel_from_vab('InfernalRobotics')
        krpctest.remove_other_vessels()
        cls.conn = krpctest.connect(cls)
        cls.ir = cls.conn.infernal_robotics

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_rotatron(self):
        servo = self.ir.servo_with_name('Rotatron')
        self.assertEqual('Rotatron', servo.name)
        self.assertEqual(0, servo.position)
        self.assertEqual(-360, servo.min_config_position)
        self.assertEqual(360, servo.max_config_position)
        self.assertEqual(-360, servo.min_position)
        self.assertEqual(360, servo.max_position)
        self.assertClose(50, servo.config_speed)
        self.assertClose(1, servo.speed)
        self.assertClose(0, servo.current_speed)
        self.assertClose(4, servo.acceleration)
        self.assertFalse(servo.is_moving)
        self.assertFalse(servo.is_free_moving)
        self.assertFalse(servo.is_locked)
        self.assertFalse(servo.is_axis_inverted)

    def test_rail(self):
        servo = self.ir.servo_with_name('Rail')
        self.assertEqual('Rail', servo.name)
        self.assertEqual(0, servo.position)
        self.assertEqual(0, servo.min_config_position)
        self.assertEqual(2, servo.max_config_position)
        self.assertEqual(0, servo.min_position)
        self.assertEqual(2, servo.max_position)
        self.assertClose(0.3, servo.config_speed)
        self.assertClose(4, servo.speed)
        self.assertClose(0, servo.current_speed)
        self.assertClose(4, servo.acceleration)
        self.assertFalse(servo.is_moving)
        self.assertFalse(servo.is_free_moving)
        self.assertFalse(servo.is_locked)
        self.assertFalse(servo.is_axis_inverted)

    def test_move(self):
        servo = self.ir.servo_with_name('Rail')
        self.assertFalse(servo.is_moving)
        servo.move_right()
        time.sleep(0.1)
        while servo.is_moving:
            time.sleep(0.1)
        self.assertFalse(servo.is_moving)
        servo.move_left()
        time.sleep(0.1)
        while servo.is_moving:
            time.sleep(0.1)
        self.assertFalse(servo.is_moving)

    def test_stop(self):
        servo = self.ir.servo_with_name('Rail')
        self.assertFalse(servo.is_moving)
        servo.move_right()
        time.sleep(0.1)
        self.assertTrue(servo.is_moving)
        servo.stop()
        time.sleep(0.1)
        self.assertFalse(servo.is_moving)
        time.sleep(0.1)
        servo.move_left()
        while servo.is_moving:
            time.sleep(0.1)
        self.assertFalse(servo.is_moving)

if __name__ == '__main__':
    unittest.main()
