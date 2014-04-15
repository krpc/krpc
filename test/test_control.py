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
        self.flight = self.vessel.flight

    def test_pitch_control(self):
        self.control.sas = False
        self.control.pitch = 1
        time.sleep(3)
        self.control.pitch = 0

        # Check flight is pitching in correct direction
        pitch = self.flight.pitch
        time.sleep(0.1)
        diff = pitch - self.flight.pitch
        self.assertGreater(diff, 0)

    def test_yaw_control(self):
        self.control.sas = False
        self.control.yaw = 1
        time.sleep(3)
        self.control.yaw = 0

        # Check flight is yawing in correct direction
        heading = self.flight.heading
        time.sleep(0.1)
        diff = self.flight.heading - heading
        self.assertGreater(diff, 0)

    def test_roll_control(self):
        self.control.sas = False
        self.control.roll = 0.1
        time.sleep(3)
        self.control.roll = 0

        self.assertClose(23, self.flight.pitch, error=1)
        self.assertClose(115, self.flight.heading, error=1)

        # Check flight is rolling in correct direction
        roll = self.flight.roll
        time.sleep(0.1)
        diff = roll - self.flight.roll
        self.assertGreater(diff, 0)

if __name__ == "__main__":
    unittest.main()
