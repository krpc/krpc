#!/usr/bin/env python2

import unittest
import testingtools
from testingtools import load_save
import krpc
import time
import math
from mathtools import vector, rad2deg, normalize, to_vector

class TestAutoPilot(testingtools.TestCase):

    def setUp(self):
        load_save('autopilot')
        self.ksp = krpc.connect()
        self.vessel = self.ksp.space_center.active_vessel
        self.ref = self.ksp.space_center.ReferenceFrame
        self.ap = self.vessel.auto_pilot

    def test_basic_rotation(self):
        pitch = 10
        yaw = 20
        roll = 30

        self.vessel.control.sas = False
        self.ap.set_rotation(pitch, yaw, roll)
        while self.ap.error > 0.1:
            time.sleep(0.25)
        self.vessel.control.sas = True
        self.ap.disengage()

        flight = self.vessel.flight()
        self.assertClose(pitch, flight.pitch, error=0.5)
        self.assertClose(yaw, flight.heading, error=0.5)
        self.assertClose(roll, flight.roll, error=0.5)

    def test_basic_direction(self):
        direction = to_vector(normalize([10,20,30]))
        roll = 42

        self.vessel.control.sas = False
        self.ap.set_direction(direction, roll=roll)
        while self.ap.error > 0.1:
            time.sleep(0.25)
        self.vessel.control.sas = True
        self.ap.disengage()

        flight = self.vessel.flight()
        self.assertClose(vector(direction), vector(flight.direction), error=0.1)
        self.assertClose(roll, flight.roll, error=0.5)

    def check_direction(self, direction, pitch, heading):
        self.vessel.control.sas = False
        self.ap.set_direction(direction)
        while self.ap.error > 0.1:
            time.sleep(0.25)
        self.vessel.control.sas = True
        self.ap.disengage()

        # Check resulting direction vector in vessel space
        flight = self.vessel.flight()
        self.assertClose(vector(direction), vector(flight.direction), error=0.1)

        # Check resulting pitch and heading in orbital space
        flight = self.vessel.flight(self.ksp.space_center.ReferenceFrame.orbital)
        self.assertClose(pitch, flight.pitch, error=0.5)
        if heading is not None:
            self.assertClose(heading, flight.heading, error=0.5)

    def test_orbital_directions(self):
        self.check_direction(self.vessel.orbit.prograde,   0, 0)
        self.check_direction(self.vessel.orbit.retrograde, 0, 180)
        self.check_direction(self.vessel.orbit.normal,     0, 270)
        self.check_direction(self.vessel.orbit.normal_neg, 0, 90)
        self.check_direction(self.vessel.orbit.radial,     90, None)
        self.check_direction(self.vessel.orbit.radial_neg, -90, None)

if __name__ == "__main__":
    unittest.main()
