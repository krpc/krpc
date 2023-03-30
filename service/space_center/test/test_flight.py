import unittest
import math
import krpctest
from krpctest.geometry import rad2deg, norm, normalize, dot, cross, vector


class TestFlight(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.remove_other_vessels()
        cls.set_circular_orbit('Kerbin', 100000)
        cls.space_center = cls.connect().space_center
        cls.vessel = cls.space_center.active_vessel
        cls.connect().testing_tools.clear_rotation()
        cls.connect().testing_tools.apply_rotation(116, (0, 0, -1))
        cls.connect().testing_tools.apply_rotation(27, (-1, 0, 0))
        cls.connect().testing_tools.apply_rotation(40, (0, -1, 0))
        cls.far = cls.space_center.far_available

    def test_equality(self):
        flight = self.vessel.flight(self.vessel.reference_frame)
        self.assertEqual(
            flight, self.vessel.flight(self.vessel.reference_frame))

    def check_properties_not_affected_by_reference_frame(self, flight):
        """ Verify flight properties that aren't
            affected by reference frames """
        self.assertAlmostEqual(100000, flight.mean_altitude, delta=10)
        self.assertAlmostEqual(100000 - max(0, flight.elevation),
                               flight.surface_altitude, delta=10)
        self.assertAlmostEqual(100000 - flight.elevation,
                               flight.bedrock_altitude, delta=10)

    def check_directions(self, flight):
        """ Check flight.direction against flight.heading and flight.pitch """
        direction = vector(flight.direction)
        up_direction = (1, 0, 0)
        north_direction = (0, 1, 0)
        self.assertAlmostEqual(1, norm(direction))

        # Check vessel direction vector agrees with pitch angle
        pitch = 90 - rad2deg(math.acos(dot(up_direction, direction)))
        self.assertAlmostEqual(pitch, flight.pitch, delta=2)

        # Check vessel direction vector agrees with heading angle
        up_component = dot(direction, up_direction) * vector(up_direction)
        north_component = normalize(vector(direction) - up_component)
        self.assertDegreesAlmostEqual(
            rad2deg(math.acos(dot(north_component, north_direction))),
            flight.heading, delta=1)

    def check_speeds(self, flight):
        """ Check flight.velocity agrees with flight.*_speed """
        up_direction = (0, 1, 0)
        velocity = vector(flight.velocity)
        vertical_speed = dot(velocity, up_direction)
        horizontal_speed = norm(velocity) - vertical_speed
        self.assertAlmostEqual(norm(velocity), flight.speed, delta=1)
        self.assertAlmostEqual(horizontal_speed,
                               flight.horizontal_speed, delta=1)
        self.assertAlmostEqual(vertical_speed,
                               flight.vertical_speed, delta=1)

    def check_orbital_vectors(self, flight):
        """ Check orbital direction vectors """
        prograde = vector(flight.prograde)
        retrograde = vector(flight.retrograde)
        normal = vector(flight.normal)
        anti_normal = vector(flight.anti_normal)
        radial = vector(flight.radial)
        anti_radial = vector(flight.anti_radial)
        self.assertAlmostEqual(1, norm(prograde))
        self.assertAlmostEqual(1, norm(retrograde))
        self.assertAlmostEqual(1, norm(normal))
        self.assertAlmostEqual(1, norm(anti_normal))
        self.assertAlmostEqual(1, norm(radial))
        self.assertAlmostEqual(1, norm(anti_radial))
        self.assertAlmostEqual(
            tuple(prograde), [-x for x in retrograde], places=2)
        self.assertAlmostEqual(
            tuple(radial), [-x for x in anti_radial], places=2)
        self.assertAlmostEqual(
            tuple(normal), [-x for x in anti_normal], places=2)
        self.assertAlmostEqual(0, dot(prograde, radial), places=2)
        self.assertAlmostEqual(0, dot(prograde, normal), places=2)
        self.assertAlmostEqual(0, dot(radial, normal), places=2)

    def test_flight_vessel_reference_frame(self):
        flight = self.vessel.flight(self.vessel.reference_frame)
        self.check_properties_not_affected_by_reference_frame(flight)

        self.assertAlmostEqual((0, 0, 0), flight.velocity, delta=0.5)
        self.assertAlmostEqual(0, flight.speed, delta=0.5)
        self.assertAlmostEqual(0, flight.horizontal_speed, delta=0.5)
        self.assertAlmostEqual(0, flight.vertical_speed, delta=0.5)
        self.assertDegreesAlmostEqual(0, flight.pitch, delta=1)
        self.assertDegreesAlmostEqual(0, flight.heading, delta=1)
        self.assertDegreesAlmostEqual(-90, flight.roll, delta=1)

        self.check_directions(flight)
        self.check_speeds(flight)
        self.check_orbital_vectors(flight)

    def test_flight_vessel_orbital_reference_frame(self):
        ref = self.vessel.orbital_reference_frame
        flight = self.vessel.flight(ref)
        self.check_properties_not_affected_by_reference_frame(flight)

        self.assertAlmostEqual((0, 0, 0), flight.velocity, delta=0.5)
        self.assertAlmostEqual(0, flight.speed, delta=0.5)
        self.assertAlmostEqual(0, flight.horizontal_speed, delta=0.5)
        self.assertAlmostEqual(0, flight.vertical_speed, delta=0.5)

        self.check_speeds(flight)
        self.check_orbital_vectors(flight)

    def test_flight_vessel_surface_reference_frame(self):
        ref = self.vessel.surface_reference_frame
        flight = self.vessel.flight(ref)
        self.check_properties_not_affected_by_reference_frame(flight)

        self.assertAlmostEqual((0, 0, 0), flight.velocity, delta=0.5)
        self.assertAlmostEqual(0, flight.speed, delta=0.5)
        self.assertAlmostEqual(0, flight.horizontal_speed, delta=0.5)
        self.assertAlmostEqual(0, flight.vertical_speed, delta=0.5)
        self.assertAlmostEqual(27, flight.pitch, delta=1)
        self.assertAlmostEqual(116, flight.heading, delta=1)
        self.assertAlmostEqual(40, flight.roll, delta=1)

        self.check_directions(flight)
        self.check_speeds(flight)
        self.check_orbital_vectors(flight)

    def test_flight_orbit_body_reference_frame(self):
        ref = self.vessel.orbit.body.reference_frame
        flight = self.vessel.flight(ref)
        self.check_properties_not_affected_by_reference_frame(flight)

        speed = 2041.75
        self.assertAlmostEqual(speed, norm(flight.velocity), delta=0.5)
        position = self.vessel.position(ref)
        direction = vector(cross(normalize(position), (0, 1, 0)))
        velocity = tuple(direction * speed)
        self.assertAlmostEqual(velocity, flight.velocity, delta=0.5)
        self.assertAlmostEqual(speed, flight.speed, delta=0.5)
        self.assertAlmostEqual(speed, flight.horizontal_speed, delta=0.5)
        self.assertAlmostEqual(0, flight.vertical_speed, delta=0.5)

        self.check_speeds(flight)
        self.check_orbital_vectors(flight)

    def test_flight_orbit_body_non_rotating_reference_frame(self):
        ref = self.vessel.orbit.body.non_rotating_reference_frame
        flight = self.vessel.flight(ref)
        self.check_properties_not_affected_by_reference_frame(flight)

        speed = 2245.75
        self.assertAlmostEqual(speed, norm(flight.velocity), delta=0.5)
        position = self.vessel.position(ref)
        direction = vector(cross(normalize(position), (0, 1, 0)))
        velocity = direction * speed
        self.assertAlmostEqual(tuple(velocity), flight.velocity, delta=2)
        self.assertAlmostEqual(speed, flight.speed, delta=0.5)
        self.assertAlmostEqual(speed, flight.horizontal_speed, delta=0.5)
        self.assertAlmostEqual(0, flight.vertical_speed, delta=0.5)

        self.check_speeds(flight)
        self.check_orbital_vectors(flight)

    def test_latitude_and_longitude(self):
        # In a circular orbit, in anti-clockwise direction looking down on the
        # north pole of Kerbin.
        # Latitude should be 0 (we're at the equator)
        # Longitude should be gradually increasing
        flight = self.vessel.flight()
        longitude = flight.longitude
        self.wait()
        for _ in range(5):
            self.assertAlmostEqual(0, flight.latitude, places=3)
            self.assertLess(longitude, flight.longitude)
            longitude = flight.longitude
            self.wait()


class TestFlightVerticalSpeed(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.remove_other_vessels()
        cls.vessel = cls.connect().space_center.active_vessel

    def check_speed(self, flight, ref):
        up = normalize(vector(self.vessel.position(ref)) -
                       vector(self.vessel.orbit.body.position(ref)))
        v = self.vessel.velocity(ref)

        speed = norm(v)
        vertical_speed = dot(v, up)
        horizontal_speed = math.sqrt(
            speed*speed - vertical_speed*vertical_speed)

        self.assertAlmostEqual(speed, flight.speed, delta=0.5)
        self.assertAlmostEqual(vertical_speed,
                               flight.vertical_speed, delta=0.5)
        self.assertAlmostEqual(horizontal_speed,
                               flight.horizontal_speed, delta=0.5)

    def test_vertical_speed_positive(self):
        self.set_orbit('Kerbin', 2000000, 0.2, 0, 0, 0, 1, 0)
        ref = self.vessel.orbit.body.reference_frame
        flight = self.vessel.flight(ref)
        self.assertGreater(flight.vertical_speed, 0)
        self.check_speed(flight, ref)

    def test_vertical_speed_negative(self):
        self.set_orbit('Kerbin', 2000000, 0.2, 0, 0, 0, -2, 0)
        ref = self.vessel.orbit.body.reference_frame
        flight = self.vessel.flight(ref)
        self.assertGreater(0, flight.vertical_speed)
        self.check_speed(flight, ref)

    def test_surface_speed(self):
        self.set_circular_orbit('Kerbin', 100000)
        ref = self.vessel.orbit.body.reference_frame
        flight = self.vessel.flight(ref)
        self.check_speed(flight, ref)
        self.assertAlmostEqual(2042.04, flight.speed, places=1)
        self.assertAlmostEqual(2042.04, flight.horizontal_speed, places=1)
        self.assertAlmostEqual(0, flight.vertical_speed, delta=0.5)

    def test_orbital_speed(self):
        self.set_circular_orbit('Kerbin', 100000)
        ref = self.vessel.orbit.body.non_rotating_reference_frame
        flight = self.vessel.flight(ref)
        self.check_speed(flight, ref)
        self.assertAlmostEqual(2246.14, flight.speed, places=1)
        self.assertAlmostEqual(2246.14, flight.horizontal_speed, places=1)
        self.assertAlmostEqual(0, flight.vertical_speed, delta=0.5)


class TestFlightAtLaunchpad(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab('Basic')
        cls.remove_other_vessels()
        cls.vessel = cls.connect().space_center.active_vessel
        cls.far = cls.connect().space_center.far_available

    def test_latitude_and_longitude(self):
        flight = self.vessel.flight()
        self.assertAlmostEqual(-0.09694444, flight.latitude, places=3)
        self.assertAlmostEqual(-74.5575, flight.longitude, places=3)

    def test_ferram_aerospace_research(self):
        if self.far:

            flight = self.vessel.flight()

            self.assertAlmostEqual(1.188, flight.atmosphere_density, places=3)
            self.assertAlmostEqual(0, flight.drag, delta=0.5)
            self.assertAlmostEqual(0, flight.dynamic_pressure)

            self.assertAlmostEqual(0, flight.angle_of_attack, delta=2.5)
            self.assertAlmostEqual(-27, flight.sideslip_angle, delta=2)
            self.assertAlmostEqual(0, flight.stall_fraction)

            self.assertAlmostEqual(0, flight.mach_number)
            self.assertAlmostEqual(193, flight.terminal_velocity, delta=0.5)

            self.assertAlmostEqual(0.103, flight.drag_coefficient, places=3)
            self.assertAlmostEqual(0, flight.lift_coefficient, places=3)
            self.assertAlmostEqual(
                0, flight.pitching_moment_coefficient, places=3)
            self.assertAlmostEqual(
                2246.8, flight.ballistic_coefficient, delta=50)
            self.assertAlmostEqual(0, flight.thrust_specific_fuel_consumption)

            self.assertEqual('Nominal', flight.far_status)

    # def test_drag_coefficient(self):
    #     if not self.far:
    #         # Using stock aerodynamic model
    #         parts = {
    #             'mk1pod': {'n': 1, 'mass': 0.8, 'drag': 0.2},
    #             'fuelTank': {'n': 1, 'mass': 0.125, 'drag': 0.2},
    #             'batteryPack': {'n': 2, 'mass': 0.01, 'drag': 0.2},
    #             'solarPanels1': {'n': 3, 'mass': 0.02, 'drag': 0.25},
    #             'liquidEngine2': {'n': 1, 'mass': 1.5, 'drag': 0.2}
    #         }
    #         total_mass = sum(x['mass']*x['n'] for x in parts.values())
    #         mass_drag_products = sum(x['mass']*x['drag']*x['n']
    #                                  for x in parts.values())
    #         drag_coefficient = mass_drag_products / total_mass
    #         self.assertAlmostEqual(
    #             drag_coefficient, self.vessel.flight().drag_coefficient)


if __name__ == '__main__':
    unittest.main()
