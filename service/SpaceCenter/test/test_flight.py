import unittest
import math
import time
import krpctest
from krpctest.geometry import rad2deg, norm, normalize, dot, cross, vector

class TestFlight(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        krpctest.new_save()
        krpctest.remove_other_vessels()
        krpctest.set_circular_orbit('Kerbin', 100000)
        cls.conn = krpctest.connect()
        cls.vessel = cls.conn.space_center.active_vessel
        cls.conn.testing_tools.clear_rotation()
        cls.conn.testing_tools.apply_rotation(116, (0, 0, -1))
        cls.conn.testing_tools.apply_rotation(27, (-1, 0, 0))
        cls.conn.testing_tools.apply_rotation(40, (0, -1, 0))
        cls.far = cls.conn.space_center.far_available

    def test_equality(self):
        flight = self.vessel.flight(self.vessel.reference_frame)
        self.assertEqual(flight, self.vessel.flight(self.vessel.reference_frame))

    def check_properties_not_affected_by_reference_frame(self, flight):
        """ Verify flight properties that aren't affected by reference frames """
        self.assertClose(100000, flight.mean_altitude, error=10)
        self.assertClose(100000 - max(0, flight.elevation), flight.surface_altitude, error=10)
        self.assertClose(100000 - flight.elevation, flight.bedrock_altitude, error=10)

    def check_directions(self, flight):
        """ Check flight.direction against flight.heading and flight.pitch """
        direction = vector(flight.direction)
        up_direction = (1, 0, 0)
        north_direction = (0, 1, 0)
        self.assertClose(1, norm(direction))

        # Check vessel direction vector agrees with pitch angle
        pitch = 90 - rad2deg(math.acos(dot(up_direction, direction)))
        self.assertClose(pitch, flight.pitch, error=2)

        # Check vessel direction vector agrees with heading angle
        up_component = dot(direction, up_direction) * vector(up_direction)
        north_component = normalize(vector(direction) - up_component)
        self.assertCloseDegrees(
            rad2deg(math.acos(dot(north_component, north_direction))),
            flight.heading, error=1)

    def check_speeds(self, flight):
        """ Check flight.velocity agrees with flight.*_speed """
        up_direction = (0, 1, 0)
        velocity = vector(flight.velocity)
        vertical_speed = dot(velocity, up_direction)
        horizontal_speed = norm(velocity) - vertical_speed
        self.assertClose(norm(velocity), flight.speed, error=1)
        self.assertClose(horizontal_speed, flight.horizontal_speed, error=1)
        self.assertClose(vertical_speed, flight.vertical_speed, error=1)

    def check_orbital_vectors(self, flight):
        """ Check orbital direction vectors """
        prograde = vector(flight.prograde)
        retrograde = vector(flight.retrograde)
        normal = vector(flight.normal)
        anti_normal = vector(flight.anti_normal)
        radial = vector(flight.radial)
        anti_radial = vector(flight.anti_radial)
        self.assertClose(1, norm(prograde))
        self.assertClose(1, norm(retrograde))
        self.assertClose(1, norm(normal))
        self.assertClose(1, norm(anti_normal))
        self.assertClose(1, norm(radial))
        self.assertClose(1, norm(anti_radial))
        self.assertClose(prograde, [-x for x in retrograde], error=0.01)
        self.assertClose(radial, [-x for x in anti_radial], error=0.01)
        self.assertClose(normal, [-x for x in anti_normal], error=0.01)
        self.assertClose(0, dot(prograde, radial), error=0.01)
        self.assertClose(0, dot(prograde, normal), error=0.01)
        self.assertClose(0, dot(radial, normal), error=0.01)

    def test_flight_vessel_reference_frame(self):
        flight = self.vessel.flight(self.vessel.reference_frame)
        self.check_properties_not_affected_by_reference_frame(flight)

        self.assertClose((0, 0, 0), flight.velocity, error=0.5)
        self.assertClose(0, flight.speed, error=0.5)
        self.assertClose(0, flight.horizontal_speed, error=0.5)
        self.assertClose(0, flight.vertical_speed, error=0.5)
        self.assertCloseDegrees(0, flight.pitch, error=1)
        self.assertCloseDegrees(0, flight.heading, error=1)
        self.assertCloseDegrees(-90, flight.roll, error=1)

        self.check_directions(flight)
        self.check_speeds(flight)
        self.check_orbital_vectors(flight)

    def test_flight_vessel_orbital_reference_frame(self):
        ref = self.vessel.orbital_reference_frame
        flight = self.vessel.flight(ref)
        self.check_properties_not_affected_by_reference_frame(flight)

        self.assertClose((0, 0, 0), flight.velocity, error=0.5)
        self.assertClose(0, flight.speed, error=0.5)
        self.assertClose(0, flight.horizontal_speed, error=0.5)
        self.assertClose(0, flight.vertical_speed, error=0.5)

        self.check_speeds(flight)
        self.check_orbital_vectors(flight)

    def test_flight_vessel_surface_reference_frame(self):
        ref = self.vessel.surface_reference_frame
        flight = self.vessel.flight(ref)
        self.check_properties_not_affected_by_reference_frame(flight)

        self.assertClose((0, 0, 0), flight.velocity, error=0.5)
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
        direction = vector(cross(normalize(position), (0, 1, 0)))
        velocity = tuple(direction * speed)
        self.assertClose(velocity, flight.velocity, error=0.5)
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
        direction = vector(cross(normalize(position), (0, 1, 0)))
        velocity = direction * speed
        self.assertClose(velocity, flight.velocity, error=2)
        self.assertClose(speed, flight.speed, error=0.5)
        self.assertClose(speed, flight.horizontal_speed, error=0.5)
        self.assertClose(0, flight.vertical_speed, error=0.5)

        self.check_speeds(flight)
        self.check_orbital_vectors(flight)

    def test_latitude_and_longitude(self):
        # In a circular orbit, in anti-clockwise direction looking down on north pole of Kerbin.
        # Latitude should be 0 (we're the equator)
        # Longitude should be gradually increasing
        flight = self.vessel.flight()
        longitude = flight.longitude
        time.sleep(1)
        for _ in range(5):
            self.assertClose(0, flight.latitude, 0.001)
            self.assertLess(longitude, flight.longitude)
            longitude = flight.longitude
            time.sleep(1)

class TestFlightVerticalSpeed(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        krpctest.new_save()
        krpctest.remove_other_vessels()
        cls.conn = krpctest.connect(cls)
        cls.vessel = cls.conn.space_center.active_vessel
        cls.conn.testing_tools.remove_other_vessels()

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def check_speed(self, flight, ref):
        up = normalize(vector(self.vessel.position(ref))
                       - vector(self.vessel.orbit.body.position(ref)))
        v = self.vessel.velocity(ref)

        speed = norm(v)
        vertical_speed = dot(v, up)
        horizontal_speed = math.sqrt(speed*speed - vertical_speed*vertical_speed)

        self.assertClose(speed, flight.speed, error=0.5)
        self.assertClose(vertical_speed, flight.vertical_speed, error=0.5)
        self.assertClose(horizontal_speed, flight.horizontal_speed, error=0.5)

    def test_vertical_speed_positive(self):
        krpctest.set_orbit('Kerbin', 2000000, 0.2, 0, 0, 0, 0, 0)
        ref = self.vessel.orbit.body.reference_frame
        flight = self.vessel.flight(ref)
        self.assertGreater(flight.vertical_speed, 0)
        self.check_speed(flight, ref)

    def test_vertical_speed_negative(self):
        krpctest.set_orbit('Kerbin', 2000000, 0.2, 0, 0, 0, 2, 0)
        ref = self.vessel.orbit.body.reference_frame
        flight = self.vessel.flight(ref)
        self.assertGreater(0, flight.vertical_speed)
        self.check_speed(flight, ref)

    def test_surface_speed(self):
        krpctest.set_circular_orbit('Kerbin', 100000)
        ref = self.vessel.orbit.body.reference_frame
        flight = self.vessel.flight(ref)
        self.check_speed(flight, ref)
        self.assertClose(2042, flight.speed, error=0.1)
        self.assertClose(2042, flight.horizontal_speed, error=0.1)
        self.assertClose(0, flight.vertical_speed, error=0.1)

    def test_orbital_speed(self):
        krpctest.set_circular_orbit('Kerbin', 100000)
        ref = self.vessel.orbit.body.non_rotating_reference_frame
        flight = self.vessel.flight(ref)
        self.check_speed(flight, ref)
        self.assertClose(2246.1, flight.speed, error=0.1)
        self.assertClose(2246.1, flight.horizontal_speed, error=0.1)
        self.assertClose(0, flight.vertical_speed, error=0.1)

class TestFlightAtLaunchpad(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        krpctest.new_save()
        krpctest.remove_other_vessels()
        krpctest.launch_vessel_from_vab('Basic')
        cls.conn = krpctest.connect(cls)
        cls.vessel = cls.conn.space_center.active_vessel
        cls.conn.testing_tools.remove_other_vessels()
        cls.far = cls.conn.space_center.far_available

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_latitude_and_longitude(self):
        flight = self.vessel.flight()
        self.assertClose(-0.09694444, flight.latitude, 0.001)
        self.assertClose(-74.5575, flight.longitude, 0.001)

    def test_ferram_aerospace_research(self):
        if self.far:

            flight = self.vessel.flight()

            self.assertClose(1.188, flight.atmosphere_density, 0.001)
            self.assertClose(0, flight.drag, 0.5)
            self.assertClose(0, flight.dynamic_pressure)

            self.assertClose(0, flight.angle_of_attack, 2.5)
            self.assertClose(-27, flight.sideslip_angle, 2)
            self.assertClose(0, flight.stall_fraction)

            self.assertClose(0, flight.mach_number)
            self.assertClose(193, flight.terminal_velocity, 0.5)

            self.assertClose(0.103, flight.drag_coefficient, 0.01)
            self.assertClose(0, flight.lift_coefficient, 0.001)
            self.assertClose(0, flight.pitching_moment_coefficient, 0.001)
            self.assertClose(2246.8, flight.ballistic_coefficient, 50)
            self.assertClose(0, flight.thrust_specific_fuel_consumption)

            self.assertEqual('Nominal', flight.far_status)

    #def test_drag_coefficient(self):
    #    if not self.far:
    #        # Using stock aerodynamic model
    #        parts = {
    #            'mk1pod': {'n': 1, 'mass': 0.8, 'drag': 0.2},
    #            'fuelTank': {'n': 1, 'mass': 0.125, 'drag': 0.2},
    #            'batteryPack': {'n': 2, 'mass': 0.01, 'drag': 0.2},
    #            'solarPanels1': {'n': 3, 'mass': 0.02, 'drag': 0.25},
    #            'liquidEngine2': {'n': 1, 'mass': 1.5, 'drag': 0.2}
    #        }
    #        total_mass = sum(x['mass']*x['n'] for x in parts.values())
    #        mass_drag_products = sum(x['mass']*x['drag']*x['n'] for x in parts.values())
    #        drag_coefficient = mass_drag_products / total_mass
    #        self.assertClose(drag_coefficient, self.vessel.flight().drag_coefficient)

if __name__ == '__main__':
    unittest.main()
