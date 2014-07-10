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
        cls.vessel = cls.conn.space_center.active_vessel

    def test_equality(self):
        flight = self.vessel.flight(self.vessel.reference_frame)
        self.assertEqual(flight, self.vessel.flight(self.vessel.reference_frame))

    def check_properties_not_affected_by_reference_frame(self, flight):
        """ Verify flight properties that aren't affected by reference frames """
        #self.assertClose(0, flight.g_force)
        self.assertClose(100000, flight.mean_altitude, error=50)
        self.assertClose(100000, flight.surface_altitude, error=50)
        self.assertClose(100930, flight.bedrock_altitude, error=100)
        self.assertClose(930, flight.elevation, error=100)
        self.assertClose(flight.elevation, flight.bedrock_altitude - flight.mean_altitude, error=5)

    def check_directions(self, flight, check_against_orbital=True):
        """ Check flight.direction against flight.heading and flight.pitch
            If check_against_orbital is True, check flight.direction against flight.radial and flight.normal """
        direction       = vector(flight.direction)
        up_direction    = (1,0,0)
        north_direction = (0,1,0)
        self.assertClose(1, norm(direction))

        # Check vessel direction vector agrees with pitch angle
        pitch = 90 - rad2deg(math.acos(dot(up_direction, direction)))
        self.assertClose(flight.pitch, pitch, error=2)

        # Check vessel direction vector agrees with heading angle
        up_component = dot(direction, up_direction) * vector(up_direction)
        north_component = vector(direction) - up_component
        north_component = north_component / norm(north_component)
        self.assertCloseDegrees(flight.heading, rad2deg(math.acos(dot(north_component, north_direction))), error=1)

        #TODO: uncomment
        #if check_against_orbital == True:
        #    # Check vessel directions agree with orbital directions
        #    # (we are in a 0 degree inclined orbit, so they should do)
        #    self.assertClose(1, dot(up_direction, vector(flight.prograde)))
        #    self.assertClose(1, dot(north_direction, vector(flight.normal)))

    def check_speeds(self, flight):
        """ Check flight.velocity agrees with flight.*_speed """
        up_direction = (0,1,0)
        velocity = vector(flight.velocity)
        vertical_speed = dot(velocity, up_direction)
        horizontal_speed = norm(velocity) - vertical_speed
        self.assertClose(norm(velocity), flight.speed, error=1)
        self.assertClose(horizontal_speed, flight.horizontal_speed, error=1)
        self.assertClose(vertical_speed, flight.vertical_speed, error=1)

    def check_orbital_vectors(self, flight):
        """ Check orbital direction vectors """
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

    def test_flight_vessel_reference_frame(self):
        flight = self.vessel.flight(self.vessel.reference_frame)
        self.check_properties_not_affected_by_reference_frame(flight)

        self.assertClose((0,0,0), flight.velocity, error=0.5)
        self.assertClose(0, flight.speed, error=0.5)
        self.assertClose(0, flight.horizontal_speed, error=0.5)
        self.assertClose(0, flight.vertical_speed, error=0.5)
        #TODO: are these correct?
        self.assertCloseDegrees(0, flight.pitch, error=1)
        self.assertCloseDegrees(0, flight.heading, error=1)
        self.assertCloseDegrees(0, flight.roll, error=1)

        self.check_directions(flight)
        self.check_speeds(flight)
        self.check_orbital_vectors(flight)

    def test_flight_vessel_non_rotating_reference_frame(self):
        ref = self.vessel.non_rotating_reference_frame
        flight = self.vessel.flight(ref)
        self.check_properties_not_affected_by_reference_frame(flight)

        self.assertClose((0,0,0), flight.velocity, error=0.5)
        self.assertClose(0, flight.speed, error=0.5)
        self.assertClose(0, flight.horizontal_speed, error=0.5)
        self.assertClose(0, flight.vertical_speed, error=0.5)
        # TODO: compute what these should be
        #self.assertCloseDegrees(336.72, flight.pitch, error=1)
        #self.assertCloseDegrees(10.92, flight.heading, error=1)
        #self.assertCloseDegrees(117, flight.roll, error=1)

        self.check_directions(flight)
        self.check_speeds(flight)
        self.check_orbital_vectors(flight)

    def test_flight_vessel_orbital_reference_frame(self):
        ref = self.vessel.orbital_reference_frame
        flight = self.vessel.flight(ref)
        self.check_properties_not_affected_by_reference_frame(flight)

        self.assertClose((0,0,0), flight.velocity, error=0.5)
        self.assertClose(0, flight.speed, error=0.5)
        self.assertClose(0, flight.horizontal_speed, error=0.5)
        self.assertClose(0, flight.vertical_speed, error=0.5)
        # TODO: compute what these should be
        #self.assertClose(27, flight.pitch, error=1)
        #self.assertClose(116, flight.heading, error=1)
        #self.assertClose(39, flight.roll, error=1)

        self.check_directions(flight)
        self.check_speeds(flight)
        self.check_orbital_vectors(flight)

    def test_flight_vessel_surface_reference_frame(self):
        ref = self.vessel.surface_reference_frame
        flight = self.vessel.flight(ref)
        self.check_properties_not_affected_by_reference_frame(flight)

        self.assertClose((0,0,0), flight.velocity, error=0.5)
        self.assertClose(0, flight.speed, error=0.5)
        self.assertClose(0, flight.horizontal_speed, error=0.5)
        self.assertClose(0, flight.vertical_speed, error=0.5)
        #TODO: are these correct?
        self.assertClose(27, flight.pitch, error=1)
        self.assertClose(116, flight.heading, error=1)
        self.assertClose(39, flight.roll, error=1)

        self.check_directions(flight)
        self.check_speeds(flight)
        self.check_orbital_vectors(flight)

    def test_flight_orbit_body_reference_frame(self):
        ref = self.vessel.orbit.body.reference_frame
        flight = self.vessel.flight(ref)
        self.check_properties_not_affected_by_reference_frame(flight)

        #TODO: are these correct?
        v = [1953.8, 0, 595.3]
        self.assertClose(2042.5, norm(v), error=0.5)
        self.assertClose(v, flight.velocity, error=2)
        self.assertClose(2042.5, flight.speed, error=0.5)
        self.assertClose(2042.5, flight.horizontal_speed, error=0.5)
        self.assertClose(0, flight.vertical_speed, error=0.5)
        #TODO: are these correct?
        self.assertClose(63, flight.pitch, error=1)
        self.assertClose(207, flight.heading, error=1)
        self.assertClose(147, flight.roll, error=1)

        #TODO: fix this
        #self.check_directions(flight)
        self.check_speeds(flight)
        self.check_orbital_vectors(flight)

    def test_flight_orbit_body_non_rotating_reference_frame(self):
        ref = self.vessel.orbit.body.non_rotating_reference_frame
        flight = self.vessel.flight(ref)
        self.check_properties_not_affected_by_reference_frame(flight)

        #TODO: are these correct?
        v = [-717.2, 0, 2128.5]
        self.assertClose(2246.1, norm(v), error=0.5)
        self.assertClose(v, flight.velocity, error=2)
        self.assertClose(2246.1, flight.speed, error=0.5)
        self.assertClose(2246.1, flight.horizontal_speed, error=0.5)
        self.assertClose(0, flight.vertical_speed, error=0.5)
        #TODO: are these correct?
        self.assertClose(10.2, flight.pitch, error=1)
        self.assertClose(113.6, flight.heading, error=1)
        self.assertClose(31.5, flight.roll, error=1)

        self.check_directions(flight)
        self.check_speeds(flight)
        self.check_orbital_vectors(flight)

    def test_maneuver_flight(self):
        #TODO: implement
        pass

if __name__ == "__main__":
    unittest.main()
