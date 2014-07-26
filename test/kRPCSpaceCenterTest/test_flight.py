import unittest
import testingtools
import krpc
import time
import math
from mathtools import rad2deg, norm, normalize, dot, cross, vector

class TestFlight(testingtools.TestCase):

    @classmethod
    def setUpClass(cls):
        testingtools.new_save()
        testingtools.set_circular_orbit('Kerbin', 100000)
        cls.conn = krpc.connect()
        cls.vessel = cls.conn.space_center.active_vessel
        cls.conn.testing_tools.clear_rotation()
        cls.conn.testing_tools.apply_rotation(116, (0,0,-1))
        cls.conn.testing_tools.apply_rotation(27, (-1,0,0))
        cls.conn.testing_tools.apply_rotation(40, (0,-1,0))

    def test_equality(self):
        flight = self.vessel.flight(self.vessel.reference_frame)
        self.assertEqual(flight, self.vessel.flight(self.vessel.reference_frame))

    def check_properties_not_affected_by_reference_frame(self, flight):
        """ Verify flight properties that aren't affected by reference frames """
        #self.assertClose(0, flight.g_force)
        self.assertClose(100000, flight.mean_altitude, error=1)
        self.assertClose(flight.mean_altitude - max(0, flight.elevation), flight.surface_altitude, error=1)
        self.assertClose(flight.mean_altitude - flight.elevation, flight.bedrock_altitude, error=1)

    def check_directions(self, flight):
        """ Check flight.direction against flight.heading and flight.pitch """
        direction       = vector(flight.direction)
        up_direction    = (1,0,0)
        north_direction = (0,1,0)
        self.assertClose(1, norm(direction))

        # Check vessel direction vector agrees with pitch angle
        pitch = 90 - rad2deg(math.acos(dot(up_direction, direction)))
        self.assertClose(flight.pitch, pitch, error=2)

        # Check vessel direction vector agrees with heading angle
        up_component = dot(direction, up_direction) * vector(up_direction)
        north_component = normalize(vector(direction) - up_component)
        self.assertCloseDegrees(flight.heading, rad2deg(math.acos(dot(north_component, north_direction))), error=1)

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
        self.assertCloseDegrees(0, flight.pitch, error=1)
        self.assertCloseDegrees(0, flight.heading, error=1)
        self.assertCloseDegrees(-90, flight.roll, error=1)

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
        # pitch, roll, yaw are meaningless as the reference frame
        # is in an arbitrary, but fixed, orientation

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
        self.assertClose(27, flight.pitch, error=1)
        self.assertClose(116, flight.heading, error=1)
        self.assertClose(40, flight.roll, error=1)

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
        self.assertClose(27, flight.pitch, error=1)
        self.assertClose(116, flight.heading, error=1)
        self.assertClose(40, flight.roll, error=1)

        self.check_directions(flight)
        self.check_speeds(flight)
        self.check_orbital_vectors(flight)

    def test_flight_orbit_body_reference_frame(self):
        ref = self.vessel.orbit.body.reference_frame
        flight = self.vessel.flight(ref)
        self.check_properties_not_affected_by_reference_frame(flight)

        speed = 2042.5
        self.assertClose(speed, norm(flight.velocity), error=0.5)
        position = self.vessel.position(ref)
        direction = vector(cross(normalize(position), (0,1,0)))
        velocity = direction * speed
        self.assertClose(velocity, flight.velocity, error=0.1)
        self.assertClose(speed, flight.speed, error=0.5)
        self.assertClose(speed, flight.horizontal_speed, error=0.5)
        self.assertClose(0, flight.vertical_speed, error=0.5)

        self.check_speeds(flight)
        self.check_orbital_vectors(flight)

    def test_flight_orbit_body_non_rotating_reference_frame(self):
        ref = self.vessel.orbit.body.non_rotating_reference_frame
        flight = self.vessel.flight(ref)
        self.check_properties_not_affected_by_reference_frame(flight)

        speed = 2246.1
        self.assertClose(speed, norm(flight.velocity), error=0.5)
        position = self.vessel.position(ref)
        direction = vector(cross(normalize(position), (0,1,0)))
        velocity = direction * speed
        self.assertClose(velocity, flight.velocity, error=2)
        self.assertClose(speed, flight.speed, error=0.5)
        self.assertClose(speed, flight.horizontal_speed, error=0.5)
        self.assertClose(0, flight.vertical_speed, error=0.5)

        self.check_speeds(flight)
        self.check_orbital_vectors(flight)

    def test_maneuver_flight(self):
        #TODO: implement
        pass

if __name__ == "__main__":
    unittest.main()
