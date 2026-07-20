import time
import unittest

import krpctest
from krpctest.geometry import normalize


class ControlMixin:
    def test_state(self):
        self.assertEqual(self.space_center.ControlState.full, self.control.state)
        self.assertEqual(self.space_center.ControlSource.kerbal, self.control.source)

    def test_special_action_groups(self):
        for name in ["rcs", "gear", "lights", "brakes", "abort"]:
            setattr(self.control, name, True)
            self.assertTrue(getattr(self.control, name))
            setattr(self.control, name, False)
            self.assertFalse(getattr(self.control, name))

    def test_numeric_action_groups(self):
        for i in range(10):
            self.control.set_action_group(i, False)
            self.assertFalse(self.control.get_action_group(i))
            self.control.set_action_group(i, True)
            self.assertTrue(self.control.get_action_group(i))
            self.control.toggle_action_group(i)
            self.assertFalse(self.control.get_action_group(i))
        self.assertRaises(ValueError, self.control.set_action_group, 11, False)
        self.assertRaises(ValueError, self.control.get_action_group, 11)
        self.assertRaises(ValueError, self.control.toggle_action_group, 11)

    def test_pitch_control(self):
        self.auto_pilot.sas = False
        self.connect().testing_tools.clear_rotation(self.vessel)
        self.wait_until(
            lambda: (
                max(
                    abs(v)
                    for v in self.vessel.angular_velocity(
                        self.vessel.orbital_reference_frame
                    )
                )
                < 0.01
            )
        )

        self.control.pitch = 1
        self.wait(1)
        self.control.pitch = 0

        # Check flight is pitching in correct direction
        pitch = self.orbital_flight.pitch
        self.wait()
        diff = pitch - self.orbital_flight.pitch
        self.assertGreater(diff, 0)

    def test_yaw_control(self):
        self.auto_pilot.sas = False
        self.connect().testing_tools.clear_rotation(self.vessel)
        self.wait_until(
            lambda: (
                max(
                    abs(v)
                    for v in self.vessel.angular_velocity(
                        self.vessel.orbital_reference_frame
                    )
                )
                < 0.01
            )
        )

        self.control.yaw = 1
        self.wait(1)
        self.control.yaw = 0

        # Check flight is yawing in correct direction. Heading wraps at 0/360,
        # so use the shortest signed angular difference rather than a raw
        # subtraction, which can flip sign across the wrap boundary.
        heading = self.orbital_flight.heading
        self.wait()
        diff = (heading - self.orbital_flight.heading + 180) % 360 - 180
        self.assertGreater(diff, 0)

    def test_roll_control(self):
        self.auto_pilot.sas = False
        self.connect().testing_tools.clear_rotation(self.vessel)
        self.wait_until(
            lambda: (
                max(
                    abs(v)
                    for v in self.vessel.angular_velocity(
                        self.vessel.orbital_reference_frame
                    )
                )
                < 0.01
            )
        )

        pitch = self.orbital_flight.pitch
        heading = self.orbital_flight.heading

        self.control.roll = 0.1
        self.wait(1)
        self.control.roll = 0.0

        self.assertAlmostEqual(pitch, self.orbital_flight.pitch, delta=1)
        self.assertDegreesAlmostEqual(heading, self.orbital_flight.heading, delta=1)

        # Check flight is rolling in correct direction
        roll = self.orbital_flight.roll
        self.wait()
        diff = self.orbital_flight.roll - roll
        self.assertGreater(diff, 0)

    def check_trim(self, axis):
        name = axis + "_trim"
        # Trim is only available for the active vessel
        if self.vessel != self.space_center.active_vessel:
            self.assertRaises(RuntimeError, getattr, self.control, name)
            self.assertRaises(RuntimeError, setattr, self.control, name, 0.5)
            return
        self.auto_pilot.sas = False
        try:
            setattr(self.control, name, 0.5)
            self.assertAlmostEqual(0.5, getattr(self.control, name), places=3)
            setattr(self.control, name, 2)  # clamped to 1
            self.assertAlmostEqual(1, getattr(self.control, name), places=3)
            setattr(self.control, name, 0)  # explicitly clears the axis
            self.assertEqual(0, getattr(self.control, name))
        finally:
            setattr(self.control, name, 0)

    def test_pitch_trim(self):
        self.check_trim("pitch")

    def test_yaw_trim(self):
        self.check_trim("yaw")

    def test_roll_trim(self):
        self.check_trim("roll")

    def test_sas_mode(self):
        sas_mode = self.space_center.SASMode
        active = self.vessel == self.space_center.active_vessel
        if active:
            self.vessel.control.add_node(self.space_center.ut + 60, 100, 0, 0)
        # Enable SAS and set a (non-default) mode with no delay between them,
        # then wait for the game autopilot to enable: the mode must survive
        # rather than being reset to StabilityAssist when the autopilot enables
        # (#236). SAS is switched off first so enabling it below is a real state
        # change, and the wait is essential - the mode is stored immediately but
        # was only discarded on the following update when the autopilot enabled.
        self.control.sas = False
        self.wait()
        self.control.sas = True
        self.control.sas_mode = sas_mode.prograde
        self.wait()
        self.assertTrue(self.control.sas)
        self.assertEqual(self.control.sas_mode, sas_mode.prograde)
        # SAS is now enabled, so further mode changes take effect immediately.
        if active:
            self.control.sas_mode = sas_mode.maneuver
            self.assertEqual(self.control.sas_mode, sas_mode.maneuver)
        self.control.sas_mode = sas_mode.retrograde
        self.assertEqual(self.control.sas_mode, sas_mode.retrograde)
        self.control.sas_mode = sas_mode.normal
        self.assertEqual(self.control.sas_mode, sas_mode.normal)
        self.control.sas_mode = sas_mode.anti_normal
        self.assertEqual(self.control.sas_mode, sas_mode.anti_normal)
        self.control.sas_mode = sas_mode.radial
        self.assertEqual(self.control.sas_mode, sas_mode.radial)
        self.control.sas_mode = sas_mode.anti_radial
        self.assertEqual(self.control.sas_mode, sas_mode.anti_radial)
        # No target set, so the target modes would be ignored here. They are
        # covered by test_sas_mode_with_target on the active vessel; a
        # non-active vessel cannot have a target, as targets are per-vessel
        # and only the active vessel's target can be set.
        self.control.sas_mode = sas_mode.stability_assist
        self.assertEqual(self.control.sas_mode, sas_mode.stability_assist)
        self.control.sas = False

    def test_sas_cannot_be_enabled_while_autopilot_engaged(self):
        # Control.sas and AutoPilot.sas are documented as equivalent, so both must
        # refuse to enable SAS while the auto-pilot is engaged. Control.sas used to
        # accept the write and let the pilot loop silently clear SAS a tick later.
        self.control.sas = False
        self.auto_pilot.reference_frame = self.vessel.surface_reference_frame
        self.auto_pilot.target_pitch_and_heading(0, 90)
        self.auto_pilot.engaged = True
        try:
            self.assertRaises(RuntimeError, setattr, self.control, "sas", True)
            self.assertRaises(RuntimeError, setattr, self.auto_pilot, "sas", True)
            # The refused writes left SAS off rather than enabling it for a tick.
            self.wait()
            self.assertFalse(self.control.sas)
            # Disabling SAS is always allowed, engaged or not.
            self.control.sas = False
            self.auto_pilot.sas = False
        finally:
            self.auto_pilot.engaged = False

    def test_speed_mode(self):
        speed_mode = self.space_center.SpeedMode
        self.control.speed_mode = speed_mode.orbit
        self.assertEqual(self.control.speed_mode, speed_mode.orbit)
        self.control.speed_mode = speed_mode.surface
        self.assertEqual(self.control.speed_mode, speed_mode.surface)
        # No target set, so the target mode would be ignored here. It is
        # covered by test_speed_mode_with_target on the active vessel.


class TestControlActiveVessel(krpctest.TestCase, ControlMixin):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("Basic")
        cls.remove_other_vessels()
        cls.set_circular_orbit("Kerbin", 100000)
        cls.space_center = cls.connect().space_center
        cls.vessel = cls.space_center.active_vessel
        cls.control = cls.vessel.control
        cls.auto_pilot = cls.vessel.auto_pilot
        cls.orbital_flight = cls.vessel.flight(cls.vessel.orbital_reference_frame)

    def test_equality(self):
        self.assertEqual(self.space_center.active_vessel.control, self.control)

    def test_sas_mode_with_target(self):
        sas_mode = self.space_center.SASMode
        self.space_center.target_body = self.space_center.bodies["Mun"]
        self.wait()
        self.control.sas = True
        self.wait()
        self.control.sas_mode = sas_mode.target
        self.assertEqual(self.control.sas_mode, sas_mode.target)
        self.control.sas_mode = sas_mode.anti_target
        self.assertEqual(self.control.sas_mode, sas_mode.anti_target)
        self.control.sas_mode = sas_mode.stability_assist
        self.control.sas = False
        self.space_center.clear_target()

    def test_speed_mode_with_target(self):
        speed_mode = self.space_center.SpeedMode
        self.space_center.target_body = self.space_center.bodies["Mun"]
        self.wait()
        self.control.speed_mode = speed_mode.target
        self.assertEqual(self.control.speed_mode, speed_mode.target)
        self.control.speed_mode = speed_mode.orbit
        self.assertEqual(self.control.speed_mode, speed_mode.orbit)
        self.space_center.clear_target()

    def test_maneuver_node_editing(self):
        node = self.control.add_node(self.space_center.ut + 60, 100, 0, 0)
        self.assertEqual(100, node.prograde)
        self.control.remove_nodes()

    def test_clear_on_disconnect(self):
        conn = self.connect(use_cached=False)
        control = conn.space_center.active_vessel.control
        control.pitch = 1
        control.yaw = 1
        control.roll = 1
        self.wait()
        conn.close()
        self.wait()
        self.assertEqual(self.control.pitch, 0)
        self.assertEqual(self.control.yaw, 0)
        self.assertEqual(self.control.roll, 0)


class TestControlNonActiveVessel(krpctest.TestCase, ControlMixin):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("Multi")
        cls.remove_other_vessels()
        cls.set_circular_orbit("Kerbin", 100000)
        cls.space_center = cls.connect().space_center
        # Decouple the two vessels
        next(iter(cls.space_center.active_vessel.parts.docking_ports)).undock()
        cls.vessel = next(
            v for v in cls.space_center.vessels if v != cls.space_center.active_vessel
        )
        # Switch vessels so the one with a kerbal is non-active
        tmp = cls.space_center.active_vessel
        cls.space_center.active_vessel = cls.vessel
        cls.vessel = tmp
        cls.control = cls.vessel.control
        cls.auto_pilot = cls.vessel.auto_pilot
        cls.orbital_flight = cls.vessel.flight(cls.vessel.orbital_reference_frame)

        # Move the vessels apart
        cls.control.rcs = True
        cls.control.forward = -1
        cls.wait(1)
        cls.control.rcs = False
        cls.control.forward = 0
        cls.wait(1)

    def test_equality(self):
        self.assertNotEqual(self.space_center.active_vessel.control, self.control)

    def test_maneuver_node_editing(self):
        self.assertRaises(
            RuntimeError, self.control.add_node, self.space_center.ut + 60, 100, 0, 0
        )


class TestControlStaging(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()

    def setUp(self):
        self.launch_vessel_from_vab("Staging")
        self.remove_other_vessels()
        self.set_circular_orbit("Kerbin", 100000)
        self.space_center = self.connect().space_center
        self.control = self.space_center.active_vessel.control

    def test_state(self):
        self.assertEqual(self.space_center.ControlState.full, self.control.state)
        self.assertEqual(self.space_center.ControlSource.kerbal, self.control.source)

    def test_stage_lock(self):
        self.control.stage_lock = False
        self.assertFalse(self.control.stage_lock)
        stage = self.control.current_stage
        self.control.activate_next_stage()
        stage -= 1
        self.assertEqual(stage, self.control.current_stage)
        self.control.stage_lock = True
        self.assertTrue(self.control.stage_lock)
        self.assertRaises(RuntimeError, self.control.activate_next_stage)
        self.assertEqual(stage, self.control.current_stage)
        self.control.stage_lock = False
        self.assertFalse(self.control.stage_lock)
        self.control.activate_next_stage()
        stage -= 1
        self.assertEqual(stage, self.control.current_stage)

    def test_staging(self):
        for i in reversed(range(12)):
            self.assertEqual(i, self.control.current_stage)
            self.wait(0.5)
            self.control.activate_next_stage()
        self.assertEqual(0, self.control.current_stage)


class TestControlRover(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("Rover")
        cls.remove_other_vessels()
        cls.space_center = cls.connect().space_center
        cls.vessel = cls.space_center.active_vessel
        cls.control = cls.vessel.control
        cls.flight = cls.vessel.flight(cls.vessel.orbit.body.reference_frame)
        # A freshly spawned rover creeps for a few seconds while its suspension
        # and terrain contact settle. Hold the brakes and wait for it to come to
        # rest so each test starts from a stationary baseline (with a timeout so
        # a rover that never settles doesn't block the suite forever).
        cls.control.brakes = True
        deadline = time.time() + 30
        while cls.flight.horizontal_speed > 0.02 and time.time() < deadline:
            cls.wait()

    def tearDown(self):
        # Bring the rover to rest between tests so a test that fails mid-drive
        # can't leave it under power and break the next test's stationary check
        # (with a timeout so a rover that never settles doesn't hang the suite).
        self.control.wheel_throttle = 0
        self.control.wheel_steering = 0
        self.control.wheel_steer = 0
        self.control.brakes = True
        deadline = time.time() + 10
        while self.flight.horizontal_speed > 0.02 and time.time() < deadline:
            self.wait()

    def test_state(self):
        self.assertEqual(self.space_center.ControlState.full, self.control.state)
        self.assertEqual(self.space_center.ControlSource.kerbal, self.control.source)

    def test_move_forward(self):
        self.control = self.space_center.active_vessel.control

        # Check the rover is stationary
        self.assertAlmostEqual(0, self.flight.horizontal_speed, delta=0.03)

        # Forward throttle for 1 second
        self.control.wheel_steer = 0
        self.control.wheel_throttle = 0.5
        self.control.brakes = False
        self.wait(1)

        # Check the rover is moving north
        self.assertGreater(self.flight.horizontal_speed, 0)
        direction = normalize(self.flight.velocity)
        # In the body's reference frame, y-axis points
        # from the CoM to the north pole
        # As we are close to the equator,
        # this is very close to the north direction
        self.assertAlmostEqual((0, 1, 0), direction, delta=0.2)

        # Apply brakes
        self.control.wheel_throttle = 0.0
        self.control.brakes = True

        # Wait until the rover has stopped
        while self.flight.horizontal_speed > 0.005:
            self.wait()

    def test_move_backward(self):
        self.control = self.space_center.active_vessel.control

        # Check the rover is stationary
        self.assertAlmostEqual(0, self.flight.horizontal_speed, delta=0.03)

        # Reverse throttle for 1 second
        self.control.wheel_steer = 0
        self.control.wheel_throttle = -0.5
        self.control.brakes = False
        self.wait(1)

        # Check the rover is moving south
        self.assertGreater(self.flight.horizontal_speed, 0)
        direction = normalize(self.flight.velocity)
        # In the body's reference frame, y-axis points
        # from the CoM to the north pole
        # As we are close to the equator,
        # this is very close to the north direction
        self.assertAlmostEqual((0, -1, 0), direction, delta=0.2)

        # Apply brakes
        self.control.wheel_throttle = 0.0
        self.control.brakes = True

        # Wait until the rover has stopped
        while self.flight.speed > 0.005:
            self.wait()

    def test_steer_left(self):
        self.control = self.space_center.active_vessel.control

        # Check the rover is stationary
        self.assertAlmostEqual(0, self.flight.horizontal_speed, delta=0.03)

        # Forward throttle and steering
        self.control.wheel_steering = -1
        self.control.wheel_throttle = 0.5
        self.control.brakes = False
        self.wait(1)

        # Check the rover is moving in an anti-clockwise circle. Its roll angle
        # sweeps negatively as it circles, but terrain jitter makes individual
        # ticks non-monotonic, so check the net wrap-safe change over a window.
        self.assertGreater(self.flight.horizontal_speed, 0)
        start_roll = self.flight.roll
        self.wait(0.5)
        diff = (self.flight.roll - start_roll + 180) % 360 - 180
        self.assertLess(diff, 0)

        # Apply brakes
        self.control.wheel_steering = 0
        self.control.wheel_throttle = 0.0
        self.control.brakes = True

        # Wait until the rover has stopped
        while self.flight.speed > 0.01:
            self.wait()

    def test_steer_right(self):
        self.control = self.space_center.active_vessel.control

        # Check the rover is stationary
        self.assertAlmostEqual(0, self.flight.horizontal_speed, delta=0.03)

        # Forward throttle and steering
        self.control.wheel_steering = 1
        self.control.wheel_throttle = 0.5
        self.control.brakes = False
        self.wait(0.5)

        # Check the rover is moving in a clockwise circle. Its roll angle sweeps
        # positively as it circles, but terrain jitter makes individual ticks
        # non-monotonic, so check the net wrap-safe change over a window.
        self.assertGreater(self.flight.horizontal_speed, 0)
        start_roll = self.flight.roll
        self.wait(0.5)
        diff = (self.flight.roll - start_roll + 180) % 360 - 180
        self.assertGreater(diff, 0)

        # Apply brakes
        self.control.wheel_steering = 0
        self.control.wheel_throttle = 0.0
        self.control.brakes = True

        # Wait until the rover has stopped
        while self.flight.horizontal_speed > 0.01:
            self.wait()


class TestControlProbe(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("Probe")
        cls.remove_other_vessels()
        cls.space_center = cls.connect().space_center
        cls.vessel = cls.space_center.active_vessel
        cls.control = cls.vessel.control

    def test_state(self):
        self.assertEqual(self.space_center.ControlState.full, self.control.state)
        self.assertEqual(self.space_center.ControlSource.probe, self.control.source)


class TestControlProbePartialControl(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("Probe")
        cls.remove_other_vessels()
        cls.set_circular_orbit("Jool", 20000000)
        cls.space_center = cls.connect().space_center
        cls.vessel = cls.space_center.active_vessel
        cls.control = cls.vessel.control

    def test_state(self):
        self.assertEqual(self.space_center.ControlState.partial, self.control.state)
        self.assertEqual(self.space_center.ControlSource.probe, self.control.source)


class TestActionGroupActions(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("ActionGroups")
        cls.remove_other_vessels()
        cls.set_circular_orbit("Kerbin", 100000)
        cls.space_center = cls.connect().space_center
        cls.control = cls.space_center.active_vessel.control

    def test_group_with_multiple_actions(self):
        # Group 1 has the Extend action of all three solar panels assigned to it
        actions = self.control.get_action_group_actions(1)
        self.assertEqual(3, len(actions))
        for action in actions:
            self.assertEqual("ModuleDeployableSolarPanel", action.module.name)
            self.assertEqual("ExtendAction", action.id)
            self.assertNotEqual("", action.name)
            self.assertEqual(action.part, action.module.part)
            self.assertNotEqual("", action.part.title)

    def test_group_with_single_action(self):
        # Group 2 has the light Toggle action assigned to it
        actions = self.control.get_action_group_actions(2)
        self.assertEqual(1, len(actions))
        action = actions[0]
        self.assertEqual("ModuleColorChanger", action.module.name)
        self.assertEqual("ToggleAction", action.id)
        self.assertNotEqual("", action.name)

    def test_empty_group(self):
        self.assertEqual([], self.control.get_action_group_actions(3))

    def test_invalid_group(self):
        self.assertRaises(ValueError, self.control.get_action_group_actions, 11)


class TestControlCustomAxes(krpctest.TestCase):
    """The custom axes are applied to the vessel they are set on, and read back
    the value that was set.

    Regression test for two bugs in the custom axis handling: the axis groups
    module was resolved from the active vessel rather than the vessel the input
    was set on, and the getter (which reads the flight control state) was
    decoupled from the setter (which wrote straight to the axis groups module),
    so a value that had been set never read back.
    """

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("Multi")
        cls.remove_other_vessels()
        cls.set_circular_orbit("Kerbin", 100000)
        cls.sc = cls.connect().space_center
        # Decouple into two vessels, one active and one not
        next(iter(cls.sc.active_vessel.parts.docking_ports)).undock()
        cls.wait(1)
        cls.active = cls.sc.active_vessel
        cls.other = next(v for v in cls.sc.vessels if v != cls.active)

    def setUp(self):
        for vessel in (self.active, self.other):
            for i in range(1, 5):
                setattr(vessel.control, "custom_axis0%d" % i, 0.0)
        self.wait(1)

    def test_round_trip(self):
        self.active.control.custom_axis01 = 0.6
        self.wait(1)
        self.assertAlmostEqual(0.6, self.active.control.custom_axis01, places=2)
        self.active.control.custom_axis01 = -0.3
        self.wait(1)
        self.assertAlmostEqual(-0.3, self.active.control.custom_axis01, places=2)

    def test_applied_to_correct_vessel(self):
        # Setting a custom axis on the non-active vessel must apply to that
        # vessel, and must not affect the active vessel.
        self.other.control.custom_axis01 = 0.5
        self.wait(1)
        self.assertAlmostEqual(0.5, self.other.control.custom_axis01, places=2)
        self.assertAlmostEqual(0.0, self.active.control.custom_axis01, places=2)

    def test_axes_independent(self):
        control = self.other.control
        control.custom_axis01 = 0.1
        control.custom_axis02 = 0.2
        control.custom_axis03 = 0.3
        control.custom_axis04 = 0.4
        self.wait(1)
        self.assertAlmostEqual(0.1, control.custom_axis01, places=2)
        self.assertAlmostEqual(0.2, control.custom_axis02, places=2)
        self.assertAlmostEqual(0.3, control.custom_axis03, places=2)
        self.assertAlmostEqual(0.4, control.custom_axis04, places=2)


if __name__ == "__main__":
    unittest.main()
