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
        cls.sc = cls.connect().space_center
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


if __name__ == "__main__":
    unittest.main()
