import unittest
import testingtools
from testingtools import load_save
import krpc
import time
import math
from mathtools import rad2deg, norm, dot, vector

class TestFlight(testingtools.TestCase):

    @classmethod
    def setUpClass(cls):
        load_save('flight')
        cls.conn = krpc.connect()
        cls.ref = cls.conn.space_center.ReferenceFrame
        cls.vessel = cls.conn.space_center.active_vessel
        cls.surface_flight = cls.vessel.flight(cls.ref.surface)
        cls.orbital_flight = cls.vessel.flight(cls.ref.orbital)

    def test_equality(self):
        self.assertEqual(self.vessel.flight(self.ref.surface), self.surface_flight)
        self.assertEqual(self.vessel.flight(self.ref.orbital), self.orbital_flight)

    def check_orbital_vectors(self, flight):
        # Check orbital direction vectors
        prograde    = vector(flight.prograde)
        retrograde  = vector(flight.retrograde)
        normal      = vector(flight.normal)
        normal_neg  = vector(flight.normal_neg)
        radial      = vector(flight.radial)
        radial_neg  = vector(flight.radial_neg)
        self.assertClose(1, norm(prograde))
        self.assertClose(1, norm(retrograde))
        self.assertClose(1, norm(normal))
        self.assertClose(1, norm(normal_neg))
        self.assertClose(1, norm(radial))
        self.assertClose(1, norm(radial_neg))
        self.assertClose(prograde, [-x for x in retrograde], error=0.01)
        self.assertClose(radial, [-x for x in radial_neg], error=0.01)
        self.assertClose(normal, [-x for x in normal_neg], error=0.01)
        self.assertClose(0, dot(prograde, radial), error=0.01)
        self.assertClose(0, dot(prograde, normal), error=0.01)
        self.assertClose(0, dot(radial, normal), error=0.01)

    def test_orbital_flight(self):
        self.assertClose(0, self.orbital_flight.g_force)
        self.assertClose(100000, self.orbital_flight.altitude, error=10)
        self.assertClose(100940, self.orbital_flight.true_altitude, error=20)
        self.assertClose([-2246.1, 0, 0], vector(self.orbital_flight.velocity), error=0.5)
        self.assertClose(2246.1, self.orbital_flight.speed, error=0.5)
        self.assertClose(2246.1, self.orbital_flight.horizontal_speed, error=0.5)
        self.assertClose(0, self.orbital_flight.vertical_speed, error=0.5)

        self.assertClose(27, self.orbital_flight.pitch, error=1)
        self.assertClose(116, self.orbital_flight.heading, error=1)
        self.assertClose(39, self.orbital_flight.roll, error=1)

        self.check_directions(self.orbital_flight)
        self.check_speeds(self.orbital_flight)
        self.check_orbital_vectors(self.orbital_flight)

    def test_surface_flight(self):
        self.assertClose(0, self.orbital_flight.g_force)
        self.assertClose(100000, self.surface_flight.altitude, error=10)
        self.assertClose(100940, self.surface_flight.true_altitude, error=20)
        self.assertClose([-2042.5, 0, 0], vector(self.surface_flight.velocity), error=0.5)
        self.assertClose(2042.5, self.surface_flight.speed, error=0.5)
        self.assertClose(2042.5, self.surface_flight.horizontal_speed, error=0.5)
        self.assertClose(0, self.surface_flight.vertical_speed, error=0.5)

        self.assertClose(27, self.surface_flight.pitch, error=1)
        self.assertClose(116, self.surface_flight.heading, error=1)
        self.assertClose(39, self.surface_flight.roll, error=1)

        self.check_directions(self.surface_flight)
        self.check_speeds(self.surface_flight)
        self.check_orbital_vectors(self.surface_flight)

    def check_directions(self, flight):
        direction       = vector(flight.direction)
        up_direction    = vector(flight.up_direction)
        north_direction = vector(flight.north_direction)
        self.assertClose(1, norm(direction))
        self.assertClose(1, norm(up_direction))
        self.assertClose(1, norm(north_direction))
        self.assertClose(0, dot(up_direction, north_direction))

        # Check vessel direction vector agrees with pitch angle
        pitch = 90 - rad2deg(math.acos(dot(up_direction, direction)))
        self.assertClose(flight.pitch, pitch, error=2)

        # Check vessel direction vector agrees with heading angle
        up_component = dot(direction, up_direction) * vector(up_direction)
        north_component = vector(direction) - up_component
        north_component = north_component / norm(north_component)
        self.assertClose(flight.heading, rad2deg(math.acos(dot(north_component, north_direction))), error=1)

        # Check vessel directions agree with orbital directions
        # (we are in a 0 degree inclined orbit, so they should do)
        self.assertClose(1, dot(up_direction, vector(flight.radial)))
        self.assertClose(1, dot(north_direction, vector(flight.normal)))

    def check_speeds(self, flight):
        up_direction = vector(flight.up_direction)
        velocity = vector(flight.velocity)
        vertical_speed = dot(velocity, up_direction)
        horizontal_speed = norm(velocity) - vertical_speed
        self.assertClose(norm(velocity), flight.speed, error=1)
        self.assertClose(horizontal_speed, flight.horizontal_speed, error=1)
        self.assertClose(vertical_speed, flight.vertical_speed, error=1)

if __name__ == "__main__":
    unittest.main()
