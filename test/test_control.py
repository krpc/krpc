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
        self.vessel = self.ksp.SpaceCenter.ActiveVessel
        self.control = self.vessel.Control
        self.flight = self.vessel.Flight

    def test_pitch_control(self):
        self.control.SAS = False
        self.control.Pitch = 1
        time.sleep(3)
        self.control.Pitch = 0

        # Check flight is pitching in correct direction
        pitch = self.flight.Pitch
        time.sleep(0.1)
        diff = pitch - self.flight.Pitch
        self.assertGreater(diff, 0)

    def test_yaw_control(self):
        self.control.SAS = False
        self.control.Yaw = 1
        time.sleep(3)
        self.control.Yaw = 0

        # Check flight is yawing in correct direction
        heading = self.flight.Heading
        time.sleep(0.1)
        diff = heading - self.flight.Heading
        self.assertGreater(diff, 0)

    def test_roll_control(self):
        self.control.SAS = False
        self.control.Roll = 0.1
        time.sleep(3)
        self.control.Roll = 0

        self.assertBetween(57, 58, self.flight.Pitch)
        self.assertBetween(224, 227, self.flight.Heading)
        self.assertBetween(80, 110, self.flight.Roll)

        # Check flight is rolling in correct direction
        roll = self.flight.Roll
        time.sleep(0.1)
        diff = roll - self.flight.Roll
        self.assertGreater(diff, 0)


if __name__ == "__main__":
    unittest.main()
