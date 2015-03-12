import unittest
import testingtools
import krpc
import time
from mathtools import normalize

class TestControl(testingtools.TestCase):

    @classmethod
    def setUpClass(cls):
        testingtools.new_save()
        testingtools.launch_vessel_from_vab('Basic')
        testingtools.remove_other_vessels()
        testingtools.set_circular_orbit('Kerbin', 100000)
        cls.conn = krpc.connect()
        vessel = cls.conn.space_center.active_vessel
        cls.control = vessel.control
        cls.auto_pilot = vessel.auto_pilot
        cls.orbital_flight = vessel.flight(vessel.orbit.reference_frame)

    def test_equality(self):
        self.assertEqual(self.conn.space_center.active_vessel.control, self.control)

    def test_special_action_groups(self):
        for name in ['rcs', 'gear', 'lights', 'brakes', 'abort']:
            setattr(self.control, name, True)
            self.assertTrue(getattr(self.control, name))
            setattr(self.control, name, False)
            self.assertFalse(getattr(self.control, name))

    def test_numeric_action_groups(self):
        for i in [0,1,2,3,4,5,6,7,8,9]:
            self.control.set_action_group(i, False)
            self.assertFalse(self.control.get_action_group(i))
            self.control.set_action_group(i, True)
            self.assertTrue(self.control.get_action_group(i))
            self.control.toggle_action_group(i)
            self.assertFalse(self.control.get_action_group(i))
        self.assertRaises(krpc.client.RPCError, self.control.set_action_group, 11, False)
        self.assertRaises(krpc.client.RPCError, self.control.get_action_group, 11)
        self.assertRaises(krpc.client.RPCError, self.control.toggle_action_group, 11)

    def test_maneuver_node_editing(self):
        node = self.control.add_node(self.conn.space_center.ut + 60, 100, 0, 0)
        self.assertEquals(100, node.prograde)
        self.control.remove_nodes()

    def test_pitch_control(self):
        testingtools.set_circular_orbit('Kerbin', 100000)
        self.conn.testing_tools.clear_rotation()

        self.auto_pilot.sas = False
        self.control.pitch = 1
        time.sleep(1)
        self.control.pitch = 0

        # Check flight is pitching in correct direction
        pitch = self.orbital_flight.pitch
        time.sleep(0.1)
        diff = self.orbital_flight.pitch - pitch
        self.assertGreater(diff, 0)

    def test_yaw_control(self):
        testingtools.set_circular_orbit('Kerbin', 100000)
        self.conn.testing_tools.clear_rotation()

        self.auto_pilot.sas = False
        self.control.yaw = 1
        time.sleep(1)
        self.control.yaw = 0

        # Check flight is yawing in correct direction
        heading = self.orbital_flight.heading
        time.sleep(0.1)
        diff = self.orbital_flight.heading - heading
        self.assertGreater(diff, 0)

    def test_roll_control(self):
        testingtools.set_circular_orbit('Kerbin', 100000)
        self.conn.testing_tools.clear_rotation()

        pitch = self.orbital_flight.pitch
        heading = self.orbital_flight.heading

        self.auto_pilot.sas = False
        self.control.roll = 0.1
        time.sleep(1)
        self.control.roll = 0

        self.assertClose(pitch, self.orbital_flight.pitch, error=1)
        self.assertCloseDegrees(heading, self.orbital_flight.heading, error=1)

        # Check flight is rolling in correct direction
        roll = self.orbital_flight.roll
        time.sleep(0.1)
        diff = self.orbital_flight.roll - roll
        self.assertGreater(diff, 0)

class TestControlStaging(testingtools.TestCase):

    def test_staging(self):
        testingtools.launch_vessel_from_vab('Staging')
        testingtools.remove_other_vessels()
        testingtools.set_circular_orbit('Kerbin', 100000)
        self.conn = krpc.connect()
        self.control = self.conn.space_center.active_vessel.control
        for i in reversed(range(12)):
            self.assertEqual(i, self.control.current_stage)
            time.sleep(3)
            self.control.activate_next_stage()
        self.assertEqual(0, self.control.current_stage)

class TestControlRover(testingtools.TestCase):

    @classmethod
    def setUpClass(cls):
        testingtools.new_save()

    def setUp(self):
        testingtools.launch_vessel_from_vab('Rover')
        testingtools.remove_other_vessels()
        self.conn = krpc.connect()
        self.vessel = self.conn.space_center.active_vessel
        self.control = self.vessel.control
        self.flight = self.vessel.flight(self.vessel.orbit.body.reference_frame)

    def test_move_forward(self):
        self.control = self.conn.space_center.active_vessel.control

        # Check the rover is stationary
        self.assertClose(0, self.flight.horizontal_speed)

        # Forward throttle for 1 second
        self.control.wheel_steer = 0
        self.control.wheel_throttle = 0.5
        self.control.brakes = False
        time.sleep(1)

        # Check the rover is moving north
        self.assertGreater(self.flight.horizontal_speed, 0)
        direction = normalize(self.flight.velocity)
        # In the body's reference frame, y-axis points from CoM to north pole
        # As we are close to the equator, this is very close to the north direction
        self.assertClose((0,1,0), direction, 0.01)

        # Apply brakes
        self.control.wheel_throttle = 0
        self.control.brakes = True
        time.sleep(5)

        # Check the rover is stopped
        self.assertClose(self.flight.horizontal_speed, 0)

    def test_move_backward(self):
        self.control = self.conn.space_center.active_vessel.control

        # Check the rover is stationary
        self.assertClose(0, self.flight.horizontal_speed)

        # Reverse throttle for 1 second
        self.control.wheel_steer = 0
        self.control.wheel_throttle = -0.5
        self.control.brakes = False
        time.sleep(1)

        # Check the rover is moving south
        self.assertGreater(self.flight.horizontal_speed, 0)
        direction = normalize(self.flight.velocity)
        # In the body's reference frame, y-axis points from CoM to north pole
        # As we are close to the equator, this is very close to the north direction
        self.assertClose((0,-1,0), direction, 0.01)

        # Apply brakes
        self.control.wheel_throttle = 0
        self.control.brakes = True
        time.sleep(5)

        # Check the rover is stopped
        self.assertClose(self.flight.horizontal_speed, 0)

    def test_steer_left(self):
        self.control = self.conn.space_center.active_vessel.control

        # Check the rover is stationary
        self.assertClose(0, self.flight.horizontal_speed)

        # Forward throttle and steering
        self.control.wheel_steering = 1
        self.control.wheel_throttle = 0.5
        self.control.brakes = False
        time.sleep(1)

        # Check the rover is moving in an anti-clockwise circle
        self.assertGreater(self.flight.horizontal_speed, 0)
        prev_roll = self.flight.roll
        time.sleep(0.25)
        for i in range(5):
            roll = self.flight.roll
            self.assertGreater(roll, prev_roll)
            prev_roll = roll
            time.sleep(0.25)

    def test_steer_right(self):
        self.control = self.conn.space_center.active_vessel.control

        # Check the rover is stationary
        self.assertClose(0, self.flight.horizontal_speed)

        # Forward throttle and steering
        self.control.wheel_steering = -1
        self.control.wheel_throttle = 0.5
        self.control.brakes = False
        time.sleep(1)

        # Check the rover is moving in a clockwise circle
        self.assertGreater(self.flight.horizontal_speed, 0)
        prev_roll = self.flight.roll
        time.sleep(0.25)
        for i in range(5):
            roll = self.flight.roll
            self.assertLess(roll, prev_roll)
            prev_roll = roll
            time.sleep(0.25)

if __name__ == "__main__":
    unittest.main()
