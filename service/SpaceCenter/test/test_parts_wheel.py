import unittest

from krpctools.game.ksp import TestCase


class TestPartsWheel(TestCase):
    """The craft stands on its legs. The wheels under test are mounted upside
    down on top, and never touch the ground."""

    @classmethod
    def setUpClass(cls):
        vessel = cls.launch("PartsWheelsLegs", editor="SPH")
        cls.parts = vessel.parts
        cls.wheels = cls.parts.wheels
        cls.deployable_wheel = cls.parts.with_name("GearMedium")[0].wheel
        cls.fixed_wheel = cls.parts.with_name("GearFixed")[0].wheel
        cls.free_wheel = cls.parts.with_name("GearFree")[0].wheel
        cls.powered_wheel = cls.parts.with_name("roverWheel1")[0].wheel
        cls.suspension_wheel = cls.parts.with_name("wheelMed")[0].wheel
        cls.state = cls.connect().space_center.DeployableState
        cls.motor_state = cls.connect().space_center.MotorState
        cls.control = vessel.control

    def test_properties(self):
        wheel = self.powered_wheel
        self.assertAlmostEqual(0.378, wheel.radius)
        self.assertFalse(wheel.grounded)

    def test_brakes(self):
        wheel = self.fixed_wheel
        self.assertTrue(wheel.has_brakes)
        self.assertEqual(100, wheel.brakes)

    def test_no_brakes(self):
        wheel = self.free_wheel
        self.assertFalse(wheel.has_brakes)
        self.assertRaises(RuntimeError, getattr, wheel, "brakes")

    def test_friction_control(self):
        wheel = self.fixed_wheel
        self.assertTrue(wheel.auto_friction_control)
        wheel.auto_friction_control = False
        self.assertFalse(wheel.auto_friction_control)
        self.assertAlmostEqual(2.0005, wheel.manual_friction_control, places=3)
        wheel.manual_friction_control = 1.2
        self.assertAlmostEqual(1.2, wheel.manual_friction_control, places=3)
        wheel.manual_friction_control = 1.0
        wheel.auto_friction_control = True

    def test_deployable(self):
        wheel = self.deployable_wheel
        self.assertTrue(wheel.deployable)
        self.assertEqual(self.state.deployed, wheel.state)
        self.assertTrue(wheel.deployed)
        self.assertTrue(self.control.wheels)
        wheel.deployed = False
        self.wait()
        self.assertEqual(self.state.retracting, wheel.state)
        self.assertFalse(wheel.deployed)
        while wheel.state == self.state.retracting:
            self.wait()
        self.assertEqual(self.state.retracted, wheel.state)
        self.assertFalse(wheel.deployed)
        self.assertFalse(self.control.wheels)
        wheel.deployed = True
        self.wait()
        self.assertEqual(self.state.deploying, wheel.state)
        self.assertFalse(wheel.deployed)
        while wheel.state == self.state.deploying:
            self.wait()
        self.assertEqual(self.state.deployed, wheel.state)
        self.assertTrue(wheel.deployed)
        self.assertTrue(self.control.wheels)

    def test_fixed_gear_is_deployed(self):
        wheel = self.fixed_wheel
        self.assertFalse(wheel.deployable)
        self.assertEqual(self.state.deployed, wheel.state)
        self.assertTrue(wheel.deployed)

    def test_control_deploy(self):
        self.assertTrue(self.control.wheels)
        self.control.wheels = False
        while self.control.wheels:
            self.wait()
        self.assertFalse(self.control.wheels)
        for wheel in self.wheels:
            if wheel.deployable:
                while wheel.state == self.state.retracting:
                    self.wait()
                self.assertFalse(wheel.deployed)
        self.control.wheels = True
        while not self.control.wheels:
            self.wait()
        self.assertTrue(self.control.wheels)
        for wheel in self.wheels:
            if wheel.deployable:
                while wheel.state == self.state.deploying:
                    self.wait()
                self.assertTrue(wheel.deployed)

    def test_powered(self):
        wheel = self.powered_wheel
        self.assertTrue(wheel.powered)
        self.assertFalse(wheel.motor_inverted)
        self.assertEqual(self.motor_state.idle, wheel.motor_state)
        self.assertEqual(0, wheel.motor_output)
        self.control.wheel_throttle = 1
        self.wait()
        self.assertLess(0.1, wheel.motor_output)
        self.wait()
        self.control.wheel_throttle = 0
        self.control.brakes = True
        self.wait(1)
        self.control.brakes = False

    def test_unpowered(self):
        wheel = self.free_wheel
        self.assertFalse(wheel.powered)
        self.assertRaises(RuntimeError, getattr, wheel, "motor_inverted")
        self.assertRaises(RuntimeError, getattr, wheel, "motor_state")
        self.assertRaises(RuntimeError, getattr, wheel, "motor_output")

    def test_traction_control(self):
        wheel = self.powered_wheel
        self.assertTrue(wheel.traction_control_enabled)
        self.assertEqual(1, wheel.traction_control)

        wheel.traction_control_enabled = False
        self.wait()
        self.assertFalse(wheel.traction_control_enabled)
        self.assertEqual(1, wheel.traction_control)

        wheel.traction_control_enabled = True
        wheel.traction_control = 3
        self.wait()
        self.assertTrue(wheel.traction_control_enabled)
        self.assertEqual(3, wheel.traction_control)

        wheel.traction_control = 1

    def test_manual_drive_limiter(self):
        wheel = self.powered_wheel
        wheel.traction_control_enabled = False
        self.wait()
        self.assertFalse(wheel.traction_control_enabled)
        self.assertAlmostEqual(100, wheel.drive_limiter, places=1)
        wheel.drive_limiter = 25
        self.wait()
        self.assertAlmostEqual(25, wheel.drive_limiter, places=1)
        wheel.drive_limiter = 100
        wheel.traction_control_enabled = True

    def test_steerable(self):
        wheel = self.powered_wheel
        self.assertTrue(wheel.steerable)
        self.assertTrue(wheel.steering_enabled)
        self.assertFalse(wheel.steering_inverted)

    def test_steering_angle_auto(self):
        wheel = self.powered_wheel
        # The setting round-trips through the live KSP field
        default = wheel.steering_angle_auto
        wheel.steering_angle_auto = not default
        self.wait()
        self.assertEqual(not default, wheel.steering_angle_auto)
        wheel.steering_angle_auto = default
        self.wait()
        self.assertEqual(default, wheel.steering_angle_auto)

    def test_unsteerable(self):
        wheel = self.fixed_wheel
        self.assertFalse(wheel.steerable)
        self.assertRaises(RuntimeError, getattr, wheel, "steering_enabled")
        self.assertRaises(RuntimeError, getattr, wheel, "steering_inverted")
        self.assertRaises(RuntimeError, getattr, wheel, "steering_angle_auto")

    def test_suspension(self):
        wheel = self.suspension_wheel
        self.assertTrue(wheel.has_suspension)
        self.assertAlmostEqual(1.20, wheel.suspension_spring_strength, places=2)
        self.assertAlmostEqual(0.85, wheel.suspension_damper_strength, places=2)
        self.assertAlmostEqual(0.125, wheel.suspension_travel, places=3)
        # Not grounded
        self.assertIsNone(wheel.suspension_spring)
        self.assertIsNone(wheel.suspension_damper)

    def test_impact_tolerance(self):
        wheel = self.deployable_wheel
        self.assertAlmostEqual(4800, wheel.impact_tolerance)
        self.assertAlmostEqual(1, wheel.explode_multiplier)
        self.assertAlmostEqual(300, self.suspension_wheel.impact_tolerance)

    def test_no_suspension(self):
        # Every stock wheel part includes a suspension module, so the negative
        # paths of has_suspension and the suspension properties cannot be
        # exercised with stock parts.
        # wheel = self.fixed_wheel
        # self.assertFalse(wheel.has_suspension)
        # self.assertRaises(RuntimeError,
        #                   getattr, wheel, 'suspension_spring_strength')
        # self.assertRaises(RuntimeError,
        #                   getattr, wheel, 'suspension_damper_strength')
        pass

    def test_breakable(self):
        wheel = self.free_wheel
        self.assertAlmostEqual(0, wheel.stress)
        self.assertAlmostEqual(2000, wheel.stress_tolerance)
        self.assertAlmostEqual(0, wheel.stress_percentage)
        self.assertAlmostEqual(0, wheel.deflection)
        self.assertAlmostEqual(0, wheel.slip)

    def test_broken(self):
        wheel = self.free_wheel
        self.assertFalse(wheel.broken)

    def test_repairable(self):
        wheel = self.free_wheel
        self.assertTrue(wheel.repairable)


class TestPartsWheelGrounded(TestCase):
    """With its legs retracted, the craft rests on its four RoveMax S2 wheels on
    the runway."""

    @classmethod
    def setUpClass(cls):
        cls.vessel = cls.launch("PartsWheelsLegs", editor="SPH")
        cls.body = cls.vessel.orbit.body
        cls.wheels = cls.vessel.parts.wheels
        cls.standing = [x.wheel for x in cls.vessel.parts.with_name("roverWheel2")]
        control = cls.vessel.control
        control.brakes = True
        control.legs = False
        while not all(wheel.grounded for wheel in cls.standing):
            cls.wait()
        cls.wait(5)

    def total_load(self):
        """The wheels' summed load, averaged over a second"""
        samples = []
        for _ in range(20):
            samples.append(sum(wheel.load for wheel in self.standing))
            self.wait(0.05)
        return sum(samples) / len(samples)

    def test_grounded(self):
        self.assertEqual(4, len(self.standing))
        for wheel in self.wheels:
            self.assertEqual(wheel in self.standing, wheel.grounded)
        for leg in self.vessel.parts.legs:
            self.assertFalse(leg.is_grounded)

    def test_load(self):
        # The wheels carry the weight, and a downward force at the center of
        # mass, in kN. The force is small, as the rear wheels' suspension bottoms
        # out at about 13 kN, and the wheel's body then carries the rest.
        for wheel in self.wheels:
            if wheel not in self.standing:
                self.assertAlmostEqual(0, wheel.load)
        weight = self.vessel.mass * self.body.surface_gravity / 1000
        self.assertAlmostEqual(weight, self.total_load(), delta=weight * 0.03)
        ref = self.vessel.surface_reference_frame
        force = self.vessel.parts.root.add_force((-5000, 0, 0), (0, 0, 0), ref)
        try:
            self.wait(3)
            self.assertAlmostEqual(
                weight + 5, self.total_load(), delta=(weight + 5) * 0.03
            )
        finally:
            force.remove()
            self.wait(3)

    def test_suspension(self):
        # The game sets the suspension from the load the wheel carries
        for wheel in self.wheels:
            if not wheel.has_suspension:
                continue
            if wheel in self.standing:
                suspension_spring = wheel.suspension_spring
                assert suspension_spring is not None
                self.assertGreater(suspension_spring, 0)
                suspension_damper = wheel.suspension_damper
                assert suspension_damper is not None
                self.assertGreater(suspension_damper, 0)
            else:
                self.assertIsNone(wheel.suspension_spring)
                self.assertIsNone(wheel.suspension_damper)

    def test_contact_position(self):
        # The standing wheels touch the flat runway at one altitude, below the
        # other wheels
        ref = self.body.reference_frame

        def altitude(wheel):
            return self.body.altitude_at_position(wheel.contact_position(ref), ref)

        runway = altitude(self.standing[0])
        for wheel in self.wheels:
            if wheel in self.standing:
                self.assertAlmostEqual(runway, altitude(wheel), delta=0.02)
            else:
                self.assertGreater(altitude(wheel), runway + 0.5)


if __name__ == "__main__":
    unittest.main()
