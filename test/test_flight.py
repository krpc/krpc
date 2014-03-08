#!/usr/bin/env python2

import testingtools
from testingtools import load_save
import krpc
import time

class TestFlight(testingtools.TestCase):

    def setUp(self):
        load_save('flight')
        self.ksp = krpc.connect()

    def test_roll_pitch_yaw(self):
        self.assertBetween(57, 58, self.ksp.Flight.Pitch)
        self.assertBetween(224, 227, self.ksp.Flight.Heading)
        self.assertBetween(132, 134, self.ksp.Flight.Roll)

    def test_pitch_control(self):
        self.ksp.Control.SAS = False
        self.ksp.Control.Pitch = 1
        time.sleep(3)
        self.ksp.Control.Pitch = 0

        # Check vessel is pitching in correct direction
        pitch = self.ksp.Flight.Pitch
        time.sleep(0.1)
        diff = pitch - self.ksp.Flight.Pitch
        self.assertGreater(diff, 0)

    def test_yaw_control(self):
        self.ksp.Control.SAS = False
        self.ksp.Control.Yaw = 1
        time.sleep(3)
        self.ksp.Control.Yaw = 0

        # Check vessel is yawing in correct direction
        heading = self.ksp.Flight.Heading
        time.sleep(0.1)
        diff = heading - self.ksp.Flight.Heading
        self.assertGreater(diff, 0)

    def test_roll_control(self):
        self.ksp.Control.SAS = False
        self.ksp.Control.Roll = 1
        time.sleep(3)
        self.ksp.Control.Roll = 0

        self.assertBetween(57, 58, self.ksp.Flight.Pitch)
        self.assertBetween(224, 227, self.ksp.Flight.Heading)
        self.assertBetween(-90, -10, self.ksp.Flight.Roll)

        # Check vessel is rolling in correct direction
        roll = self.ksp.Flight.Roll
        time.sleep(0.1)
        diff = roll - self.ksp.Flight.Roll
        self.assertGreater(diff, 0)

if __name__ == "__main__":
    unittest.main()
