#!/usr/bin/env python2

import unittest
import testingtools
from testingtools import load_save
import krpc
import time

class TestControl(testingtools.TestCase):

    def setUp(self):
        load_save('flight')
        self.ksp = krpc.connect()
        self.vessel = self.ksp.space_center.active_vessel
        self.control = self.vessel.control
        self.orbital_flight = self.vessel.orbital_flight

    def test_basics(self):
        # Check bool properties
        for name in ['sas', 'rcs', 'gear', 'lights', 'brakes']:
            setattr(self.control, name, True)
            self.assertTrue(getattr(self.control, name))
            setattr(self.control, name, False)
            self.assertFalse(getattr(self.control, name))
        # Action groups
        for i in [1,2,3,4,5,6,7,8,9]:
            self.control.set_action_group(i, False)
            self.assertFalse(self.control.get_action_group(i))
            self.control.set_action_group(i, True)
            self.assertTrue(self.control.get_action_group(i))
            self.control.toggle_action_group(i)
            self.assertFalse(self.control.get_action_group(i))
        # Maneuver node editing
        node = self.control.add_node(self.ksp.space_center.ut + 60, 100, 0, 0)
        self.assertEquals(100, node.prograde)
        self.control.remove_nodes()

    def test_pitch_control(self):
        self.control.sas = False
        self.control.pitch = 1
        time.sleep(3)
        self.control.pitch = 0

        # Check flight is pitching in correct direction
        pitch = self.orbital_flight.pitch
        time.sleep(0.1)
        diff = self.orbital_flight.pitch - pitch
        self.assertGreater(diff, 0)

    def test_yaw_control(self):
        self.control.sas = False
        self.control.yaw = 1
        time.sleep(3)
        self.control.yaw = 0

        # Check flight is yawing in correct direction
        heading = self.orbital_flight.heading
        time.sleep(0.1)
        diff = self.orbital_flight.heading - heading
        self.assertGreater(diff, 0)

    def test_roll_control(self):
        self.control.sas = False
        self.control.roll = 0.1
        time.sleep(3)
        self.control.roll = 0

        self.assertClose(27, self.orbital_flight.pitch, error=1)
        self.assertClose(116, self.orbital_flight.heading, error=1)

        # Check flight is rolling in correct direction
        roll = self.orbital_flight.roll
        time.sleep(0.1)
        diff = self.orbital_flight.roll - roll
        self.assertGreater(diff, 0)

if __name__ == "__main__":
    unittest.main()
