import unittest
import krpctest


class TestPartsRobotic(krpctest.TestCase):
    # Breaking Ground stock robotic parts. The class is skipped if the expansion is
    # not installed (expansions cannot be installed by the test harness).
    expansions = ["Serenity"]

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("PartsRobotic")
        cls.remove_other_vessels()
        cls.vessel = cls.connect().space_center.active_vessel
        cls.parts = cls.vessel.parts

    def wait_converge(self, read, target, tol, timeout=60, message="servo"):
        """Wait until read() is within tol of target. Robust to the delay between
        commanding a servo and it starting to move (is_moving briefly reads False)."""
        self.wait_until(
            lambda: abs(read() - target) <= tol,
            timeout=timeout,
            message="%s to reach %g" % (message, target),
        )

    def test_expansions(self):
        self.assertIn("Serenity", self.connect().space_center.expansions)

    def test_hinge(self):
        hinges = self.parts.robotic_hinges
        self.assertGreater(len(hinges), 0)
        hinge = hinges[0]

        # Part identity and equality
        self.assertEqual(hinge, hinge.part.robotic_hinge)
        self.assertIsNone(hinge.part.robotic_piston)

        # Limits are readable and ordered
        self.assertLessEqual(hinge.min_angle, hinge.max_angle)

        # Configurable properties round-trip
        hinge.rate = 20
        self.assertAlmostEqual(20, hinge.rate, places=3)
        hinge.damping = 5
        self.assertAlmostEqual(5, hinge.damping, places=3)

        # Drive a 45 degree move and back home, checking the angle converges
        hinge.locked = False
        hinge.motor_engaged = True
        home = hinge.current_angle
        target = home + 45 if home + 45 <= hinge.max_angle else home - 45
        hinge.target_angle = target
        self.wait_converge(lambda: hinge.current_angle, target, 1, message="hinge")
        hinge.move_home()
        self.wait_converge(lambda: hinge.current_angle, home, 1, message="hinge home")
        # is_moving lags convergence slightly; wait for the servo to settle.
        self.wait_while(lambda: hinge.is_moving, timeout=10, message="hinge to settle")

        # Locking round-trips
        hinge.locked = True
        self.assertTrue(hinge.locked)
        hinge.locked = False
        self.assertFalse(hinge.locked)

    def test_rotation(self):
        rotations = self.parts.robotic_rotations
        self.assertGreater(len(rotations), 0)
        rotation = rotations[0]

        self.assertEqual(rotation, rotation.part.robotic_rotation)
        self.assertLessEqual(rotation.min_angle, rotation.max_angle)

        # allow_full_rotation round-trips
        original = rotation.allow_full_rotation
        rotation.allow_full_rotation = not original
        self.assertEqual(not original, rotation.allow_full_rotation)
        rotation.allow_full_rotation = original

        rotation.rate = 45
        self.assertAlmostEqual(45, rotation.rate, places=3)

        rotation.locked = False
        rotation.motor_engaged = True
        home = rotation.current_angle
        target = home + 45 if home + 45 <= rotation.max_angle else home - 45
        rotation.target_angle = target
        self.wait_converge(
            lambda: rotation.current_angle, target, 1, message="rotation"
        )
        rotation.move_home()
        self.wait_converge(
            lambda: rotation.current_angle, home, 1, message="rotation home"
        )
        self.wait_while(
            lambda: rotation.is_moving, timeout=10, message="rotation to settle"
        )

    def test_piston(self):
        pistons = self.parts.robotic_pistons
        self.assertGreater(len(pistons), 0)
        piston = pistons[0]

        self.assertEqual(piston, piston.part.robotic_piston)
        self.assertLessEqual(piston.min_extension, piston.max_extension)

        piston.rate = 1.0
        self.assertAlmostEqual(1.0, piston.rate, places=3)

        piston.locked = False
        piston.motor_engaged = True
        home = piston.current_extension
        piston.target_extension = piston.max_extension
        self.wait_converge(
            lambda: piston.current_extension,
            piston.max_extension,
            0.05,
            message="piston",
        )
        piston.move_home()
        self.wait_converge(
            lambda: piston.current_extension, home, 0.05, message="piston home"
        )
        self.wait_while(
            lambda: piston.is_moving, timeout=10, message="piston to settle"
        )

    def test_rotor(self):
        rotors = self.parts.robotic_rotors
        self.assertGreater(len(rotors), 0)
        rotor = rotors[0]

        self.assertEqual(rotor, rotor.part.robotic_rotor)

        # Static properties
        self.assertGreater(rotor.max_torque, 0)

        original_inverted = rotor.inverted
        rotor.inverted = not original_inverted
        self.assertEqual(not original_inverted, rotor.inverted)
        rotor.inverted = original_inverted

        # Spin up towards the target RPM. A rotor needs a non-zero torque limit and no
        # brake to actually turn.
        rotor.locked = False
        rotor.torque_limit = 100
        self.assertAlmostEqual(100, rotor.torque_limit, places=3)
        rotor.brake_percentage = 0
        self.assertAlmostEqual(0, rotor.brake_percentage, places=3)
        rotor.target_rpm = 50
        rotor.motor_engaged = True
        self.wait_until(
            lambda: rotor.current_rpm > 1, timeout=30, message="rotor to spin up"
        )

        # Bring it back to rest by driving the target RPM to zero (the motor actively
        # holds it there; a free rotor does not spin down on brake alone).
        rotor.target_rpm = 0
        self.wait_until(
            lambda: rotor.current_rpm < 1, timeout=30, message="rotor to stop"
        )

    def test_controller(self):
        controllers = self.parts.robotic_controllers
        self.assertGreater(len(controllers), 0)
        controller = controllers[0]

        self.assertEqual(controller, controller.part.robotic_controller)

        # Enabled round-trips
        controller.enabled = True
        self.assertTrue(controller.enabled)

        # A controller with no configured sequence is not playing
        self.assertFalse(controller.playing)
        self.assertGreaterEqual(controller.length, 0)
        self.assertGreater(controller.play_speed, 0)

        # Adding and clearing an axis for a hinge on the vessel
        hinge = self.parts.robotic_hinges[0]
        module = next(
            m for m in hinge.part.modules if m.name == "ModuleRoboticServoHinge"
        )
        self.assertTrue(controller.add_axis(module, "Target Angle"))
        self.assertTrue(controller.has_part(hinge.part))
        self.assertTrue(controller.add_key_frame(module, "Target Angle", 0, 0))
        self.assertGreater(len(controller.axes()), 0)
        self.assertTrue(controller.clear_axis(module, "Target Angle"))


if __name__ == "__main__":
    unittest.main()
