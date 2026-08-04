import math
import unittest

import krpctest
from krpctest.geometry import (
    cross,
    dot,
    norm,
    normalize,
    quaternion_axis_angle,
    quaternion_mult,
    rad2deg,
    vector,
)


class TestFlight(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.set_circular_orbit("Kerbin", 100000)
        cls.space_center = cls.connect().space_center
        cls.vessel = cls.space_center.active_vessel
        cls.set_pitch_heading_roll(27, 116, 40)
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

    def test_surface_normal(self):
        body = self.vessel.orbit.body
        frame = body.reference_frame
        flight = self.vessel.flight(frame)
        normal = flight.surface_normal
        self.assertAlmostEqual(1, norm(normal))
        # It is the normal of the surface at the position of the vessel
        self.assertAlmostEqual(
            body.surface_normal(flight.latitude, flight.longitude, frame),
            normal,
            places=6,
        )
        # The launchpad is level, so its normal points straight up
        up = body.msl_normal(flight.latitude, flight.longitude, frame)
        self.assertAlmostEqual(1, dot(up, normal), places=5)

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


class TestFlightAero(krpctest.TestCase):
    # Stock aerodynamic torque simulation (issue #914) for a craft with control
    # surfaces off the center of mass. The tests are self-consistent: they compare
    # simulated values against each other under a synthetic 300 m/s wind, so they
    # need no live airflow and run on the launchpad. Works for stock and FAR,
    # except where noted.

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("Aero")
        cls.remove_other_vessels()
        cls.space_center = cls.connect().space_center
        cls.vessel = cls.space_center.active_vessel
        cls.far = cls.space_center.far_available

    def head_on_wind(self, ref):
        # 300 m/s wind along the nose (angle of attack 0 at the current attitude)
        return tuple(300 * vector(self.vessel.direction(ref)))

    def pitch_axis(self, ref):
        nose = vector(self.vessel.direction(ref))
        axis = cross(nose, (0, 1, 0))
        if norm(axis) < 0.1:
            axis = cross(nose, (1, 0, 0))
        return normalize(axis)

    def assert_vectors_close(self, expected, actual, rtol=1e-4, atol=1e-3):
        expected = vector(expected)
        actual = vector(actual)
        tolerance = atol + rtol * max(norm(expected), norm(actual))
        self.assertLessEqual(norm(actual - expected), tolerance)

    def test_simulate_aerodynamic_wrench_compatibility(self):
        # The public wrench is ordered force then torque. At zero rate its force
        # agrees with the legacy force API, and its torque backs the existing
        # torque API (SimulateAerodynamicTorqueAt delegates to the wrench). The
        # endpoints share one implementation, but each RPC samples a different
        # physics frame of the unpaused game: the craft shifts slightly on its
        # launchpad suspension between calls, and the legacy endpoints evaluate
        # the atmosphere at their own call-time UT. Torque is more sensitive to
        # that drift than force (lever arms), so it gets its own tolerance.
        body = self.vessel.orbit.body
        ref = body.reference_frame
        flight = self.vessel.flight(ref)
        position = self.vessel.position(ref)
        velocity = self.head_on_wind(ref)
        rotation = self.vessel.rotation(ref)
        zero = (0.0, 0.0, 0.0)
        ut = self.space_center.ut

        # A newly spawned craft can take the DragCubeList exception/fallback path on
        # its first hypothetical direction and initialize KSP's cube caches. Warm all
        # three paths before comparing their steady-state semantics.
        flight.simulate_aerodynamic_wrench_at(
            body, position, velocity, rotation, zero, ut
        )
        flight.simulate_aerodynamic_force_at(body, position, velocity, rotation)
        flight.simulate_aerodynamic_torque_at(body, position, velocity, rotation, zero)
        # Recapture UT after the warm-up: the first wrench call can spend game
        # time rendering drag cubes, and the legacy endpoints always evaluate
        # the atmosphere at their own call-time UT.
        ut = self.space_center.ut
        force, torque = flight.simulate_aerodynamic_wrench_at(
            body, position, velocity, rotation, zero, ut
        )
        legacy_torque = flight.simulate_aerodynamic_torque_at(
            body, position, velocity, rotation, zero
        )
        legacy_force = flight.simulate_aerodynamic_force_at(
            body, position, velocity, rotation
        )

        self.assertEqual(3, len(force))
        self.assertEqual(3, len(torque))
        self.assert_vectors_close(legacy_force, force)
        self.assert_vectors_close(legacy_torque, torque, rtol=1e-3, atol=1e-3)

    def test_position_atmosphere_matches_live_vessel(self):
        # The position APIs and the stock wrench share one atmospheric-state
        # evaluator, which must reproduce FlightIntegrator at the current UT.
        body = self.vessel.orbit.body
        ref = body.reference_frame
        flight = self.vessel.flight(ref)
        position = self.vessel.position(ref)

        self.assertAlmostEqual(
            flight.static_air_temperature,
            body.temperature_at(position, ref),
            delta=1e-2,
        )
        self.assertAlmostEqual(
            flight.atmosphere_density,
            body.atmospheric_density_at_position(position, ref),
            delta=1e-5 * max(flight.atmosphere_density, 1.0),
        )

    def test_simulate_aerodynamic_wrench_ut_changes_solar_exposure(self):
        if self.far:
            self.skipTest("FAR intentionally ignores the wrench UT")
        body = self.vessel.orbit.body
        ref = body.non_rotating_reference_frame
        flight = self.vessel.flight(ref)
        position = self.vessel.position(ref)
        velocity = self.head_on_wind(ref)
        rotation = self.vessel.rotation(ref)
        zero = (0.0, 0.0, 0.0)
        ut = self.space_center.ut

        force_now, _ = flight.simulate_aerodynamic_wrench_at(
            body, position, velocity, rotation, zero, ut
        )
        force_quarter_orbit, _ = flight.simulate_aerodynamic_wrench_at(
            body, position, velocity, rotation, zero, ut + body.orbit.period / 4.0
        )
        self.assertGreater(norm(vector(force_quarter_orbit) - vector(force_now)), 0.1)

    def test_simulate_aerodynamic_torque_attitude(self):
        # A head-on wind (angle of attack 0) produces little torque; pitching 90
        # degrees to a broadside wind produces a large torque about the center of
        # mass (issue #914). Works for stock and FAR.
        body = self.vessel.orbit.body
        ref = body.reference_frame
        flight = self.vessel.flight(ref)
        position = self.vessel.position(ref)
        velocity = self.head_on_wind(ref)
        rotation = self.vessel.rotation(ref)
        zero = (0.0, 0.0, 0.0)

        head_on = vector(
            flight.simulate_aerodynamic_torque_at(
                body, position, velocity, rotation, zero
            )
        )
        pitched = quaternion_mult(
            quaternion_axis_angle(self.pitch_axis(ref), math.radians(90)), rotation
        )
        broadside = vector(
            flight.simulate_aerodynamic_torque_at(
                body, position, velocity, pitched, zero
            )
        )
        self.assertGreater(norm(broadside), 1)  # a real torque
        self.assertGreater(norm(broadside), norm(head_on))

    def test_simulate_aerodynamic_torque_angular_velocity(self):
        # Angular velocity adds the solid-body rotation term omega x r to each
        # part's airflow, producing a damping torque. Stock model only; the FAR
        # path ignores angular velocity.
        if self.far:
            self.skipTest("angular velocity is ignored under FAR")
        body = self.vessel.orbit.body
        ref = body.reference_frame
        flight = self.vessel.flight(ref)
        position = self.vessel.position(ref)
        velocity = self.head_on_wind(ref)
        rotation = self.vessel.rotation(ref)
        zero = (0.0, 0.0, 0.0)
        spin = tuple(5 * vector(self.pitch_axis(ref)))  # 5 rad/s pitch rate

        static = vector(
            flight.simulate_aerodynamic_torque_at(
                body, position, velocity, rotation, zero
            )
        )
        damped = vector(
            flight.simulate_aerodynamic_torque_at(
                body, position, velocity, rotation, spin
            )
        )
        self.assertGreater(norm(damped - static), 1)

    def test_simulate_aerodynamic_wrench_angular_velocity(self):
        # pylint: disable=too-many-locals
        # Central differences expose both the rate-aware force response and the
        # aerodynamic damping torque. FAR intentionally ignores angular velocity.
        if self.far:
            self.skipTest("angular velocity is ignored under FAR")
        body = self.vessel.orbit.body
        ref = body.reference_frame
        flight = self.vessel.flight(ref)
        position = self.vessel.position(ref)
        velocity = self.head_on_wind(ref)
        rotation = self.vessel.rotation(ref)
        axis = vector(self.pitch_axis(ref))
        rate = 5.0
        positive = tuple(rate * axis)
        negative = tuple(-rate * axis)
        ut = self.space_center.ut

        force_positive, torque_positive = flight.simulate_aerodynamic_wrench_at(
            body, position, velocity, rotation, positive, ut
        )
        force_negative, torque_negative = flight.simulate_aerodynamic_wrench_at(
            body, position, velocity, rotation, negative, ut
        )
        force_derivative = (1 / (2 * rate)) * (
            vector(force_positive) - vector(force_negative)
        )
        torque_derivative = (1 / (2 * rate)) * (
            vector(torque_positive) - vector(torque_negative)
        )

        self.assertGreater(norm(force_derivative), 0.01)
        self.assertGreater(norm(torque_derivative), 1)
        self.assertLess(dot(torque_derivative, axis), 0)

    def test_simulate_aerodynamic_wrench_reference_frames(self):
        # pylint: disable=too-many-locals
        # Express one physical COM state in rotating-body, non-rotating-body and
        # vessel frames. Transform every result to the non-rotating frame before
        # comparison. CelestialBody.angular_velocity supplies one stable physical
        # body rate while accounting for each reference frame's rotation rate.
        body = self.vessel.orbit.body
        common = body.non_rotating_reference_frame
        rotating = body.reference_frame
        vessel_frame = self.vessel.reference_frame

        def capture_common_state():
            return (
                self.vessel.position(common),
                tuple(300 * vector(self.vessel.direction(common))),
                self.vessel.rotation(common),
            )

        common_position, common_velocity, common_rotation = capture_common_state()
        ut = self.space_center.ut

        def evaluate(ref):
            position = self.space_center.transform_position(
                common_position, common, ref
            )
            velocity = self.space_center.transform_velocity(
                common_position, common_velocity, common, ref
            )
            rotation = self.space_center.transform_rotation(
                common_rotation, common, ref
            )
            angular_velocity = body.angular_velocity(ref)
            force, torque = self.vessel.flight(ref).simulate_aerodynamic_wrench_at(
                body, position, velocity, rotation, angular_velocity, ut
            )
            return (
                self.space_center.transform_direction(force, ref, common),
                self.space_center.transform_direction(torque, ref, common),
            )

        # Initialize any fresh-craft drag-cube cache paths before recording the
        # invariant outputs. KRPC.Paused cannot be used here because it may stop
        # the server before the setter response is flushed.
        for ref in (rotating, common, vessel_frame):
            evaluate(ref)

        common_position, common_velocity, common_rotation = capture_common_state()
        ut = self.space_center.ut
        results = [evaluate(ref) for ref in (rotating, common, vessel_frame)]

        expected_force, expected_torque = results[0]
        for force, torque in results[1:]:
            self.assert_vectors_close(expected_force, force)
            self.assert_vectors_close(expected_torque, torque)

    def test_simulate_aerodynamic_wrench_edge_cases(self):
        body = self.vessel.orbit.body
        ref = body.reference_frame
        flight = self.vessel.flight(ref)
        position = vector(self.vessel.position(ref))
        rotation = self.vessel.rotation(ref)
        zero = (0.0, 0.0, 0.0)
        ut = self.space_center.ut

        # Air fixed in the rotating body frame gives no relative airflow.
        force, torque = flight.simulate_aerodynamic_wrench_at(
            body, tuple(position), zero, rotation, zero, ut
        )
        self.assert_vectors_close(zero, force, rtol=0, atol=1e-3)
        self.assert_vectors_close(zero, torque, rtol=0, atol=1e-3)

        # A state above the atmosphere gives no aerodynamic wrench.
        vacuum_position = tuple(
            (body.equatorial_radius + body.atmosphere_depth + 10000)
            * vector(normalize(position))
        )
        force, torque = flight.simulate_aerodynamic_wrench_at(
            body, vacuum_position, self.head_on_wind(ref), rotation, zero, ut
        )
        self.assert_vectors_close(zero, force, rtol=0, atol=1e-3)
        self.assert_vectors_close(zero, torque, rtol=0, atol=1e-3)

        # A hypothetical attitude must affect both components on this asymmetric
        # fixture without mutating the vessel's real orientation.
        pitched = quaternion_mult(
            quaternion_axis_angle(self.pitch_axis(ref), math.radians(90)), rotation
        )
        head_on = flight.simulate_aerodynamic_wrench_at(
            body, tuple(position), self.head_on_wind(ref), rotation, zero, ut
        )
        broadside = flight.simulate_aerodynamic_wrench_at(
            body, tuple(position), self.head_on_wind(ref), pitched, zero, ut
        )
        self.assertGreater(norm(vector(broadside[0]) - vector(head_on[0])), 100)
        self.assertGreater(norm(vector(broadside[1]) - vector(head_on[1])), 1)


class TestFlightAeroFAR(TestFlightAero):
    """The aerodynamic simulation suite run against Ferram Aerospace Research instead of the
    stock model. The simulation RPCs hand the whole state to FAR when it is installed, so the
    same expectations hold; the base class skips the few checks that are stock-only."""

    mods = ["FAR"]


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

    def _simulate_force_and_check_wrench(
        self, flight, body, position, velocity, rotation
    ):
        # At zero angular velocity, the wrench force must preserve the legacy
        # force API for every physical control-surface state.
        legacy_force = vector(
            flight.simulate_aerodynamic_force_at(body, position, velocity, rotation)
        )
        ut = self.space_center.ut
        wrench_force, _ = flight.simulate_aerodynamic_wrench_at(
            body, position, velocity, rotation, (0.0, 0.0, 0.0), ut
        )
        wrench_force = vector(wrench_force)
        tolerance = 1e-3 + 1e-4 * max(norm(legacy_force), norm(wrench_force))
        self.assertLessEqual(norm(wrench_force - legacy_force), tolerance)
        return legacy_force

    def test_simulate_aerodynamic_force_and_wrench_airbrake_deploy(self):
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
            retracted = self._simulate_force_and_check_wrench(
                flight, body, position, velocity, rotation
            )
            retracted_mag = norm(retracted)
            self.assertGreater(retracted_mag, 1.0)

            self._set_airbrakes_deployed(True)
            deployed = self._simulate_force_and_check_wrench(
                flight, body, position, velocity, rotation
            )
            deployed_mag = norm(deployed)
            # Before the fix, deflection is ignored so deployed == retracted.
            self.assertGreater(deployed_mag, 1.25 * retracted_mag)

            self._set_airbrakes_deployed(False)
            retracted_again = self._simulate_force_and_check_wrench(
                flight, body, position, velocity, rotation
            )
            self.assertAlmostEqual(
                retracted_mag, norm(retracted_again), delta=0.05 * retracted_mag
            )
        finally:
            for cs in self._airbrakes():
                cs.deployed = False


class TestFlightFAR(krpctest.TestCase):
    """The live aerodynamic readouts with Ferram Aerospace Research installed, which
    replaces the stock aerodynamics model with its own (issue #498)."""

    mods = ["FAR"]

    ALTITUDE = 5000
    SPEED = 200
    # A nose-up attitude relative to the flight path, so the air stream pushes the pod
    # sideways as well as backwards and there is a lift force to check.
    ANGLE_OF_ATTACK = 20
    # Time given to the flight readouts to catch up with a freshly placed vessel. A couple
    # of physics frames: the pod is unstable at this angle of attack and starts pitching up
    # as soon as physics resume, so every extra frame moves the state being measured.
    SETTLE = 0.05

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("Basic")
        cls.remove_other_vessels()
        cls.space_center = cls.connect().space_center
        cls.vessel = cls.space_center.active_vessel
        cls.body = cls.vessel.orbit.body
        cls.ref = cls.body.reference_frame

    def fly(self, roll=0):
        """Put the pod in level flight at a fixed angle of attack and the given roll, and
        wait until FAR reports a force on it (it voxelizes the craft asynchronously, and
        reports nothing until the vessel is unpacked and moving through the air)."""
        self.set_flight(
            altitude=self.ALTITUDE,
            speed=self.SPEED,
            pitch=self.ANGLE_OF_ATTACK,
            angle_of_attack=self.ANGLE_OF_ATTACK,
            roll=roll,
        )
        flight = self.vessel.flight(self.ref)
        self.wait_until(
            lambda: norm(vector(flight.aerodynamic_force)) > 1,
            message="FAR to report an aerodynamic force on the vessel",
        )
        # The per-frame flight readouts lag the teleport by a frame or two, so let them
        # catch up with the state the vessel was placed in before reading any of them.
        self.wait(self.SETTLE)
        return flight

    def airstream_angles(self):
        """The angle of attack and sideslip angle the vessel is currently flying at, in
        degrees, worked out from the direction the air stream comes from in the vessel's own
        axes: +x to its right, +y along its nose, +z out of its belly. Each is the angle
        between the nose and the air stream in one plane, so the air stream is projected
        onto the nose-belly plane for the angle of attack and the nose-right plane for the
        sideslip angle."""
        airstream = self.space_center.transform_direction(
            self.vessel.flight(self.ref).velocity, self.ref, self.vessel.reference_frame
        )
        right, nose, belly = airstream
        return (
            rad2deg(math.atan2(belly, nose)),
            rad2deg(math.atan2(right, nose)),
        )

    def test_aerodynamic_force_matches_far(self):
        # The reported force is the one FAR applies to the vessel, so it agrees with FAR's
        # own simulation of the same state in direction as well as magnitude. Building it
        # from FAR's lift and drag coefficients instead, as kRPC used to, gets the
        # direction wrong because those coefficients are scalars measured against the
        # vessel's own axes.
        flight = self.fly()
        force = vector(flight.aerodynamic_force)
        simulated = vector(
            flight.simulate_aerodynamic_force_at(
                self.body,
                self.vessel.position(self.ref),
                self.vessel.velocity(self.ref),
                self.vessel.rotation(self.ref),
            )
        )
        self.assertGreater(norm(simulated), 1)
        # The live force and the state passed to the simulation are read a few physics
        # frames apart while the pod pitches up, so allow for the state moving on between
        # them. The force the old code built pointed somewhere else entirely.
        self.assertLess(norm(force - simulated), 0.2 * norm(simulated))

    def test_lift_and_drag_split_the_force(self):
        # Drag, lift and side force are the components of the total force along the air
        # stream, across it towards the top of the vessel, and across it out of its side.
        # The three are mutually perpendicular and sum back to the total force, and each
        # acceleration is its force over the mass.
        flight = self.fly()
        force = vector(flight.aerodynamic_force)
        lift = vector(flight.lift)
        drag = vector(flight.drag)
        side_force = vector(flight.side_force)
        airstream = normalize(vector(flight.velocity))
        magnitude = norm(force)
        self.assertGreater(magnitude, 1)
        self.assertLess(norm(lift + side_force + drag - force), 1e-3 * magnitude)
        # Drag opposes the motion through the air; the other two are across it.
        self.assertLess(dot(drag, airstream), 0)
        self.assertLess(abs(dot(lift, airstream)), 1e-3 * magnitude)
        self.assertLess(abs(dot(side_force, airstream)), 1e-3 * magnitude)
        self.assertLess(abs(dot(lift, side_force)), 1e-3 * magnitude * magnitude)
        # The pod is placed with its nose in the plane of its flight path, so the air
        # stream pushes it towards its top and hardly at all out of its side.
        self.assertGreater(norm(lift), 10 * norm(side_force))
        mass = self.vessel.mass
        for force_attr, accel_attr in (
            ("aerodynamic_force", "aerodynamic_acceleration"),
            ("lift", "lift_acceleration"),
            ("drag", "drag_acceleration"),
        ):
            expected = tuple(f / mass for f in getattr(flight, force_attr))
            self.assertAlmostEqual(
                expected, getattr(flight, accel_attr), delta=1e-3 * magnitude / mass
            )

    def test_aerodynamic_force_independent_of_roll(self):
        # Issue #498: rolling a vessel of revolution about its own axis does not change the
        # aerodynamic force acting on it. Compare the two quantities that describe the
        # force relative to the air stream - how strong it is, and how far it is turned
        # from the air stream - after flying at the same angle of attack, once upright and
        # once rolled onto its side.
        def measure(roll):
            flight = self.fly(roll)
            force = vector(flight.aerodynamic_force)
            airstream = normalize(vector(flight.velocity))
            return norm(force), rad2deg(math.acos(dot(normalize(force), airstream)))

        upright_magnitude, upright_angle = measure(0)
        rolled_magnitude, rolled_angle = measure(90)
        self.assertGreater(upright_magnitude, 1)
        # The pod is a body of revolution apart from its hatch and windows, so allow a
        # little for those. Reconstructing the force from FAR's lift coefficient loses the
        # whole across-stream component when rolled, which is far larger than this.
        self.assertAlmostEqual(
            upright_magnitude, rolled_magnitude, delta=0.1 * upright_magnitude
        )
        self.assertAlmostEqual(upright_angle, rolled_angle, delta=5)

    def test_rolling_moves_lift_into_side_force(self):
        # Lift and side force are named for the vessel, not for the world: they are the two
        # halves of the across-stream force, one towards the vessel's top and one out of its
        # side. Flying the same angle of attack rolled onto its side turns what was lift into
        # side force, while the total force across the stream is unchanged.
        def measure(roll):
            flight = self.fly(roll)
            return norm(vector(flight.lift)), norm(vector(flight.side_force))

        upright_lift, upright_side = measure(0)
        rolled_lift, rolled_side = measure(90)
        self.assertGreater(upright_lift, 10 * upright_side)
        self.assertGreater(rolled_side, 10 * rolled_lift)
        # The air pushes the pod just as hard across the stream either way round.
        self.assertAlmostEqual(
            math.hypot(upright_lift, upright_side),
            math.hypot(rolled_lift, rolled_side),
            delta=0.15 * math.hypot(upright_lift, upright_side),
        )

    def test_flight_parameters(self):
        # The flight parameters FAR computes, read while flying a known state: the pod is
        # placed at a set airspeed and angle of attack with no sideslip, and the pressures
        # and speeds follow from the atmosphere it is flying through.
        flight = self.fly()
        speed = flight.true_air_speed
        density = flight.atmosphere_density
        dynamic_pressure = flight.dynamic_pressure
        mach = flight.mach
        self.assertAlmostEqual(self.SPEED, speed, delta=15)
        self.assertAlmostEqual(
            0.5 * density * speed * speed,
            dynamic_pressure,
            delta=0.05 * dynamic_pressure,
        )
        # FAR derives the speed of sound from its own atmosphere model, so its Mach number
        # is near, but not equal to, the airspeed over the speed of sound the game reports.
        self.assertAlmostEqual(speed / flight.speed_of_sound, mach, delta=0.2 * mach)
        # The angle of attack and sideslip angle are the two angles between the nose and the
        # air stream, measured in the vessel's pitch and yaw planes. Check them against the
        # air stream in the vessel's own axes, which moves with the pod as it pitches up
        # out of the attitude it was placed in.
        angle_of_attack, sideslip_angle = self.airstream_angles()
        self.assertGreater(
            angle_of_attack, 0
        )  # placed nose-up, so the air comes from below
        self.assertAlmostEqual(angle_of_attack, flight.angle_of_attack, delta=2)
        self.assertAlmostEqual(sideslip_angle, flight.sideslip_angle, delta=2)
        # Terminal velocity is an estimate for this vessel falling in this atmosphere. The
        # pod carries no lifting surface, so there is nothing on it that can stall.
        self.assertGreater(flight.terminal_velocity, 0)
        self.assertEqual(0, flight.stall_fraction)
        # A blunt capsule at 200 m/s in the lower atmosphere: a large Reynolds number, real
        # drag, and lift towards its back as it is held nose-up.
        self.assertGreater(flight.reynolds_number, 1e6)
        self.assertGreater(flight.drag_coefficient, 0)
        self.assertGreater(flight.lift_coefficient, 0)
        self.assertGreater(flight.ballistic_coefficient, 0)
        # The pod carries no engine, so nothing is burning fuel to make thrust.
        self.assertEqual(0, flight.thrust_specific_fuel_consumption)

    def test_lift_and_drag_coefficients_match_the_forces(self):
        # Both coefficients are their force divided by the same dynamic pressure and
        # reference area, so their ratio is the ratio of the forces they measure. FAR
        # measures its lift against the vessel's own axes rather than the air stream, as the
        # component of the whole across-stream force that points towards the top of the
        # vessel, so take that force in the vessel frame, whose z-axis points out of its
        # bottom.
        self.fly()
        flight = self.vessel.flight(self.vessel.reference_frame)
        across_stream = vector(flight.lift) + vector(flight.side_force)
        lift_over_drag = -across_stream[2] / norm(vector(flight.drag))
        self.assertGreater(abs(lift_over_drag), 0.01)
        self.assertAlmostEqual(
            lift_over_drag,
            flight.lift_coefficient / flight.drag_coefficient,
            delta=0.1 * abs(lift_over_drag),
        )

    def test_stock_only_readouts_unavailable(self):
        # The per-part stock aerodynamic readouts have no FAR counterpart, so they refuse
        # rather than report a stale stock value.
        self.assertTrue(self.space_center.far_available)
        part = self.vessel.parts.root
        self.assertRaises(RuntimeError, part.lift, part.reference_frame)
        self.assertRaises(RuntimeError, part.drag, part.reference_frame)


if __name__ == "__main__":
    unittest.main()
