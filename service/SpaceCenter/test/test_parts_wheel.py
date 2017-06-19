import unittest
import krpc.error
import krpctest


class TestPartsWheel(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        if cls.connect().space_center.active_vessel.name != 'PartsWheel':
            cls.launch_vessel_from_vab('PartsWheel')
            cls.remove_other_vessels()
        vessel = cls.connect().space_center.active_vessel
        cls.parts = vessel.parts
        cls.wheels = cls.parts.wheels
        cls.deployable_wheel = cls.parts.with_title(
            'LY-60 Large Landing Gear')[0].wheel
        cls.fixed_wheel = cls.parts.with_title(
            'LY-01 Fixed Landing Gear')[0].wheel
        cls.free_wheel = cls.parts.with_title(
            'LY-05 Steerable Landing Gear')[0].wheel
        cls.powered_wheel = cls.parts.with_title(
            'RoveMax Model M1')[0].wheel
        cls.suspension_wheel = cls.parts.with_title(
            'TR-2L Ruggedized Vehicular Wheel')[0].wheel
        cls.state = cls.connect().space_center.WheelState
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
        self.assertRaises(krpc.error.RPCError, getattr, wheel, 'brakes')

    def test_friction_control(self):
        wheel = self.fixed_wheel
        self.assertTrue(wheel.auto_friction_control)
        wheel.auto_friction_control = False
        self.wait()
        self.assertFalse(wheel.auto_friction_control)
        self.assertAlmostEqual(1, wheel.manual_friction_control, places=3)
        wheel.manual_friction_control = 1.2
        self.wait()
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
        self.assertRaises(krpc.error.RPCError,
                          getattr, wheel, 'motor_inverted')
        self.assertRaises(krpc.error.RPCError,
                          getattr, wheel, 'motor_state')
        self.assertRaises(krpc.error.RPCError,
                          getattr, wheel, 'motor_output')

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

    def test_unsteerable(self):
        wheel = self.fixed_wheel
        self.assertFalse(wheel.steerable)
        self.assertRaises(krpc.error.RPCError,
                          getattr, wheel, 'steering_enabled')
        self.assertRaises(krpc.error.RPCError,
                          getattr, wheel, 'steering_inverted')

    def test_suspension(self):
        wheel = self.suspension_wheel
        self.assertTrue(wheel.has_suspension)
        self.assertAlmostEquals(1.2, wheel.suspension_spring_strength)
        self.assertAlmostEquals(0.85, wheel.suspension_damper_strength)

    def test_no_suspension(self):
        # TODO: there are no wheel with no suspension to test!
        # wheel = self.fixed_wheel
        # self.assertFalse(wheel.has_suspension)
        # self.assertRaises(krpc.error.RPCError,
        #                   getattr, wheel, 'suspension_spring_strength')
        # self.assertRaises(krpc.error.RPCError,
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


class TestPartsWheelGrounded(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab('PartsWheel')
        cls.remove_other_vessels()
        vessel = cls.connect().space_center.active_vessel
        cls.parts = vessel.parts
        cls.wheel = cls.parts.with_title('LY-60 Large Landing Gear')[0].wheel

    def test_grounded(self):
        self.assertTrue(self.wheel.deployed)
        self.assertFalse(self.wheel.grounded)
        self.parts.launch_clamps[0].release()
        self.wait(1)
        self.assertTrue(self.wheel.grounded)


if __name__ == '__main__':
    unittest.main()
