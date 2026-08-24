import math
import unittest

import krpctest
from krpctest.geometry import compute_position, norm


class TestOrbit(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.space_center = cls.connect().space_center

    def check_radius_and_speed(self, obj, orbit):
        # Compute position from orbital elements
        pos = compute_position(obj, orbit.body.non_rotating_reference_frame)
        # Compute radius from position
        radius = norm(pos) * 1000000
        self.assertAlmostEqual(radius, orbit.radius, delta=1)
        # Compute speed from radius
        speed = math.sqrt(
            orbit.body.gravitational_parameter
            * ((2 / radius) - (1 / orbit.semi_major_axis))
        )
        self.assertAlmostEqual(speed, orbit.speed, delta=1)

    def check_orbital_energy(self, orbit):
        # Specific orbital energy is -mu / (2a) for a bound orbit. KSP derives
        # it from the instantaneous state vectors, so compare as a ratio to
        # tolerate the small jitter in an active vessel's velocity.
        energy = -orbit.body.gravitational_parameter / (2 * orbit.semi_major_axis)
        self.assertAlmostEqual(energy / orbit.orbital_energy, 1, places=3)

    def check_angles_close(self, angle, other_angle, places=2):
        # Compare two angles, in radians, ignoring multiples of 2*pi
        diff = (angle - other_angle + math.pi) % (2 * math.pi) - math.pi
        self.assertAlmostEqual(0, diff, places=places)

    def check_anomalies(self, obj, orbit):
        g = self.space_center.g
        ut = self.space_center.ut
        mean_anomaly_at_epoch = orbit.mean_anomaly_at_epoch
        epoch = orbit.epoch

        # Compute mean anomaly using Kepler's equation
        mean_anomaly = orbit.eccentric_anomaly - (
            orbit.eccentricity * math.sin(orbit.eccentric_anomaly)
        )
        self.check_angles_close(mean_anomaly, orbit.mean_anomaly)

        # Compute mean anomaly using mean motion and time since epoch
        mean_motion = math.sqrt(
            (g * (orbit.body.mass + obj.mass)) / (orbit.semi_major_axis**3)
        )
        delta_t = ut - epoch
        mean_anomaly = mean_anomaly_at_epoch + (mean_motion * delta_t)
        self.check_angles_close(mean_anomaly, orbit.mean_anomaly)

    def check_time_to_apoapsis_and_periapsis(self, obj, orbit):
        # Compute the time to apoapsis and periapsis using mean motion
        g = self.space_center.g
        mean_motion = math.sqrt(
            (g * (orbit.body.mass + obj.mass)) / (orbit.semi_major_axis**3)
        )
        time_since_periapsis = orbit.mean_anomaly / mean_motion
        time_to_periapsis = orbit.period - time_since_periapsis
        time_to_apoapsis = (orbit.period / 2) - time_since_periapsis
        if time_to_apoapsis < 0:
            time_to_apoapsis += orbit.period

        self.assertAlmostEqual(time_to_apoapsis, orbit.time_to_apoapsis, delta=2)
        self.assertAlmostEqual(time_to_periapsis, orbit.time_to_periapsis, delta=2)

    def test_vessel_orbiting_kerbin(self):
        self.set_circular_orbit("Kerbin", 100000)
        vessel = self.space_center.active_vessel
        orbit = vessel.orbit
        self.assertEqual("Kerbin", orbit.body.name)
        self.assertAlmostEqual(100000 + 600000, orbit.apoapsis, delta=50)
        self.assertAlmostEqual(100000 + 600000, orbit.periapsis, delta=50)
        self.assertAlmostEqual(100000, orbit.apoapsis_altitude, delta=50)
        self.assertAlmostEqual(100000, orbit.periapsis_altitude, delta=50)
        self.assertAlmostEqual(100000 + 600000, orbit.semi_major_axis, delta=50)
        self.assertAlmostEqual(100000 + 600000, orbit.semi_minor_axis, delta=50)
        self.assertAlmostEqual(700000, orbit.radius, delta=50)
        self.assertAlmostEqual(2246.1, orbit.speed, delta=1)
        self.check_radius_and_speed(vessel, orbit)
        self.check_orbital_energy(orbit)
        # self.check_time_to_apoapsis_and_periapsis(vessel, orbit)
        self.assertIsNaN(orbit.time_to_soi_change)
        self.assertAlmostEqual(0, orbit.eccentricity, places=1)
        self.assertAlmostEqual(0, orbit.inclination, places=1)
        # longitude_of_ascending_node and argument_of_periapsis are
        # degenerate for a circular equatorial orbit, so cannot be checked
        # self.check_anomalies(vessel, orbit)
        self.assertIsNone(orbit.next_orbit)

    def test_vessel_orbiting_bop(self):
        self.set_orbit("Bop", 320000, 0.18, 27, 38, 241, 2.3, 0)
        vessel = self.space_center.active_vessel
        orbit = vessel.orbit
        self.assertEqual("Bop", orbit.body.name)
        self.assertAlmostEqual(377600, orbit.apoapsis, delta=50)
        self.assertAlmostEqual(262400, orbit.periapsis, delta=50)
        self.assertAlmostEqual(377600 - 65000, orbit.apoapsis_altitude, delta=50)
        self.assertAlmostEqual(262400 - 65000, orbit.periapsis_altitude, delta=50)
        sma = 0.5 * (377600 + 262400)
        ecc = 0.18
        self.assertAlmostEqual(sma, orbit.semi_major_axis, delta=50)
        self.assertAlmostEqual(
            sma * math.sqrt(1 - (ecc * ecc)), orbit.semi_minor_axis, delta=50
        )
        # self.check_radius_and_speed(vessel, orbit)
        self.check_time_to_apoapsis_and_periapsis(vessel, orbit)
        # self.assertIsNaN(orbit.time_to_soi_change)
        self.assertAlmostEqual(ecc, orbit.eccentricity, places=1)
        self.assertAlmostEqual(27 * (math.pi / 180), orbit.inclination, places=1)
        self.assertAlmostEqual(
            38 * (math.pi / 180), orbit.longitude_of_ascending_node, places=1
        )
        self.assertAlmostEqual(
            241 * (math.pi / 180), orbit.argument_of_periapsis, places=1
        )
        # mean_anomaly_at_epoch and epoch cannot be checked against the values
        # passed to set_orbit, as KSP re-epochs the orbit every frame while the
        # vessel is under physics; check_anomalies verifies their consistency
        self.check_anomalies(vessel, orbit)
        # self.assertNone(orbit.next_orbit)

    def test_vessel_orbiting_mun_on_escape_soi(self):
        self.set_orbit("Mun", 1800000, 0.52, 0, 13, 67, 6.25, 0)
        vessel = self.space_center.active_vessel
        orbit = vessel.orbit
        self.assertEqual("Mun", orbit.body.name)
        self.assertAlmostEqual(2736000, orbit.apoapsis, delta=100)
        self.assertAlmostEqual(864000, orbit.periapsis, delta=50)
        self.assertAlmostEqual(2736000 - 200000, orbit.apoapsis_altitude, delta=100)
        self.assertAlmostEqual(864000 - 200000, orbit.periapsis_altitude, delta=50)
        sma = 0.5 * (2736000 + 864000)
        ecc = 0.52
        self.assertAlmostEqual(sma, orbit.semi_major_axis, delta=50)
        self.assertAlmostEqual(
            sma * math.sqrt(1 - (ecc * ecc)), orbit.semi_minor_axis, delta=50
        )
        # self.check_radius_and_speed(vessel, orbit)
        # self.check_time_to_apoapsis_and_periapsis(vessel, orbit)
        # self.assertAlmostEqual(17414, orbit.time_to_soi_change,delta=5)
        self.assertAlmostEqual(ecc, orbit.eccentricity, places=1)
        self.assertAlmostEqual(0, orbit.inclination, places=1)
        # longitude_of_ascending_node and argument_of_periapsis are
        # degenerate for an equatorial orbit, so cannot be checked
        # self.check_anomalies(vessel, orbit)
        self.assertIsNotNone(orbit.next_orbit)

        orbit = orbit.next_orbit
        self.assertEqual("Kerbin", orbit.body.name)

    def test_vessel_orbiting_minmus_on_parabolic_arc(self):
        self.set_orbit("Minmus", 80000, 3, 0, 0, 0, 0, 0)
        vessel = self.space_center.active_vessel
        orbit = vessel.orbit
        self.assertEqual("Minmus", orbit.body.name)
        self.assertAlmostEqual(-320000, orbit.apoapsis, delta=50)
        self.assertAlmostEqual(160000, orbit.periapsis, delta=50)
        self.assertAlmostEqual(-320000 - 60000, orbit.apoapsis_altitude, delta=50)
        self.assertAlmostEqual(160000 - 60000, orbit.periapsis_altitude, delta=50)
        sma = 0.5 * (-320000 + 160000)
        ecc = 3
        self.assertAlmostEqual(sma, orbit.semi_major_axis, delta=50)
        self.assertIsNaN(orbit.semi_minor_axis)
        # self.check_radius_and_speed(vessel, orbit)
        # self.check_time_to_apoapsis_and_periapsis(vessel, orbit)
        # self.assertAlmostEqual(12884, orbit.time_to_soi_change, delta=5)
        self.assertAlmostEqual(ecc, orbit.eccentricity, places=1)
        self.assertAlmostEqual(0, orbit.inclination, places=1)
        # longitude_of_ascending_node and argument_of_periapsis are
        # degenerate for an equatorial orbit, so cannot be checked
        # self.check_anomalies(vessel, orbit)
        self.assertIsNotNone(orbit.next_orbit)

        orbit = orbit.next_orbit
        self.assertEqual("Kerbin", orbit.body.name)

    def test_sun_orbit(self):
        sun = self.space_center.bodies["Sun"]
        self.assertIsNone(sun.orbit)

    def test_kerbin_orbiting_sun(self):
        body = self.space_center.bodies["Kerbin"]
        orbit = body.orbit
        self.assertEqual("Sun", orbit.body.name)
        self.assertAlmostEqual(13599840256, orbit.apoapsis)
        self.assertAlmostEqual(13599840256, orbit.periapsis)
        self.assertAlmostEqual(13599840256 - 261600000, orbit.apoapsis_altitude)
        self.assertAlmostEqual(13599840256 - 261600000, orbit.periapsis_altitude)
        self.assertAlmostEqual(13599840256, orbit.semi_major_axis)
        self.assertAlmostEqual(13599840256, orbit.semi_minor_axis)
        self.assertAlmostEqual(13599840256, orbit.radius)
        self.assertAlmostEqual(9284.50, orbit.speed, places=1)
        self.check_radius_and_speed(body, orbit)
        self.check_orbital_energy(orbit)
        # self.check_time_to_apoapsis_and_periapsis(body, orbit)
        self.assertIsNaN(orbit.time_to_soi_change)
        self.assertAlmostEqual(0, orbit.eccentricity)
        self.assertAlmostEqual(0, orbit.inclination)
        self.assertAlmostEqual(0, orbit.longitude_of_ascending_node)
        self.assertAlmostEqual(0, orbit.argument_of_periapsis)
        self.check_anomalies(body, orbit)

    def test_minmus_orbiting_kerbin(self):
        body = self.space_center.bodies["Minmus"]
        orbit = body.orbit
        self.assertEqual("Kerbin", orbit.body.name)
        self.assertAlmostEqual(47000000, orbit.apoapsis)
        self.assertAlmostEqual(47000000, orbit.periapsis)
        self.assertAlmostEqual(47000000 - 600000, orbit.apoapsis_altitude)
        self.assertAlmostEqual(47000000 - 600000, orbit.periapsis_altitude)
        self.assertAlmostEqual(47000000, orbit.semi_major_axis)
        self.assertAlmostEqual(47000000, orbit.semi_minor_axis)
        self.assertAlmostEqual(47000000, orbit.radius)
        self.assertAlmostEqual(274.1, orbit.speed, delta=0.5)
        self.check_radius_and_speed(body, orbit)
        self.check_orbital_energy(orbit)
        # self.check_time_to_apoapsis_and_periapsis(body, orbit)
        self.assertIsNaN(orbit.time_to_soi_change)
        self.assertAlmostEqual(0, orbit.eccentricity)
        self.assertAlmostEqual(6 * (math.pi / 180), orbit.inclination)
        self.assertAlmostEqual(78 * (math.pi / 180), orbit.longitude_of_ascending_node)
        self.assertAlmostEqual(38 * (math.pi / 180), orbit.argument_of_periapsis)
        self.check_anomalies(body, orbit)

    def test_eeloo_orbiting_sun(self):
        body = self.space_center.bodies["Eeloo"]
        orbit = body.orbit
        self.assertEqual("Sun", orbit.body.name)
        self.assertAlmostEqual(113549713200, orbit.apoapsis)
        self.assertAlmostEqual(66687926800, orbit.periapsis)
        self.assertAlmostEqual(113549713200 - 261600000, orbit.apoapsis_altitude)
        self.assertAlmostEqual(66687926800 - 261600000, orbit.periapsis_altitude)
        sma = 0.5 * (113549713200 + 66687926800)
        ecc = 0.26
        self.assertAlmostEqual(sma, orbit.semi_major_axis)
        self.assertAlmostEqual(sma * math.sqrt(1 - (ecc * ecc)), orbit.semi_minor_axis)
        # self.check_radius_and_speed(body, orbit)
        # self.check_time_to_apoapsis_and_periapsis(body, orbit)
        self.assertIsNaN(orbit.time_to_soi_change)
        self.assertAlmostEqual(ecc, orbit.eccentricity)
        self.assertAlmostEqual(6.15 * (math.pi / 180), orbit.inclination)
        self.assertAlmostEqual(50 * (math.pi / 180), orbit.longitude_of_ascending_node)
        self.assertAlmostEqual(260 * (math.pi / 180), orbit.argument_of_periapsis)
        self.check_anomalies(body, orbit)

    def test_reference_plane(self):
        kerbin = self.space_center.bodies["Kerbin"]
        ref = kerbin.non_rotating_reference_frame
        normal = kerbin.orbit.reference_plane_normal(ref)
        direction = kerbin.orbit.reference_plane_direction(ref)
        self.assertAlmostEqual((0, 1, 0), normal)
        self.assertAlmostEqual((1, 0, 0), direction)


class TestClosestApproach(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        # Two coplanar circular orbits. The target is inner (and so faster) and
        # trails the active vessel, so it catches up to a close approach in the
        # future rather than at the current instant.
        cls.launch_vessel_from_vab("Basic")
        cls.remove_other_vessels()
        cls.set_orbit("Kerbin", 1600000, 0, 0, 0, 0, 0, 0)
        cls.launch_vessel_from_vab("Basic")
        cls.set_orbit("Kerbin", 1650000, 0, 0, 0, 0, 0.15, 0)
        cls.conn = cls.connect()
        cls.sc = cls.conn.space_center
        cls.vessel = cls.sc.active_vessel
        cls.other = next(v for v in cls.sc.vessels if v != cls.vessel)
        cls.orbit = cls.vessel.orbit
        cls.target = cls.other.orbit

    def test_next_closest_approach(self):
        approach = self.orbit.next_closest_approach(self.target)
        # Time is in the future and consistent with time_to
        self.assertGreater(approach.ut, self.sc.ut)
        self.assertGreater(approach.time_to, 0)
        self.assertAlmostEqual(approach.time_to, approach.ut - self.sc.ut, delta=1)
        # The objects genuinely approach: the closest distance is much smaller
        # than their initial separation
        self.assertGreater(approach.distance, 0)
        self.assertLess(approach.distance, 100000)
        # Agrees with the deprecated scalar helpers
        self.assertAlmostEqual(
            approach.ut, self.orbit.time_of_closest_approach(self.target), delta=1
        )
        self.assertAlmostEqual(
            approach.distance,
            self.orbit.distance_at_closest_approach(self.target),
            delta=1,
        )
        # Both endpoints are vessels, not celestial bodies
        self.assertEqual(approach.vessel, self.vessel)
        self.assertIsNone(approach.body)
        self.assertEqual(approach.target_vessel, self.other)
        self.assertIsNone(approach.target_body)

    def test_target_body(self):
        # Approaching a celestial body: target_body is set, target_vessel is not,
        # while the approaching side is still this vessel
        approach = self.orbit.next_closest_approach(self.sc.bodies["Mun"].orbit)
        self.assertEqual(approach.vessel, self.vessel)
        self.assertIsNone(approach.body)
        self.assertIsNone(approach.target_vessel)
        self.assertEqual(approach.target_body, self.sc.bodies["Mun"])

    def test_relative_quantities(self):
        approach = self.orbit.next_closest_approach(self.target)
        frame = self.orbit.body.non_rotating_reference_frame
        pos = approach.position(frame)
        target_pos = approach.target_position(frame)
        rel_pos = approach.relative_position(frame)
        vel = approach.velocity(frame)
        target_vel = approach.target_velocity(frame)
        rel_vel = approach.relative_velocity(frame)
        # Relative quantities are the target relative to the orbiting object.
        # In a non-rotating frame they are the plain difference of the absolutes.
        for i in range(3):
            self.assertAlmostEqual(rel_pos[i], target_pos[i] - pos[i], delta=1)
            self.assertAlmostEqual(rel_vel[i], target_vel[i] - vel[i], delta=0.1)
        # Distance and relative speed are the magnitudes, and frame independent
        self.assertAlmostEqual(approach.distance, norm(rel_pos), delta=1)
        self.assertAlmostEqual(approach.relative_speed, norm(rel_vel), delta=0.1)
        self.assertAlmostEqual(
            norm(rel_pos),
            norm(approach.relative_position()),  # default frame
            delta=1,
        )

    def test_absolute_velocities(self):
        """The velocities are each object's own, not merely consistent with each
        other.

        Both orbits are circular, so the speed at the approach is the circular
        speed at that radius, which is computed here from the gravitational
        parameter alone and owes nothing to the reference frame machinery. The
        relative quantities are differences and so cannot see an offset common to
        both velocities, which is how these came to carry the velocity of the
        frame the active vessel is in, some 9284 m/s around Kerbin."""
        approach = self.orbit.next_closest_approach(self.target)
        frame = self.orbit.body.non_rotating_reference_frame
        mu = self.orbit.body.gravitational_parameter
        for orbit, speed in (
            (self.orbit, norm(approach.velocity(frame))),
            (self.target, norm(approach.target_velocity(frame))),
        ):
            expected = math.sqrt(
                mu * ((2 / orbit.radius_at(approach.ut)) - (1 / orbit.semi_major_axis))
            )
            self.assertAlmostEqual(expected, speed, delta=1)

    def test_asking_for_the_same_approach_twice(self):
        # The object names which of the approaches between the two orbits it is, and
        # reads the time and distance from the orbits as they are now. Asking for the
        # same one again gives back the same object, rather than filling the object
        # store with a snapshot per call.
        first = self.orbit.next_closest_approach(self.target)
        before = self.conn.testing_tools.object_store_size
        for _ in range(5):
            self.wait(0.1)
            self.assertEqual(first, self.orbit.next_closest_approach(self.target))
        self.assertEqual(before, self.conn.testing_tools.object_store_size)
        # The first of the approaches per orbital period is the same one
        self.assertEqual(first, self.orbit.closest_approaches(self.target, 3)[0])

    def test_closest_approaches(self):
        approaches = self.orbit.closest_approaches(self.target, 3)
        self.assertEqual(3, len(approaches))
        times = [approach.ut for approach in approaches]
        # Strictly increasing in time, all in the future
        self.assertGreater(times[0], self.sc.ut)
        for earlier, later in zip(times, times[1:]):
            self.assertGreater(later, earlier)
        # The first matches next_closest_approach
        first = self.orbit.next_closest_approach(self.target)
        self.assertAlmostEqual(approaches[0].ut, first.ut, delta=1)
        self.assertAlmostEqual(approaches[0].distance, first.distance, delta=1)


class TestCreateFromPositionAndVelocity(krpctest.TestCase):
    """Orbits built from state vectors rather than read off a game object.

    The checks that pin down the conversion into the game's state-vector
    convention are built from chosen numbers rather than from a vessel's live
    state, so that the physics frame the game advances between two RPCs cannot
    move the answer. In Kerbin's non-rotating frame the y-axis points at the
    north pole and the x and z axes lie in the equatorial plane, so a position
    on the z-axis with a velocity along y is a polar orbit and the same position
    with a velocity along x is an equatorial one. Get the body-relative
    subtraction or the axis swap wrong and neither comes out right."""

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.remove_other_vessels()
        cls.set_orbit("Kerbin", 800000, 0.15, 25, 40, 70, 1.2, 0)
        cls.space_center = cls.connect().space_center
        cls.vessel = cls.space_center.active_vessel
        cls.kerbin = cls.space_center.bodies["Kerbin"]
        cls.frame = cls.kerbin.non_rotating_reference_frame

    def create(self, position, velocity, reference_frame=None):
        return self.space_center.Orbit.create_from_position_and_velocity(
            self.kerbin,
            position,
            velocity,
            self.space_center.ut,
            reference_frame or self.frame,
        )

    def circular_speed(self, radius):
        return math.sqrt(self.kerbin.gravitational_parameter / radius)

    def test_circular_polar_orbit(self):
        radius = 1000000
        orbit = self.create((0, 0, radius), (0, self.circular_speed(radius), 0))
        self.assertEqual("Kerbin", orbit.body.name)
        self.assertAlmostEqual(radius, orbit.semi_major_axis, delta=1)
        self.assertAlmostEqual(radius, orbit.apoapsis, delta=1)
        self.assertAlmostEqual(radius, orbit.periapsis, delta=1)
        self.assertAlmostEqual(0, orbit.eccentricity, places=6)
        self.assertAlmostEqual(math.pi / 2, orbit.inclination, places=6)
        period = (
            2 * math.pi * math.sqrt(radius**3 / self.kerbin.gravitational_parameter)
        )
        self.assertAlmostEqual(period, orbit.period, delta=1)

    def test_circular_equatorial_orbit(self):
        radius = 1200000
        orbit = self.create((0, 0, radius), (self.circular_speed(radius), 0, 0))
        self.assertAlmostEqual(radius, orbit.semi_major_axis, delta=1)
        self.assertAlmostEqual(0, orbit.eccentricity, places=6)
        # An equatorial orbit is at inclination 0 or pi depending on which way
        # round it goes; either way it stays in the plane of the equator.
        self.assertAlmostEqual(0, math.sin(orbit.inclination), places=6)

    def test_eccentric_orbit(self):
        """A speed below circular at the given radius puts apoapsis there."""
        radius = 2000000
        orbit = self.create((0, 0, radius), (0.8 * self.circular_speed(radius), 0, 0))
        self.assertAlmostEqual(radius, orbit.apoapsis, delta=1)
        self.assertLess(orbit.periapsis, radius)
        self.assertAlmostEqual(0.36, orbit.eccentricity, places=6)

    def test_escape_trajectory(self):
        radius = 1000000
        escape_speed = math.sqrt(2 * self.kerbin.gravitational_parameter / radius)
        orbit = self.create((0, 0, radius), (escape_speed * 1.5, 0, 0))
        self.assertGreater(orbit.eccentricity, 1)
        self.assertLess(orbit.semi_major_axis, 0)
        self.assertIsNaN(orbit.time_to_soi_change)

    def test_no_soi_change(self):
        """A constructed orbit is a single conic, with no following patch.

        The orbit is far outside Kerbin's sphere of influence, where a real
        vessel would have left it long ago."""
        radius = 500000000
        orbit = self.create((0, 0, radius), (0, self.circular_speed(radius), 0))
        self.assertGreater(radius, self.kerbin.sphere_of_influence)
        self.assertIsNaN(orbit.time_to_soi_change)
        self.assertIsNone(orbit.next_orbit)

    def test_velocity_at_matches_speed(self):
        """velocity_at is the orbital velocity, so its magnitude is the speed."""
        radius = 1500000
        orbit = self.create((0, 0, radius), (0.9 * self.circular_speed(radius), 0, 0))
        ut = self.space_center.ut
        for offset in (0, 300, 1200):
            at = ut + offset
            speed = norm(orbit.velocity_at(at, self.frame))
            expected = math.sqrt(
                self.kerbin.gravitational_parameter
                * ((2 / orbit.radius_at(at)) - (1 / orbit.semi_major_axis))
            )
            self.assertAlmostEqual(expected, speed, delta=0.1)

    def test_position_at_matches_radius(self):
        radius = 1500000
        orbit = self.create((0, 0, radius), (0.9 * self.circular_speed(radius), 0, 0))
        ut = self.space_center.ut
        for offset in (0, 300, 1200):
            at = ut + offset
            position = orbit.position_at(at, self.frame)
            self.assertAlmostEqual(orbit.radius_at(at), norm(position), delta=0.1)

    def test_state_vectors_are_recovered(self):
        """The orbit passes through the position and velocity it was built from."""
        radius = 1400000
        position = (0, 0, radius)
        velocity = (0.85 * self.circular_speed(radius), 0, 0)
        ut = self.space_center.ut
        orbit = self.space_center.Orbit.create_from_position_and_velocity(
            self.kerbin, position, velocity, ut, self.frame
        )
        self.assertAlmostEqual(position, orbit.position_at(ut, self.frame), delta=1)
        self.assertAlmostEqual(velocity, orbit.velocity_at(ut, self.frame), delta=0.1)

    def test_rotating_construction_frame(self):
        """The frame the state vectors are given in is taken into account.

        Kerbin's rotating frame turns with the surface, so the same numbers given
        in it describe a different orbit: the surface motion at this radius is a
        sixth of orbital speed, which takes the orbit well away from circular."""
        radius = 1000000
        speed = self.circular_speed(radius)
        inertial = self.create((0, 0, radius), (speed, 0, 0))
        rotating = self.create(
            (0, 0, radius), (speed, 0, 0), self.kerbin.reference_frame
        )
        self.assertAlmostEqual(0, inertial.eccentricity, places=6)
        self.assertGreater(rotating.eccentricity, 0.1)

    def test_default_reference_frame(self):
        """Omitting the frame uses the body's non-rotating frame."""
        radius = 1000000
        velocity = (0, self.circular_speed(radius), 0)
        explicit = self.create((0, 0, radius), velocity, self.frame)
        default = self.space_center.Orbit.create_from_position_and_velocity(
            self.kerbin, (0, 0, radius), velocity, self.space_center.ut
        )
        self.assertAlmostEqual(
            explicit.semi_major_axis, default.semi_major_axis, delta=1
        )
        self.assertAlmostEqual(explicit.inclination, default.inclination, places=6)

    def test_round_trip_from_vessel(self):
        """An orbit built from a vessel's own state vectors is that vessel's orbit.

        The position, velocity and time come from separate RPCs, so the game
        advances a physics frame or two between them and the state is not quite
        one the vessel was ever in. The tolerances allow for that."""
        expected = self.vessel.orbit
        orbit = self.space_center.Orbit.create_from_position_and_velocity(
            self.kerbin,
            self.vessel.position(self.frame),
            self.vessel.velocity(self.frame),
            self.space_center.ut,
            self.frame,
        )
        self.assertEqual("Kerbin", orbit.body.name)
        self.assertAlmostEqual(expected.apoapsis, orbit.apoapsis, delta=2000)
        self.assertAlmostEqual(expected.periapsis, orbit.periapsis, delta=2000)
        self.assertAlmostEqual(
            expected.semi_major_axis, orbit.semi_major_axis, delta=2000
        )
        self.assertAlmostEqual(expected.eccentricity, orbit.eccentricity, places=3)
        self.assertAlmostEqual(expected.inclination, orbit.inclination, places=3)
        self.assertAlmostEqual(
            expected.longitude_of_ascending_node,
            orbit.longitude_of_ascending_node,
            places=2,
        )
        self.assertAlmostEqual(
            expected.argument_of_periapsis, orbit.argument_of_periapsis, places=2
        )
        self.assertAlmostEqual(expected.period, orbit.period, delta=5)

    def test_position_at_the_center_of_the_body(self):
        """A position at the center of the body is not on any orbit."""
        with self.assertRaises(ValueError):
            self.space_center.Orbit.create_from_position_and_velocity(
                self.kerbin, (0, 0, 0), (0, 2000, 0), self.space_center.ut, self.frame
            )


class TestCreateFromOrbitalElements(krpctest.TestCase):
    """Orbits built from orbital elements rather than from a position and velocity.

    The load-bearing check is against Orbit.CreateFromPositionAndVelocity, which
    the tests above pin down independently: an orbit built that way and rebuilt
    from the elements it reports must describe the same trajectory. Reading the
    elements back catches the radians-to-degrees conversion on its own, since the
    game holds the three orientation angles in degrees but the mean anomaly in
    radians, and this reports all four in radians."""

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.remove_other_vessels()
        cls.set_orbit("Kerbin", 800000, 0.15, 25, 40, 70, 1.2, 0)
        cls.space_center = cls.connect().space_center
        cls.vessel = cls.space_center.active_vessel
        cls.kerbin = cls.space_center.bodies["Kerbin"]
        cls.frame = cls.kerbin.non_rotating_reference_frame

    DEFAULT_ELEMENTS = {
        "semi_major_axis": 1200000,
        "eccentricity": 0.2,
        "inclination": 0.4,
        "longitude_of_ascending_node": 1.1,
        "argument_of_periapsis": 2.3,
        "mean_anomaly_at_epoch": 0.7,
        "epoch": None,
    }

    def create(self, **overrides):
        """An orbit built from a fixed set of elements, with any of them replaced.

        An epoch of None, which is the default, means the current universal
        time."""
        elements = dict(self.DEFAULT_ELEMENTS, **overrides)
        epoch = elements["epoch"]
        if epoch is None:
            epoch = self.space_center.ut
        return self.space_center.Orbit.create_from_orbital_elements(
            self.kerbin,
            elements["semi_major_axis"],
            elements["eccentricity"],
            elements["inclination"],
            elements["longitude_of_ascending_node"],
            elements["argument_of_periapsis"],
            elements["mean_anomaly_at_epoch"],
            epoch,
        )

    def test_elements_are_recovered(self):
        """The orbit reports back the elements it was built from.

        The angles would come back a factor of 180/pi out if they were handed to
        the game as radians, since it holds them in degrees."""
        epoch = self.space_center.ut
        orbit = self.create(epoch=epoch)
        self.assertEqual("Kerbin", orbit.body.name)
        self.assertAlmostEqual(1200000, orbit.semi_major_axis, delta=1)
        self.assertAlmostEqual(0.2, orbit.eccentricity, places=6)
        self.assertAlmostEqual(0.4, orbit.inclination, places=6)
        self.assertAlmostEqual(1.1, orbit.longitude_of_ascending_node, places=6)
        self.assertAlmostEqual(2.3, orbit.argument_of_periapsis, places=6)
        self.assertAlmostEqual(0.7, orbit.mean_anomaly_at_epoch, places=6)
        self.assertAlmostEqual(epoch, orbit.epoch, places=3)

    def test_shape_follows_from_the_elements(self):
        """Apoapsis and periapsis are the semi-major axis scaled by the
        eccentricity, so the two shape elements reach the orbit the right way
        round."""
        orbit = self.create(semi_major_axis=1200000, eccentricity=0.2)
        self.assertAlmostEqual(1200000 * 1.2, orbit.apoapsis, delta=1)
        self.assertAlmostEqual(1200000 * 0.8, orbit.periapsis, delta=1)

    def test_polar_orbit(self):
        """An inclination of a quarter turn takes the orbit over the poles.

        In Kerbin's non-rotating frame the y-axis points at the north pole, so a
        polar orbit reaches nearly its full radius along y and an equatorial one
        stays near zero there."""
        polar = self.create(eccentricity=0, inclination=math.pi / 2)
        equatorial = self.create(eccentricity=0, inclination=0)
        ut = self.space_center.ut
        for orbit, expected in ((polar, 1), (equatorial, 0)):
            reach = max(
                abs(orbit.position_at(ut + t, self.frame)[1]) / orbit.semi_major_axis
                for t in range(0, int(orbit.period), max(1, int(orbit.period) // 32))
            )
            self.assertAlmostEqual(expected, reach, delta=0.05)

    def test_matches_an_orbit_built_from_a_position_and_velocity(self):
        """Rebuilding an orbit from the elements it reports gives the same
        trajectory, checked as position and velocity over a range of times."""
        radius = 1500000
        speed = 0.9 * math.sqrt(self.kerbin.gravitational_parameter / radius)
        ut = self.space_center.ut
        expected = self.space_center.Orbit.create_from_position_and_velocity(
            self.kerbin, (0, 0, radius), (speed, 0, 0), ut, self.frame
        )
        orbit = self.space_center.Orbit.create_from_orbital_elements(
            self.kerbin,
            expected.semi_major_axis,
            expected.eccentricity,
            expected.inclination,
            expected.longitude_of_ascending_node,
            expected.argument_of_periapsis,
            expected.mean_anomaly_at_epoch,
            expected.epoch,
        )
        for offset in (0, 300, 1200, 3000):
            at = ut + offset
            self.assertAlmostEqual(
                expected.position_at(at, self.frame),
                orbit.position_at(at, self.frame),
                delta=1,
            )
            self.assertAlmostEqual(
                expected.velocity_at(at, self.frame),
                orbit.velocity_at(at, self.frame),
                delta=0.1,
            )

    def test_round_trip_from_vessel(self):
        """An orbit built from a vessel's own elements is that vessel's orbit.

        Every element is read off the one orbit, so unlike building from a
        position and velocity there is no physics frame separating the inputs and
        the tolerances can be tight."""
        expected = self.vessel.orbit
        orbit = self.space_center.Orbit.create_from_orbital_elements(
            self.kerbin,
            expected.semi_major_axis,
            expected.eccentricity,
            expected.inclination,
            expected.longitude_of_ascending_node,
            expected.argument_of_periapsis,
            expected.mean_anomaly_at_epoch,
            expected.epoch,
        )
        for offset in (0, 600, 2400):
            at = self.space_center.ut + offset
            self.assertAlmostEqual(
                expected.position_at(at, self.frame),
                orbit.position_at(at, self.frame),
                delta=1,
            )
        self.assertAlmostEqual(expected.period, orbit.period, delta=0.1)

    def test_mean_anomaly_is_measured_at_the_epoch(self):
        """The mean anomaly is the one given at the epoch, and advances at the
        mean motion from there: a quarter of a period on is a quarter turn."""
        epoch = self.space_center.ut
        orbit = self.create(mean_anomaly_at_epoch=0.7, epoch=epoch)
        self.assertAlmostEqual(0.7, orbit.mean_anomaly, places=6)
        self.assertAlmostEqual(
            0.7 + math.pi / 2,
            orbit.mean_anomaly_at_ut(epoch + orbit.period / 4),
            places=4,
        )

    def test_hyperbolic_orbit(self):
        """A negative semi-major axis with an eccentricity above one is a
        hyperbola, and like every constructed orbit it has no following patch."""
        orbit = self.create(semi_major_axis=-1000000, eccentricity=1.5)
        self.assertAlmostEqual(1.5, orbit.eccentricity, places=6)
        self.assertLess(orbit.semi_major_axis, 0)
        self.assertIsNaN(orbit.time_to_soi_change)
        self.assertIsNone(orbit.next_orbit)

    def test_negative_eccentricity(self):
        with self.assertRaises(ValueError):
            self.create(eccentricity=-0.1)

    def test_parabolic_orbit(self):
        """A parabola has no semi-major axis, so it cannot be described."""
        with self.assertRaises(ValueError):
            self.create(eccentricity=1)

    def test_ellipse_with_a_negative_semi_major_axis(self):
        with self.assertRaises(ValueError):
            self.create(semi_major_axis=-1000000, eccentricity=0.2)

    def test_hyperbola_with_a_positive_semi_major_axis(self):
        with self.assertRaises(ValueError):
            self.create(semi_major_axis=1000000, eccentricity=1.5)


if __name__ == "__main__":
    unittest.main()
