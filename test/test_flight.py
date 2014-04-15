#!/usr/bin/env python2

import unittest
import testingtools
from testingtools import load_save
import krpc
import time
import numpy as np
import math
from mathtools import v3, rad2deg

class TestFlight(testingtools.TestCase):

    def setUp(self):
        load_save('flight')
        self.ksp = krpc.connect()
        self.vessel = self.ksp.space_center.active_vessel
        self.flight = self.vessel.flight
        self.control = self.vessel.control

    def test_flight(self):

        # Check basic flight telemetry
        self.assertBetween(99900, 100100, self.flight.altitude)
        self.assertBetween(99900, 100100, self.flight.true_altitude)
        #self.assertBetween(2245, 2247, self.flight.orbital_speed)
        #self.assertBetween(2041, 2043, self.flight.surface_speed)
        #self.assertBetween(-1, 1, self.flight.vertical_surface_speed)
        #v3(self.flight.center_of_mass)

        # Check vessel direction vectors
        direction       = v3(self.flight.direction)
        up_direction    = v3(self.flight.up_direction)
        north_direction = v3(self.flight.north_direction)
        self.assertClose(1, np.linalg.norm(direction))
        self.assertClose(1, np.linalg.norm(up_direction))
        self.assertClose(1, np.linalg.norm(north_direction))
        self.assertClose(0, np.dot(up_direction, north_direction))

        # Check vessel direction vector agrees with pitch angle
        pitch = self.flight.pitch
        heading = self.flight.heading
        self.assertClose(pitch, 90 - rad2deg(math.acos(np.dot(direction, up_direction))), error=0.1)

        # Check vessel direction vector agrees with heading angle
        up_component = np.dot(direction, up_direction) * np.array(up_direction)
        north_component = np.array(direction) - up_component
        north_component = north_component / np.linalg.norm(north_component)
        self.assertClose(heading, rad2deg(math.acos(np.dot(north_component, north_direction))), error=0.1)

        # Check orbital direction vectors
        prograde    = v3(self.flight.prograde)
        retrograde  = v3(self.flight.retrograde)
        normal      = v3(self.flight.normal)
        normal_neg  = v3(self.flight.normal_neg)
        radial      = v3(self.flight.radial)
        radial_neg  = v3(self.flight.radial_neg)
        self.assertClose(1, np.linalg.norm(prograde))
        self.assertClose(1, np.linalg.norm(retrograde))
        self.assertClose(1, np.linalg.norm(normal))
        self.assertClose(1, np.linalg.norm(normal_neg))
        self.assertClose(1, np.linalg.norm(radial))
        self.assertClose(1, np.linalg.norm(radial_neg))
        self.assertClose(prograde, [-x for x in retrograde], error=0.001)
        self.assertClose(radial, [-x for x in radial_neg], error=0.001)
        self.assertClose(normal, [-x for x in normal_neg], error=0.001)
        self.assertClose(0, np.dot(prograde, radial), error=0.001)
        self.assertClose(0, np.dot(prograde, normal), error=0.001)
        self.assertClose(0, np.dot(radial, normal), error=0.001)

        # Check vessel directions agree with orbital directions
        # (we are in a 0 degree inclined orbit, so they should do)
        self.assertClose(1, np.dot(up_direction, radial))
        self.assertClose(1, np.dot(north_direction, normal))

    def test_roll_pitch_yaw(self):
        vessel = self.ksp.space_center.active_vessel
        self.assertClose(26, self.flight.pitch, error=1)
        self.assertClose(116, self.flight.heading, error=1)
        self.assertClose(39, self.flight.roll, error=1)

if __name__ == "__main__":
    unittest.main()
