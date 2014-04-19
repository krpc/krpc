#!/usr/bin/env python2

import unittest
import testingtools
from testingtools import load_save
import krpc
import time
import numpy as np
import math
from mathtools import v3, rad2deg, normalize, to_vector

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
        self.ap.set_rotation(self.ref.Surface, pitch, yaw, roll)
        while self.ap.error > 0.1:
            time.sleep(0.25)
        self.vessel.control.sas = True
        self.ap.disengage()

        flight = self.vessel.surface_flight
        self.assertClose(pitch, flight.pitch, error=0.5)
        self.assertClose(yaw, flight.heading, error=0.5)
        self.assertClose(roll, flight.roll, error=0.5)

    def test_basic_direction(self):
        direction = to_vector(normalize([0,0,1]))
        roll = 25

        self.vessel.control.sas = False
        self.ap.set_direction(self.ref.Surface, direction) #, roll=roll)
        while self.ap.error > 0.1:
            time.sleep(0.25)
        self.vessel.control.sas = True
        self.ap.disengage()

        flight = self.vessel.surface_flight
        self.assertClose(v3(direction), v3(flight.direction), error=0.1)
        self.assertClose(roll, flight.roll, error=0.5)

    def check_direction(self, direction):
        self.vessel.control.sas = False
        self.ap.set_direction(self.ref.Surface, direction)
        while self.ap.error > 0.1:
            time.sleep(0.25)
        self.vessel.control.sas = True
        self.ap.disengage()

        flight = self.vessel.surface_flight
        self.assertClose(v3(direction), v3(flight.direction), error=0.1)

    def test_orbital_directions(self):
        self.check_direction(self.vessel.orbit.prograde)
        self.check_direction(self.vessel.orbit.retrograde)
        self.check_direction(self.vessel.orbit.normal)
        self.check_direction(self.vessel.orbit.normal_neg)
        self.check_direction(self.vessel.orbit.radial)
        self.check_direction(self.vessel.orbit.radial_neg)

if __name__ == "__main__":
    unittest.main()
