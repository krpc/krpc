import unittest
import krpctest


class TestServoGroup(krpctest.TestCase):
    mods = ["InfernalRobotics"]

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("InfernalRobotics")
        cls.remove_other_vessels()
        cls.ir = cls.connect().infernal_robotics
        cls.vessel = cls.connect().space_center.active_vessel
        # Group state such as speed factor and expanded flag lives in IR's controller, which
        # only populates once it is ready; until then the active vessel's groups are served by
        # the synthesized (non-active) path. Wait so those members are controller-backed.
        for _ in range(300):
            if cls.ir.ready:
                break
            cls.wait()

    def _roundtrip(self, obj, attr, value, places=3):
        original = getattr(obj, attr)
        try:
            setattr(obj, attr, value)
            if isinstance(value, float):
                self.assertAlmostEqual(value, getattr(obj, attr), places=places)
            else:
                self.assertEqual(value, getattr(obj, attr))
        finally:
            setattr(obj, attr, original)

    def test_servo_group(self):
        group1 = self.ir.servo_group_with_name(self.vessel, "Group1")
        group2 = self.ir.servo_group_with_name(self.vessel, "Group2")
        self.assertEqual("Group1", group1.name)
        self.assertCountEqual(
            ["Joint Pivotron - Basic", "Rotatron - Basic", "Rotatron - Basic"],
            [x.name for x in group1.servos],
        )
        self.assertCountEqual(
            ["IR.Pivotron.Basic.v3", "IR.Rotatron.Basic.v3", "IR.Rotatron.Basic.v3"],
            [x.name for x in group1.parts],
        )
        self.assertEqual("Group2", group2.name)
        self.assertCountEqual(["Rail Gantry - Short"], [x.name for x in group2.servos])
        self.assertCountEqual(["IR.RailGantry.Short"], [x.name for x in group2.parts])

    def test_servo_with_name(self):
        group = self.ir.servo_group_with_name(self.vessel, "Group1")
        servo = group.servo_with_name("Rotatron - Basic")
        self.assertEqual(servo.name, "Rotatron - Basic")
        self.assertIsNone(group.servo_with_name("Foo"))

    def test_properties(self):
        group = self.ir.servo_group_with_name(self.vessel, "Group1")
        self.assertEqual(self.vessel, group.vessel)
        self.assertAlmostEqual(1, group.speed)
        self.assertEqual(0, group.moving_direction)
        self.assertFalse(group.expanded)
        self.assertFalse(group.advanced_mode)
        self.assertFalse(group.build_aid)
        self.assertFalse(group.ik_active)
        self.assertGreater(group.electric_charge_required, 0)

    def test_setters(self):
        group = self.ir.servo_group_with_name(self.vessel, "Group1")
        self._roundtrip(group, "speed", 2.0)
        self._roundtrip(group, "expanded", True)
        self._roundtrip(group, "advanced_mode", True)
        self._roundtrip(group, "build_aid", True)
        self._roundtrip(group, "ik_active", True)
        self._roundtrip(group, "forward_key", "n")
        self._roundtrip(group, "reverse_key", "m")
        self._roundtrip(group, "name", "Renamed")

    def test_move(self):
        group = self.ir.servo_group_with_name(self.vessel, "Group2")
        servo = group.servo_with_name("Rail Gantry - Short")
        start = servo.position
        group.move_right()
        self.wait()
        self.assertTrue(servo.is_moving)
        while servo.is_moving:
            self.wait()
        self.assertGreater(abs(servo.position - start), 0.1)
        group.move_left()
        self.wait()
        while servo.is_moving:
            self.wait()
        self._recenter(group)

    def test_stop(self):
        group = self.ir.servo_group_with_name(self.vessel, "Group2")
        servo = group.servo_with_name("Rail Gantry - Short")
        group.move_right()
        self.wait()
        self.assertTrue(servo.is_moving)
        group.stop()
        self.wait()
        self.assertFalse(servo.is_moving)
        self._recenter(group)

    def _recenter(self, group):
        group.move_center()
        self.wait()
        while any(s.is_moving for s in group.servos):
            self.wait()


if __name__ == "__main__":
    unittest.main()
