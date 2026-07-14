import unittest
import krpctest


class TestNonActiveVessel(krpctest.TestCase):
    """Exercise the InfernalRobotics service against a loaded but *non-active* vessel.

    Infernal Robotics' own controller only ever tracks the active vessel, so kRPC enumerates
    and drives the servos of other loaded vessels directly. The test craft carries a command
    pod on a docking port; undocking it and switching to the pod leaves the servo vehicle
    loaded (and within physics range) but no longer active, which is the case being tested.
    """

    mods = ["InfernalRobotics"]

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("InfernalRobotics")
        cls.remove_other_vessels()
        sc = cls.connect().space_center
        cls.ir = cls.connect().infernal_robotics
        # Split the craft in two by undocking the command pod.
        port = next(
            p
            for p in sc.active_vessel.parts.docking_ports
            if p.state == sc.DockingPortState.docked
        )
        port.undock()
        cls.wait(1)
        # The undock leaves two loaded vessels: the servo vehicle (many parts) and the lone
        # command pod (one part). Switch to the pod so the servo vehicle becomes non-active.
        vessels = sc.vessels
        cls.vessel = max(vessels, key=lambda v: len(v.parts.all))
        pod = min(vessels, key=lambda v: len(v.parts.all))
        sc.active_vessel = pod
        for _ in range(300):
            if sc.active_vessel == pod:
                break
            cls.wait()
        if sc.active_vessel == cls.vessel:
            raise RuntimeError("failed to make the servo vehicle non-active")

    def test_vessel_is_non_active(self):
        active = self.connect().space_center.active_vessel
        self.assertNotEqual(active, self.vessel)

    def test_servo_groups(self):
        groups = self.ir.servo_groups(self.vessel)
        self.assertCountEqual(["Group1", "Group2"], [g.name for g in groups])

    def test_servo_group_contents(self):
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

    def test_servo_group_with_name(self):
        self.assertEqual(
            "Group1", self.ir.servo_group_with_name(self.vessel, "Group1").name
        )
        self.assertEqual(
            "Group2", self.ir.servo_group_with_name(self.vessel, "Group2").name
        )
        self.assertIsNone(self.ir.servo_group_with_name(self.vessel, "Group3"))

    def test_servo_with_name(self):
        self.assertEqual(
            "Rail Gantry - Short",
            self.ir.servo_with_name(self.vessel, "Rail Gantry - Short").name,
        )
        self.assertEqual(
            "Rotatron - Basic",
            self.ir.servo_with_name(self.vessel, "Rotatron - Basic").name,
        )
        self.assertIsNone(self.ir.servo_with_name(self.vessel, "Foo"))

    def test_group_servo_with_name(self):
        group = self.ir.servo_group_with_name(self.vessel, "Group1")
        self.assertEqual(
            "Rotatron - Basic", group.servo_with_name("Rotatron - Basic").name
        )
        self.assertIsNone(group.servo_with_name("Foo"))

    def test_servo_properties(self):
        servo = self.ir.servo_with_name(self.vessel, "Rotatron - Basic")
        self.assertEqual("Rotatron - Basic", servo.name)
        self.assertEqual("IR.Rotatron.Basic.v3", servo.part.name)
        self.assertEqual(-360, servo.min_config_position)
        self.assertEqual(360, servo.max_config_position)
        self.assertAlmostEqual(1, servo.speed)
        self.assertFalse(servo.is_moving)
        self.assertFalse(servo.is_locked)
        self.assertFalse(servo.is_axis_inverted)

    def test_move(self):
        servo = self.ir.servo_with_name(self.vessel, "Rail Gantry - Short")
        self.assertFalse(servo.is_moving)
        start = servo.position
        servo.move_right()
        self.wait()
        self.assertTrue(servo.is_moving)
        while servo.is_moving:
            self.wait()
        # The servo physically moved despite its vessel not being active.
        self.assertGreater(abs(servo.position - start), 0.1)
        servo.move_left()
        self.wait()
        while servo.is_moving:
            self.wait()
        self.assertFalse(servo.is_moving)
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
        servo.move_left()
        while servo.is_moving:
            self.wait()
        self._recenter(servo)

    def test_group_move(self):
        group = self.ir.servo_group_with_name(self.vessel, "Group2")
        servo = group.servo_with_name("Rail Gantry - Short")
        start = servo.position
        group.move_right()
        self.wait()
        self.assertTrue(servo.is_moving)
        while servo.is_moving:
            self.wait()
        self.assertGreater(abs(servo.position - start), 0.1)
        group.move_center()
        self.wait()
        while servo.is_moving:
            self.wait()

    def test_unsupported_group_members(self):
        # Group state that IR keeps only for the active vessel is unavailable here.
        group = self.ir.servo_group_with_name(self.vessel, "Group1")
        with self.assertRaises(RuntimeError):
            _ = group.speed
        with self.assertRaises(RuntimeError):
            group.speed = 1
        with self.assertRaises(RuntimeError):
            _ = group.forward_key
        with self.assertRaises(RuntimeError):
            _ = group.reverse_key
        with self.assertRaises(RuntimeError):
            _ = group.expanded
        with self.assertRaises(RuntimeError):
            group.move_next_preset()
        with self.assertRaises(RuntimeError):
            group.move_prev_preset()

    def _recenter(self, servo):
        servo.move_center()
        self.wait()
        while servo.is_moving:
            self.wait()


if __name__ == "__main__":
    unittest.main()
