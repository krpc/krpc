import unittest
import math
import krpctest
from krpctest.geometry import (
    rad2deg,
    norm,
    normalize,
    dot,
    cross,
    vector,
    quaternion_axis_angle,
    quaternion_mult,
)


class TestFlight(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.remove_other_vessels()
        cls.set_circular_orbit("Kerbin", 100000)
        cls.space_center = cls.connect().space_center
        cls.vessel = cls.space_center.active_vessel
        cls.connect().testing_tools.clear_rotation()
        cls.connect().testing_tools.apply_rotation(116, (0, 0, -1))
        cls.connect().testing_tools.apply_rotation(27, (-1, 0, 0))
        cls.connect().testing_tools.apply_rotation(40, (0, -1, 0))
        cls.far = cls.space_center.far_available

    def test_equality(self):
        flight = self.vessel.flight(self.vessel.reference_frame)
        self.assertEqual(flight, self.vessel.flight(self.vessel.reference_frame))

    def check_properties_not_affected_by_reference_frame(self, flight):
        """Verify flight properties that aren't
        affected by reference frames"""
        # The orbit is set once in setUpClass and shared by every test, so by
        # the time the later tests run the vessel has drifted a few meters from
        # the ideal circular orbit due to off-rails physics integration. Use a
        # tolerance loose enough to absorb that drift (still 0.05% of 100km).
        self.assertAlmostEqual(100000, flight.mean_altitude, delta=50)
        self.assertAlmostEqual(
            100000 - max(0, flight.elevation), flight.surface_altitude, delta=50
        )
        self.assertAlmostEqual(
            100000 - flight.elevation, flight.bedrock_altitude, delta=50
        )

    def check_directions(self, flight):
        """Check flight.direction against flight.heading and flight.pitch"""
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
            flight.heading,
            delta=1,
        )

    def check_speeds(self, flight):
        """Check flight.velocity agrees with flight.*_speed"""
        up_direction = (0, 1, 0)
        velocity = vector(flight.velocity)
        vertical_speed = dot(velocity, up_direction)
        horizontal_speed = norm(velocity) - vertical_speed
        self.assertAlmostEqual(norm(velocity), flight.speed, delta=1)
        self.assertAlmostEqual(horizontal_speed, flight.horizontal_speed, delta=1)
        self.assertAlmostEqual(vertical_speed, flight.vertical_speed, delta=1)

    def check_orbital_vectors(self, flight):
        """Check orbital direction vectors"""
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
        self.assertAlmostEqual(tuple(prograde), [-x for x in retrograde], places=2)
        self.assertAlmostEqual(tuple(radial), [-x for x in anti_radial], places=2)
        self.assertAlmostEqual(tuple(normal), [-x for x in anti_normal], places=2)
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

    def test_acceleration(self):
        # On a stable circular orbit the total acceleration (which includes
        # gravity) is gravitational: directed radially inward with a magnitude
        # close to the local gravity. KSP's own reported acceleration differs from
        # mu/r^2 by a few percent on a teleported orbit, so the magnitude is
        # checked with a loose tolerance; the radially-inward direction is exact.
        ref = self.vessel.orbit.body.non_rotating_reference_frame
        flight = self.vessel.flight(ref)
        g = self.vessel.orbit.body.gravitational_parameter / (
            self.vessel.orbit.radius**2
        )
        acceleration = vector(flight.acceleration)
        radial_out = normalize(vector(self.vessel.position(ref)))
        # Directed radially inward (the world-to-frame transform and sign is exact).
        self.assertAlmostEqual(
            -norm(acceleration), dot(acceleration, radial_out), delta=0.1
        )
        # Gravity-scale magnitude.
        self.assertAlmostEqual(g, norm(acceleration), delta=1.0)

    def test_aerodynamic_acceleration(self):
        # aerodynamic_acceleration/lift_acceleration/drag_acceleration should each
        # equal the corresponding aerodynamic force divided by the vessel's mass.
        if self.far:
            self.skipTest("stock aerodynamics only")
        flight = self.vessel.flight(self.vessel.reference_frame)
        mass = self.vessel.mass
        for force_attr, accel_attr in (
            ("aerodynamic_force", "aerodynamic_acceleration"),
            ("lift", "lift_acceleration"),
            ("drag", "drag_acceleration"),
        ):
            force = getattr(flight, force_attr)
            accel = getattr(flight, accel_attr)
            expected = tuple(f / mass for f in force)
            self.assertAlmostEqual(expected, accel, delta=1e-3)


class TestFlightVerticalSpeed(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.remove_other_vessels()
        cls.space_center = cls.connect().space_center
        cls.vessel = cls.space_center.active_vessel

    def check_speed(self, flight, ref):
        up = normalize(
            vector(self.vessel.position(ref))
            - vector(self.vessel.orbit.body.position(ref))
        )
        v = self.vessel.velocity(ref)

        speed = norm(v)
        vertical_speed = dot(v, up)
        horizontal_speed = math.sqrt(speed * speed - vertical_speed * vertical_speed)

        self.assertAlmostEqual(speed, flight.speed, delta=0.5)
        self.assertAlmostEqual(vertical_speed, flight.vertical_speed, delta=0.5)
        self.assertAlmostEqual(horizontal_speed, flight.horizontal_speed, delta=0.5)

    def test_vertical_speed_positive(self):
        # Pin the epoch to the current universal time so the vessel is observed
        # at mean anomaly 1 (well past periapsis, climbing), where the radial
        # velocity is large and unambiguously positive. With epoch=0 the
        # observed phase depends on the save's current UT, which can place the
        # vessel at a turning point where the sign of vertical_speed is
        # decided by timing jitter.
        ut = self.space_center.ut
        self.set_orbit("Kerbin", 2000000, 0.2, 0, 0, 0, 1, ut)
        ref = self.vessel.orbit.body.reference_frame
        flight = self.vessel.flight(ref)
        self.assertGreater(flight.vertical_speed, 0)
        self.check_speed(flight, ref)

    def test_vertical_speed_negative(self):
        # See test_vertical_speed_positive: mean anomaly -2 places the vessel
        # past apoapsis and descending, so vertical_speed is solidly negative
        # once the epoch is pinned to the current universal time.
        ut = self.space_center.ut
        self.set_orbit("Kerbin", 2000000, 0.2, 0, 0, 0, -2, ut)
        ref = self.vessel.orbit.body.reference_frame
        flight = self.vessel.flight(ref)
        self.assertGreater(0, flight.vertical_speed)
        self.check_speed(flight, ref)

    def test_surface_speed(self):
        self.set_circular_orbit("Kerbin", 100000)
        ref = self.vessel.orbit.body.reference_frame
        flight = self.vessel.flight(ref)
        self.check_speed(flight, ref)
        self.assertAlmostEqual(2042.04, flight.speed, places=1)
        self.assertAlmostEqual(2042.04, flight.horizontal_speed, places=1)
        self.assertAlmostEqual(0, flight.vertical_speed, delta=0.5)

    def test_orbital_speed(self):
        self.set_circular_orbit("Kerbin", 100000)
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
        cls.launch_vessel_from_vab("Basic")
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
            self.assertAlmostEqual(0, flight.pitching_moment_coefficient, places=3)
            self.assertAlmostEqual(2246.8, flight.ballistic_coefficient, delta=50)
            self.assertAlmostEqual(0, flight.thrust_specific_fuel_consumption)

            self.assertEqual("Nominal", flight.far_status)

    def test_simulate_aerodynamic_force_rotation(self):
        # The rotation argument sets the vessel attitude the force is computed
        # for (issue #913). A 300 m/s head-on wind (angle of attack 0 at the
        # current attitude) gives a real force; pitching 90 degrees turns it into
        # a broadside wind and changes the force. Works for stock and FAR (both
        # return the force in newtons).
        body = self.vessel.orbit.body
        ref = body.reference_frame
        flight = self.vessel.flight(ref)
        position = self.vessel.position(ref)
        nose = vector(self.vessel.direction(ref))
        velocity = tuple(300 * nose)  # angle of attack 0 at the current attitude
        head_on = vector(
            flight.simulate_aerodynamic_force_at(
                body, position, velocity, self.vessel.rotation(ref)
            )
        )
        self.assertGreater(norm(head_on), 1000)  # a real force
        axis = cross(nose, (0, 1, 0))
        if norm(axis) < 0.1:
            axis = cross(nose, (1, 0, 0))
        pitched = quaternion_mult(
            quaternion_axis_angle(normalize(axis), math.radians(90)),
            self.vessel.rotation(ref),
        )
        broadside = vector(
            flight.simulate_aerodynamic_force_at(body, position, velocity, pitched)
        )
        self.assertGreater(norm(broadside - head_on), 0.1 * norm(head_on))

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


class TestFlightAirbrake(krpctest.TestCase):
    """Regression for issue #622: SimulateAerodynamicForceAt must account for
    the current physical deflection of ModuleAeroSurface airbrakes."""

    # Stock airbrake1: actuatorSpeed 20 deg/s, ctrlSurfaceRange 70 deg.
    ACTUATOR_WAIT = 5.0

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("FlightAirbrake")
        cls.remove_other_vessels()
        cls.space_center = cls.connect().space_center
        cls.vessel = cls.space_center.active_vessel
        cls.far = cls.space_center.far_available

    def _airbrakes(self):
        return [
            cs
            for cs in self.vessel.parts.control_surfaces
            if any(m.name == "ModuleAeroSurface" for m in cs.part.modules)
        ]

    def _set_airbrakes_deployed(self, deployed):
        for cs in self._airbrakes():
            cs.deployed = deployed
        self.wait(self.ACTUATOR_WAIT)

    def test_simulate_aerodynamic_force_airbrake_deploy(self):
        if self.far:
            self.skipTest("Stock airbrake deflection path; skip when FAR is installed")

        airbrakes = self._airbrakes()
        self.assertGreaterEqual(len(airbrakes), 1)

        body = self.vessel.orbit.body
        ref = body.reference_frame
        flight = self.vessel.flight(ref)
        # Fixed synthetic airflow so the test does not depend on flight.
        position = self.vessel.position(ref)
        rotation = self.vessel.rotation(ref)
        nose = vector(self.vessel.direction(ref))
        velocity = tuple(300 * nose)

        try:
            self._set_airbrakes_deployed(False)
            retracted = vector(
                flight.simulate_aerodynamic_force_at(body, position, velocity, rotation)
            )
            retracted_mag = norm(retracted)
            self.assertGreater(retracted_mag, 1.0)

            self._set_airbrakes_deployed(True)
            deployed = vector(
                flight.simulate_aerodynamic_force_at(body, position, velocity, rotation)
            )
            deployed_mag = norm(deployed)
            # Before the fix, deflection is ignored so deployed == retracted.
            self.assertGreater(deployed_mag, 1.25 * retracted_mag)

            self._set_airbrakes_deployed(False)
            retracted_again = vector(
                flight.simulate_aerodynamic_force_at(body, position, velocity, rotation)
            )
            self.assertAlmostEqual(
                retracted_mag, norm(retracted_again), delta=0.05 * retracted_mag
            )
        finally:
            for cs in self._airbrakes():
                cs.deployed = False


if __name__ == "__main__":
    unittest.main()
