import unittest
import krpctest


class TestServo(krpctest.TestCase):
    mods = ["InfernalRobotics"]

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("InfernalRobotics")
        cls.remove_other_vessels()
        cls.ir = cls.connect().infernal_robotics
        cls.vessel = cls.connect().space_center.active_vessel
        cls.Mode = cls.connect().infernal_robotics.ServoMode

    def _recenter(self, servo):
        servo.move_center()
        self.wait()
        while servo.is_moving:
            self.wait()

    def _roundtrip(self, obj, attr, value, places=3):
        """Set attr to value, assert it took, then restore the original."""
        original = getattr(obj, attr)
        try:
            setattr(obj, attr, value)
            if isinstance(value, float):
                self.assertAlmostEqual(value, getattr(obj, attr), places=places)
            else:
                self.assertEqual(value, getattr(obj, attr))
        finally:
            setattr(obj, attr, original)

    def test_rotatron(self):
        servo = self.ir.servo_with_name(self.vessel, "Rotatron - Basic")
        self.assertEqual("Rotatron - Basic", servo.name)
        self.assertEqual("IR.Rotatron.Basic.v3", servo.part.name)
        self.assertGreater(servo.uid, 0)
        self.assertEqual(self.Mode.servo, servo.mode)
        self.assertAlmostEqual(0, servo.position, places=3)
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
        # Members added to cover the wider IR-Next API.
        self.assertAlmostEqual(0, servo.target_position, places=3)
        self.assertAlmostEqual(0, servo.commanded_position, places=3)
        self.assertAlmostEqual(0, servo.default_position, places=3)
        self.assertAlmostEqual(4, servo.force_limit)
        self.assertGreater(servo.max_force, 0)
        self.assertGreater(servo.max_acceleration, 0)
        self.assertGreater(servo.max_speed, 0)
        self.assertGreater(servo.electric_charge_required, 0)
        self.assertTrue(servo.is_rotational)
        self.assertTrue(servo.is_servo)
        self.assertTrue(servo.can_have_limits)
        self.assertFalse(servo.is_limited)
        self.assertFalse(servo.has_spring)
        self.assertFalse(servo.is_running)
        self.assertEqual([-180, -90, 0, 90, 180], list(servo.preset_positions))

    def test_rail(self):
        servo = self.ir.servo_with_name(self.vessel, "Rail Gantry - Short")
        self.assertEqual("Rail Gantry - Short", servo.name)
        self.assertEqual("IR.RailGantry.Short", servo.part.name)
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
        self.assertFalse(servo.is_rotational)
        self.assertTrue(servo.is_servo)

    def test_setters(self):
        servo = self.ir.servo_with_name(self.vessel, "Rotatron - Basic")
        self._roundtrip(servo, "speed", 2.0)
        self._roundtrip(servo, "acceleration", 8.0)
        self._roundtrip(servo, "force_limit", 10.0)
        self._roundtrip(servo, "damping_power", 3.0)
        self._roundtrip(servo, "rotor_acceleration", 6.0)
        self._roundtrip(servo, "is_locked", True)
        self._roundtrip(servo, "is_axis_inverted", True)
        self._roundtrip(servo, "is_limited", True)

    def test_position_limits_distinct_from_config(self):
        # Regression: min_position/max_position are the tweak-menu limits and must be
        # independent of the fixed min_config_position/max_config_position.
        servo = self.ir.servo_with_name(self.vessel, "Rotatron - Basic")
        try:
            servo.min_position = -90
            servo.max_position = 90
            self.assertAlmostEqual(-90, servo.min_position)
            self.assertAlmostEqual(90, servo.max_position)
            self.assertEqual(-360, servo.min_config_position)
            self.assertEqual(360, servo.max_config_position)
        finally:
            servo.min_position = -360
            servo.max_position = 360

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
        # Restore the servo to its rest position so other tests are unaffected.
        self._recenter(servo)

    def test_move_to(self):
        servo = self.ir.servo_with_name(self.vessel, "Rail Gantry - Short")
        self.assertFalse(servo.is_moving)
        servo.move_to(0.5, 1.0)
        self.wait()
        while servo.is_moving:
            self.wait()
        self.assertAlmostEqual(0.5, servo.position, delta=0.05)
        self._recenter(servo)

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
        # Restore the servo to its rest position so other tests are unaffected.
        self._recenter(servo)

    def test_presets(self):
        servo = self.ir.servo_with_name(self.vessel, "Rotatron - Basic")
        original = list(servo.preset_positions)
        try:
            servo.add_preset(45)
            self.assertIn(45, servo.preset_positions)
            servo.sort_presets()
            positions = list(servo.preset_positions)
            self.assertEqual(sorted(positions), positions)
            servo.remove_preset_at(positions.index(45))
            self.assertNotIn(45, servo.preset_positions)
            self.assertCountEqual(original, servo.preset_positions)
        finally:
            # Rebuild the original preset list if the test left it altered.
            while len(servo.preset_positions) > 0:
                servo.remove_preset_at(0)
            for position in original:
                servo.add_preset(position)
            servo.sort_presets()


if __name__ == "__main__":
    unittest.main()
