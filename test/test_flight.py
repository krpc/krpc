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
        self.orbital_flight = self.vessel.orbital_flight
        self.surface_flight = self.vessel.surface_flight

    def test_orbital_flight(self):

        self.assertClose(100000, self.orbital_flight.altitude, error=10)
        self.assertClose(100000, self.orbital_flight.true_altitude, error=10)
        #self.assertClose([-1013.6, 0.0, -2004.4], v3(self.orbital_flight.velocity), error=100)
        self.assertClose(2246.1, self.orbital_flight.speed, error=0.05)
        self.assertClose(2246.1, self.orbital_flight.horizontal_speed, error=0.5)
        self.assertClose(0, self.orbital_flight.vertical_speed, error=0.5)
        #self.assertClose([0,0,0], v3(self.orbital_flight.center_of_mass))

        self.assertClose(27, self.orbital_flight.pitch, error=1)
        self.assertClose(116, self.orbital_flight.heading, error=1)
        self.assertClose(39, self.orbital_flight.roll, error=1)

        self.check_directions(self.orbital_flight)

    def test_surface_flight(self):

        self.assertClose(100000, self.surface_flight.altitude, error=10)
        self.assertClose(100000, self.surface_flight.true_altitude, error=10)
        #self.assertClose([-1013.6, 0.0, -2004.4], v3(self.surface_flight.velocity), error=100)
        self.assertClose(2042.5, self.surface_flight.speed, error=0.05)
        self.assertClose(2042.5, self.surface_flight.horizontal_speed, error=0.5)
        self.assertClose(0, self.surface_flight.vertical_speed, error=0.5)
        #self.assertClose([0,0,0], v3(self.surface_flight.center_of_mass))

        self.assertClose(27, self.surface_flight.pitch, error=1)
        self.assertClose(116, self.surface_flight.heading, error=1)
        self.assertClose(39, self.surface_flight.roll, error=1)

        self.check_directions(self.surface_flight)

    def check_directions(self, flight):
        direction       = v3(flight.direction)
        up_direction    = v3(flight.up_direction)
        north_direction = v3(flight.north_direction)
        self.assertClose(1, np.linalg.norm(direction))
        self.assertClose(1, np.linalg.norm(up_direction))
        self.assertClose(1, np.linalg.norm(north_direction))
        self.assertClose(0, np.dot(up_direction, north_direction))

        # Check vessel direction vector agrees with pitch angle
        pitch = flight.pitch
        heading = flight.heading
        self.assertClose(pitch, 90 - rad2deg(math.acos(np.dot(direction, up_direction))), error=0.1)

        # Check vessel direction vector agrees with heading angle
        up_component = np.dot(direction, up_direction) * np.array(up_direction)
        north_component = np.array(direction) - up_component
        north_component = north_component / np.linalg.norm(north_component)
        self.assertClose(heading, rad2deg(math.acos(np.dot(north_component, north_direction))), error=0.1)

        # Check vessel directions agree with orbital directions
        # (we are in a 0 degree inclined orbit, so they should do)
        self.assertClose(1, np.dot(up_direction, v3(self.vessel.orbit.radial)))
        self.assertClose(1, np.dot(north_direction, v3(self.vessel.orbit.normal)))

if __name__ == "__main__":
    unittest.main()
