import unittest
import testingtools
from testingtools import load_save
import krpc
import time
import math
from mathtools import rad2deg, norm, dot, vector

class TestFlight(testingtools.TestCase):

    def setUp(self):
        load_save('flight')
        self.ksp = krpc.connect()
        ref = self.ksp.space_center.ReferenceFrame
        self.vessel = self.ksp.space_center.active_vessel
        self.surface_flight = self.vessel.flight()
        self.surfacev_flight = self.vessel.flight(ref.surface_velocity)
        self.orbital_flight = self.vessel.flight(ref.orbital)

    def test_orbital_flight(self):
        self.assertClose(100000, self.orbital_flight.altitude, error=10)
        self.assertClose(100920, self.orbital_flight.true_altitude, error=20)
        self.assertClose([0, 0, 2246.1], vector(self.orbital_flight.velocity), error=0.5)
        self.assertClose(2246.1, self.orbital_flight.speed, error=0.5)
        self.assertClose(2246.1, self.orbital_flight.horizontal_speed, error=0.5)
        self.assertClose(0, self.orbital_flight.vertical_speed, error=0.5)

        self.assertClose(27, self.orbital_flight.pitch, error=1)
        self.assertClose(116 - 90, self.orbital_flight.heading, error=1)
        self.assertClose(39, self.orbital_flight.roll, error=1)

        self.check_directions(self.orbital_flight)
        self.check_speeds(self.orbital_flight)

    def test_surface_flight(self):
        self.assertClose(100000, self.surface_flight.altitude, error=10)
        self.assertClose(100920, self.surface_flight.true_altitude, error=20)
        self.assertClose([-2246.1, 0.0, 0.0], vector(self.surface_flight.velocity), error=0.5)
        self.assertClose(2246.1, self.surface_flight.speed, error=0.5)
        self.assertClose(2246.1, self.surface_flight.horizontal_speed, error=0.5)
        self.assertClose(0, self.surface_flight.vertical_speed, error=0.5)

        self.assertClose(27, self.surface_flight.pitch, error=1)
        self.assertClose(116, self.surface_flight.heading, error=1)
        self.assertClose(39, self.surface_flight.roll, error=1)

        self.check_directions(self.surface_flight)
        self.check_speeds(self.surface_flight)

    def test_surfacev_flight(self):
        self.assertClose(100000, self.surfacev_flight.altitude, error=10)
        self.assertClose(100920, self.surfacev_flight.true_altitude, error=20)
        self.assertClose([0,0,2042.5], vector(self.surfacev_flight.velocity), error=0.5)
        self.assertClose(2042.5, self.surfacev_flight.speed, error=0.5)
        self.assertClose(2042.5, self.surfacev_flight.horizontal_speed, error=0.5)
        self.assertClose(0, self.surfacev_flight.vertical_speed, error=0.5)

        self.assertClose(27, self.surfacev_flight.pitch, error=1)
        self.assertClose(116 - 90, self.surfacev_flight.heading, error=1)
        self.assertClose(39, self.surfacev_flight.roll, error=1)

        self.check_directions(self.surfacev_flight)
        self.check_speeds(self.surfacev_flight)

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
        self.assertClose(1, dot(up_direction, vector(self.vessel.orbit.radial)))
        self.assertClose(1, dot(north_direction, vector(self.vessel.orbit.normal)))

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
