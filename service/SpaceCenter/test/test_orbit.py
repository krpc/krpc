import unittest
import math
import krpctest
from krpctest.geometry import norm, compute_position


# TODO: fix commented out test cases
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
            orbit.body.gravitational_parameter *
            ((2/radius) - (1/orbit.semi_major_axis)))
        self.assertAlmostEqual(speed, orbit.speed, delta=1)

    def check_anomalies(self, obj, orbit):
        g = self.space_center.g
        ut = self.space_center.ut
        mean_anomaly_at_epoch = orbit.mean_anomaly_at_epoch
        epoch = orbit.epoch

        # Compute mean anomaly using Kepler's equation
        mean_anomaly = (orbit.eccentric_anomaly -
                        (orbit.eccentricity *
                         math.sin(orbit.eccentric_anomaly))) % (2*math.pi)
        self.assertAlmostEqual(mean_anomaly, orbit.mean_anomaly, places=2)

        # Compute mean anomaly using mean motion and time since epoch
        mean_motion = math.sqrt(
            (g * (orbit.body.mass + obj.mass)) / (orbit.semi_major_axis ** 3))
        delta_t = ut - epoch
        mean_anomaly = (mean_anomaly_at_epoch +
                        (mean_motion * delta_t)) % (2*math.pi)
        self.assertAlmostEqual(mean_anomaly, orbit.mean_anomaly, places=2)

    def check_time_to_apoapsis_and_periapsis(self, obj, orbit):
        # Compute the time to apoapsis and periapsis using mean motion
        g = self.space_center.g
        mean_motion = math.sqrt(
            (g * (orbit.body.mass + obj.mass)) / (orbit.semi_major_axis ** 3))
        time_since_periapsis = orbit.mean_anomaly / mean_motion
        time_to_periapsis = orbit.period - time_since_periapsis
        time_to_apoapsis = (orbit.period / 2) - time_since_periapsis
        if time_to_apoapsis < 0:
            time_to_apoapsis += orbit.period

        self.assertAlmostEqual(
            time_to_apoapsis, orbit.time_to_apoapsis, delta=2)
        self.assertAlmostEqual(
            time_to_periapsis, orbit.time_to_periapsis, delta=2)

    def test_fix(self):
        self.set_circular_orbit('Kerbin', 100000)
        vessel = self.space_center.active_vessel
        orbit = vessel.orbit
        self.assertAlmostEqual(0, orbit.eccentricity, places=1)
        self.assertAlmostEqual(0, orbit.inclination, places=1)
        # self.assertAlmostEqual(2,
        #                        orbit.longitude_of_ascending_node, places=1)
        # self.assertAlmostEqual(0, orbit.argument_of_periapsis, places=1)
        # self.assertAlmostEqual(0, orbit.mean_anomaly_at_epoch, places=1)
        # self.assertAlmostEqual(0, orbit.epoch, places=1)

    def test_vessel_orbiting_kerbin(self):
        self.set_circular_orbit('Kerbin', 100000)
        vessel = self.space_center.active_vessel
        orbit = vessel.orbit
        self.assertEqual('Kerbin', orbit.body.name)
        self.assertAlmostEqual(100000 + 600000, orbit.apoapsis, delta=50)
        self.assertAlmostEqual(100000 + 600000, orbit.periapsis, delta=50)
        self.assertAlmostEqual(100000, orbit.apoapsis_altitude, delta=50)
        self.assertAlmostEqual(100000, orbit.periapsis_altitude, delta=50)
        self.assertAlmostEqual(100000 + 600000,
                               orbit.semi_major_axis, delta=50)
        self.assertAlmostEqual(100000 + 600000,
                               orbit.semi_minor_axis, delta=50)
        self.assertAlmostEqual(700000, orbit.radius, delta=50)
        self.assertAlmostEqual(2246.1, orbit.speed, delta=1)
        self.check_radius_and_speed(vessel, orbit)
        # self.check_time_to_apoapsis_and_periapsis(vessel, orbit)
        self.assertIsNaN(orbit.time_to_soi_change)
        self.assertAlmostEqual(0, orbit.eccentricity, places=1)
        self.assertAlmostEqual(0, orbit.inclination, places=1)
        # self.assertAlmostEqual(0,
        #                        orbit.longitude_of_ascending_node, places=1)
        # self.assertAlmostEqual(0, orbit.argument_of_periapsis, places=1)
        # self.assertAlmostEqual(0, orbit.mean_anomaly_at_epoch, places=1)
        # self.assertAlmostEqual(0, orbit.epoch, places=1)
        # self.check_anomalies(vessel, orbit)
        self.assertIsNone(orbit.next_orbit)

    def test_vessel_orbiting_bop(self):
        self.set_orbit('Bop', 320000, 0.18, 27, 38, 241, 2.3, 0)
        vessel = self.space_center.active_vessel
        orbit = vessel.orbit
        self.assertEqual('Bop', orbit.body.name)
        self.assertAlmostEqual(377600, orbit.apoapsis, delta=50)
        self.assertAlmostEqual(262400, orbit.periapsis, delta=50)
        self.assertAlmostEqual(377600 - 65000,
                               orbit.apoapsis_altitude, delta=50)
        self.assertAlmostEqual(262400 - 65000,
                               orbit.periapsis_altitude, delta=50)
        sma = 0.5 * (377600 + 262400)
        ecc = 0.18
        self.assertAlmostEqual(sma, orbit.semi_major_axis, delta=50)
        self.assertAlmostEqual(sma * math.sqrt(1 - (ecc*ecc)),
                               orbit.semi_minor_axis, delta=50)
        # self.check_radius_and_speed(vessel, orbit)
        self.check_time_to_apoapsis_and_periapsis(vessel, orbit)
        # self.assertIsNaN(orbit.time_to_soi_change)
        self.assertAlmostEqual(ecc, orbit.eccentricity, places=1)
        self.assertAlmostEqual(27 * (math.pi/180), orbit.inclination, places=1)
        # self.assertAlmostEqual(38 * (math.pi/180),
        #                        orbit.longitude_of_ascending_node, places=1)
        # self.assertAlmostEqual(241 * (math.pi/180),
        #                        orbit.argument_of_periapsis, places=1)
        # self.assertAlmostEqual(2.3, orbit.mean_anomaly_at_epoch, places=1)
        # self.assertAlmostEqual(0, orbit.epoch, places=1)
        self.check_anomalies(vessel, orbit)
        # self.assertNone(orbit.next_orbit)

    def test_vessel_orbiting_mun_on_escape_soi(self):
        self.set_orbit('Mun', 1800000, 0.52, 0, 13, 67, 6.25, 0)
        vessel = self.space_center.active_vessel
        orbit = vessel.orbit
        self.assertEqual('Mun', orbit.body.name)
        self.assertAlmostEqual(2736000, orbit.apoapsis, delta=100)
        self.assertAlmostEqual(864000, orbit.periapsis, delta=50)
        self.assertAlmostEqual(2736000 - 200000,
                               orbit.apoapsis_altitude, delta=100)
        self.assertAlmostEqual(864000 - 200000,
                               orbit.periapsis_altitude, delta=50)
        sma = (0.5 * (2736000 + 864000))
        ecc = 0.52
        self.assertAlmostEqual(sma, orbit.semi_major_axis, delta=50)
        self.assertAlmostEqual(sma * math.sqrt(1 - (ecc*ecc)),
                               orbit.semi_minor_axis, delta=50)
        # self.check_radius_and_speed(vessel, orbit)
        # self.check_time_to_apoapsis_and_periapsis(vessel, orbit)
        # self.assertAlmostEqual(17414, orbit.time_to_soi_change,delta=5)
        self.assertAlmostEqual(ecc, orbit.eccentricity, places=1)
        self.assertAlmostEqual(0, orbit.inclination, places=1)
        # self.assertAlmostEqual(13 * (math.pi/180),
        #                        orbit.longitude_of_ascending_node, places=1)
        # self.assertAlmostEqual(67 * (math.pi/180),
        #                        orbit.argument_of_periapsis, places=1)
        # self.assertAlmostEqual(6.25, orbit.mean_anomaly_at_epoch, places=1)
        # self.assertAlmostEqual(0, orbit.epoch, places=1)
        # self.check_anomalies(vessel, orbit)
        self.assertIsNotNone(orbit.next_orbit)

        orbit = orbit.next_orbit
        self.assertEqual('Kerbin', orbit.body.name)

    def test_vessel_orbiting_minmus_on_parabolic_arc(self):
        self.set_orbit('Minmus', 80000, 3, 0, 0, 0, 0, 0)
        vessel = self.space_center.active_vessel
        orbit = vessel.orbit
        self.assertEqual('Minmus', orbit.body.name)
        self.assertAlmostEqual(-320000, orbit.apoapsis, delta=50)
        self.assertAlmostEqual(160000, orbit.periapsis, delta=50)
        self.assertAlmostEqual(-320000 - 60000,
                               orbit.apoapsis_altitude, delta=50)
        self.assertAlmostEqual(160000 - 60000,
                               orbit.periapsis_altitude, delta=50)
        sma = (0.5 * (-320000 + 160000))
        ecc = 3
        self.assertAlmostEqual(sma, orbit.semi_major_axis, delta=50)
        self.assertIsNaN(orbit.semi_minor_axis)
        # self.check_radius_and_speed(vessel, orbit)
        # self.check_time_to_apoapsis_and_periapsis(vessel, orbit)
        # self.assertAlmostEqual(12884, orbit.time_to_soi_change, delta=5)
        self.assertAlmostEqual(ecc, orbit.eccentricity, places=1)
        self.assertAlmostEqual(0, orbit.inclination, places=1)
        # self.assertAlmostEqual(0,
        #                        orbit.longitude_of_ascending_node, places=1)
        # self.assertAlmostEqual(0, orbit.argument_of_periapsis, places=1)
        # self.assertAlmostEqual(0, orbit.mean_anomaly_at_epoch, places=1)
        # self.assertAlmostEqual(0, orbit.epoch, places=1)
        # self.check_anomalies(vessel, orbit)
        self.assertIsNotNone(orbit.next_orbit)

        orbit = orbit.next_orbit
        self.assertEqual('Kerbin', orbit.body.name)

    def test_sun_orbit(self):
        sun = self.space_center.bodies['Sun']
        self.assertIsNone(sun.orbit)

    def test_kerbin_orbiting_sun(self):
        body = self.space_center.bodies['Kerbin']
        orbit = body.orbit
        self.assertEqual('Sun', orbit.body.name)
        self.assertAlmostEqual(13599840256, orbit.apoapsis)
        self.assertAlmostEqual(13599840256, orbit.periapsis)
        self.assertAlmostEqual(13599840256 - 261600000,
                               orbit.apoapsis_altitude)
        self.assertAlmostEqual(13599840256 - 261600000,
                               orbit.periapsis_altitude)
        self.assertAlmostEqual(13599840256, orbit.semi_major_axis)
        self.assertAlmostEqual(13599840256, orbit.semi_minor_axis)
        self.assertAlmostEqual(13599840256, orbit.radius)
        self.assertAlmostEqual(9284.50, orbit.speed, places=1)
        self.check_radius_and_speed(body, orbit)
        # self.check_time_to_apoapsis_and_periapsis(body, orbit)
        self.assertIsNaN(orbit.time_to_soi_change)
        self.assertAlmostEqual(0, orbit.eccentricity)
        self.assertAlmostEqual(0, orbit.inclination)
        self.assertAlmostEqual(0, orbit.longitude_of_ascending_node)
        self.assertAlmostEqual(0, orbit.argument_of_periapsis)
        self.check_anomalies(body, orbit)

    def test_minmus_orbiting_kerbin(self):
        body = self.space_center.bodies['Minmus']
        orbit = body.orbit
        self.assertEqual('Kerbin', orbit.body.name)
        self.assertAlmostEqual(47000000, orbit.apoapsis)
        self.assertAlmostEqual(47000000, orbit.periapsis)
        self.assertAlmostEqual(47000000 - 600000, orbit.apoapsis_altitude)
        self.assertAlmostEqual(47000000 - 600000, orbit.periapsis_altitude)
        self.assertAlmostEqual(47000000, orbit.semi_major_axis)
        self.assertAlmostEqual(47000000, orbit.semi_minor_axis)
        self.assertAlmostEqual(47000000, orbit.radius)
        self.assertAlmostEqual(274.1, orbit.speed, delta=0.5)
        self.check_radius_and_speed(body, orbit)
        # self.check_time_to_apoapsis_and_periapsis(body, orbit)
        self.assertIsNaN(orbit.time_to_soi_change)
        self.assertAlmostEqual(0, orbit.eccentricity)
        self.assertAlmostEqual(6 * (math.pi/180), orbit.inclination)
        self.assertAlmostEqual(78 * (math.pi/180),
                               orbit.longitude_of_ascending_node)
        self.assertAlmostEqual(38 * (math.pi/180),
                               orbit.argument_of_periapsis)
        self.check_anomalies(body, orbit)

    def test_eeloo_orbiting_sun(self):
        body = self.space_center.bodies['Eeloo']
        orbit = body.orbit
        self.assertEqual('Sun', orbit.body.name)
        self.assertAlmostEqual(113549713200, orbit.apoapsis)
        self.assertAlmostEqual(66687926800, orbit.periapsis)
        self.assertAlmostEqual(113549713200 - 261600000,
                               orbit.apoapsis_altitude)
        self.assertAlmostEqual(66687926800 - 261600000,
                               orbit.periapsis_altitude)
        sma = 0.5 * (113549713200 + 66687926800)
        ecc = 0.26
        self.assertAlmostEqual(sma, orbit.semi_major_axis)
        self.assertAlmostEqual(sma * math.sqrt(1 - (ecc*ecc)),
                               orbit.semi_minor_axis)
        # self.check_radius_and_speed(body, orbit)
        # self.check_time_to_apoapsis_and_periapsis(body, orbit)
        self.assertIsNaN(orbit.time_to_soi_change)
        self.assertAlmostEqual(ecc, orbit.eccentricity)
        self.assertAlmostEqual(6.15 * (math.pi/180), orbit.inclination)
        self.assertAlmostEqual(50 * (math.pi/180),
                               orbit.longitude_of_ascending_node)
        self.assertAlmostEqual(260 * (math.pi/180),
                               orbit.argument_of_periapsis)
        self.check_anomalies(body, orbit)

    def test_reference_plane(self):
        kerbin = self.space_center.bodies['Kerbin']
        ref = kerbin.non_rotating_reference_frame
        normal = kerbin.orbit.reference_plane_normal(ref)
        direction = kerbin.orbit.reference_plane_direction(ref)
        self.assertAlmostEqual((0, 1, 0), normal)
        self.assertAlmostEqual((1, 0, 0), direction)


if __name__ == '__main__':
    unittest.main()
