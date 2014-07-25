import unittest
import testingtools
from mathtools import vector, norm, dot
from geometrytools import compute_position
import math
import krpc
import time

class TestOrbit(testingtools.TestCase):

    @classmethod
    def setUpClass(cls):
        testingtools.new_save()
        testingtools.launch_vessel_from_vab('Basic')
        cls.conn = krpc.connect()

    def check_radius_and_speed(self, obj, orbit):
        # Compute position from orbital elements
        pos = compute_position(obj, orbit.body.non_rotating_reference_frame)
        # Compute radius from position
        radius = norm(pos) * 1000000
        self.assertClose(radius, orbit.radius, error=0.5)
        # Compute speed from radius
        speed = math.sqrt(orbit.body.gravitational_parameter * ((2/radius)-(1/orbit.semi_major_axis)))
        self.assertClose(speed, orbit.speed, error=0.5)

    def check_anomalies(self, obj, orbit):
        mean_anomaly_at_epoch = orbit.mean_anomaly_at_epoch
        epoch = orbit.epoch

        # Compute mean anomaly using Kepler's equation
        mean_anomaly = (orbit.eccentric_anomaly - (orbit.eccentricity * math.sin(orbit.eccentric_anomaly))) % (2*math.pi)
        self.assertClose(mean_anomaly, orbit.mean_anomaly)

        # Compute mean anomaly using mean motion and time since epoch
        mean_motion = math.sqrt((self.conn.space_center.g * (orbit.body.mass + obj.mass)) / (orbit.semi_major_axis ** 3))
        t = self.conn.space_center.ut - orbit.epoch
        mean_anomaly = (orbit.mean_anomaly_at_epoch + (mean_motion * t)) % (2*math.pi)
        self.assertClose(mean_anomaly, orbit.mean_anomaly)

    def check_time_to_apoapsis_and_periapsis(self, obj, orbit):
        # Compute the time to apoapsis and periapsis using mean motion
        mean_motion = math.sqrt((self.conn.space_center.g * (orbit.body.mass + obj.mass)) / (orbit.semi_major_axis ** 3))
        time_since_periapsis = orbit.mean_anomaly / mean_motion
        time_to_periapsis = orbit.period - time_since_periapsis
        time_to_apoapsis = (orbit.period / 2) - time_since_periapsis
        if time_to_apoapsis < 0:
            time_to_apoapsis += orbit.period

        self.assertClose(time_to_apoapsis, orbit.time_to_apoapsis, error=1)
        self.assertClose(time_to_periapsis, orbit.time_to_periapsis, error=1)

    def test_vessel_orbiting_kerbin(self):
        testingtools.set_circular_orbit('Kerbin', 100000)
        vessel = self.conn.space_center.active_vessel
        orbit = vessel.orbit
        self.assertEqual('Kerbin', orbit.body.name)
        self.assertClose(100000 + 600000, orbit.apoapsis, error=10)
        self.assertClose(100000 + 600000, orbit.periapsis, error=10)
        self.assertClose(100000,  orbit.apoapsis_altitude, error=10)
        self.assertClose(100000, orbit.periapsis_altitude, error=10)
        self.assertClose(100000 + 600000, orbit.semi_major_axis, error=10)
        self.assertClose(100000 + 600000, orbit.semi_minor_axis, error=10)
        self.assertClose(700000, orbit.radius, error=10)
        self.assertClose(2246.1, orbit.speed, error=1)
        self.check_radius_and_speed(vessel, orbit)
        self.check_time_to_apoapsis_and_periapsis(vessel, orbit)
        self.assertTrue(math.isnan(orbit.time_to_soi_change))
        self.assertClose(0, orbit.eccentricity, error=0.1)
        self.assertClose(0, orbit.inclination, error=0.1)
        self.assertClose(0, orbit.longitude_of_ascending_node, error=0.1)
        self.assertClose(0, orbit.argument_of_periapsis, error=0.1)
        self.assertClose(0, orbit.mean_anomaly_at_epoch, error=0.1)
        self.assertClose(0, orbit.epoch, error=0.1)
        self.check_anomalies(vessel, orbit)
        self.assertEqual(None, orbit.next_orbit)

    def test_vessel_orbiting_bop(self):
        testingtools.set_orbit('Bop', 320000, 0.18, 27, 38, 241, 2.3, 0)
        vessel = self.conn.space_center.active_vessel
        orbit = vessel.orbit
        self.assertEqual('Bop', orbit.body.name)
        self.assertClose(377600,  orbit.apoapsis, error=10)
        self.assertClose(262400 , orbit.periapsis, error=10)
        self.assertClose(377600 - 65000,  orbit.apoapsis_altitude, error=10)
        self.assertClose(262400 - 65000, orbit.periapsis_altitude, error=10)
        sma = 0.5 * (377600 + 262400)
        e = 0.18
        self.assertClose(sma, orbit.semi_major_axis, error=10)
        self.assertClose(sma * math.sqrt(1 - (e*e)), orbit.semi_minor_axis, error=10)
        #self.check_radius_and_speed(vessel, orbit)
        self.check_time_to_apoapsis_and_periapsis(vessel, orbit)
        self.assertTrue(math.isnan(orbit.time_to_soi_change))
        self.assertClose(e, orbit.eccentricity, error=0.1)
        self.assertClose(27 * (math.pi/180), orbit.inclination, error=0.1)
        self.assertClose(38 * (math.pi/180), orbit.longitude_of_ascending_node, error=0.1)
        self.assertClose(241 * (math.pi/180), orbit.argument_of_periapsis, error=0.1)
        self.assertClose(2.3, orbit.mean_anomaly_at_epoch, error=0.1)
        self.assertClose(0, orbit.epoch, error=0.1)
        self.check_anomalies(vessel, orbit)
        self.assertEqual(None, orbit.next_orbit)

    def test_vessel_orbiting_mun_on_escape_soi(self):
        testingtools.set_orbit('Mun', 1800000, 0.52, 0, 13, 67, 6.25, 0)
        vessel = self.conn.space_center.active_vessel
        orbit = vessel.orbit
        self.assertEqual('Mun', orbit.body.name)
        self.assertClose(2736000, orbit.apoapsis, error=10)
        self.assertClose(864000, orbit.periapsis, error=10)
        self.assertClose(2736000 - 200000, orbit.apoapsis_altitude, error=10)
        self.assertClose(864000 - 200000, orbit.periapsis_altitude, error=10)
        sma = (0.5 * (2736000 + 864000))
        e = 0.52
        self.assertClose(sma, orbit.semi_major_axis, error=10)
        self.assertClose(sma * math.sqrt(1 - (e*e)), orbit.semi_minor_axis, error=10)
        #self.check_radius_and_speed(vessel, orbit)
        self.check_time_to_apoapsis_and_periapsis(vessel, orbit)
        self.assertClose(17414, orbit.time_to_soi_change,error=5)
        self.assertClose(e, orbit.eccentricity, error=0.1)
        self.assertClose(0, orbit.inclination, error=0.1)
        self.assertClose(13 * (math.pi/180), orbit.longitude_of_ascending_node, error=0.1)
        self.assertClose(67 * (math.pi/180), orbit.argument_of_periapsis, error=0.1)
        self.assertClose(6.25, orbit.mean_anomaly_at_epoch, error=0.1)
        self.assertClose(0, orbit.epoch, error=0.1)
        self.check_anomalies(vessel, orbit)
        self.assertTrue(orbit.next_orbit is not None)

        orbit = orbit.next_orbit
        self.assertEqual('Kerbin', orbit.body.name)

    def test_vessel_orbiting_minmus_on_parabolic_arc(self):
        testingtools.set_orbit('Minmus', 80000, 3, 0, 0, 0, 0, 0)
        vessel = self.conn.space_center.active_vessel
        orbit = vessel.orbit
        self.assertEqual('Minmus', orbit.body.name)
        self.assertClose(-320000, orbit.apoapsis, error=10)
        self.assertClose(160000, orbit.periapsis, error=10)
        self.assertClose(-320000 - 60000, orbit.apoapsis_altitude, error=10)
        self.assertClose(160000 - 60000, orbit.periapsis_altitude, error=10)
        sma = (0.5 * (-320000 + 160000))
        e = 3
        self.assertClose(sma, orbit.semi_major_axis, error=10)
        self.assertTrue(math.isnan(orbit.semi_minor_axis))
        #self.check_radius_and_speed(vessel, orbit)
        #self.check_time_to_apoapsis_and_periapsis(vessel, orbit)
        #self.assertClose(12884, orbit.time_to_soi_change, error=5)
        self.assertClose(e, orbit.eccentricity, error=0.1)
        self.assertClose(0, orbit.inclination, error=0.1)
        self.assertClose(0, orbit.longitude_of_ascending_node, error=0.1)
        self.assertClose(0, orbit.argument_of_periapsis, error=0.1)
        self.assertClose(0, orbit.mean_anomaly_at_epoch, error=0.1)
        self.assertClose(0, orbit.epoch, error=0.1)
        #self.check_anomalies(vessel, orbit)
        self.assertTrue(orbit.next_orbit is not None)

        orbit = orbit.next_orbit
        self.assertEqual('Kerbin', orbit.body.name)

    def test_sun_orbit(self):
        sun = self.conn.space_center.bodies['Sun']
        self.assertIsNone(sun.orbit)

    def test_kerbin_orbiting_sun(self):
        body = self.conn.space_center.bodies['Kerbin']
        orbit = body.orbit
        self.assertEqual('Sun', orbit.body.name)
        self.assertClose(13599840256, orbit.apoapsis)
        self.assertClose(13599840256, orbit.periapsis)
        self.assertClose(13599840256 - 261600000, orbit.apoapsis_altitude)
        self.assertClose(13599840256 - 261600000, orbit.periapsis_altitude)
        self.assertClose(13599840256, orbit.semi_major_axis)
        self.assertClose(13599840256, orbit.semi_minor_axis)
        self.assertClose(13599840256, orbit.radius)
        self.assertClose(9284.5, orbit.speed)
        self.check_radius_and_speed(body, orbit)
        #self.check_time_to_apoapsis_and_periapsis(body, orbit)
        self.assertTrue(math.isnan(orbit.time_to_soi_change))
        self.assertClose(0, orbit.eccentricity)
        self.assertClose(0, orbit.inclination)
        self.assertClose(0, orbit.longitude_of_ascending_node)
        self.assertClose(0, orbit.argument_of_periapsis)
        self.check_anomalies(body, orbit)

    def test_minmus_orbiting_kerbin(self):
        body = self.conn.space_center.bodies['Minmus']
        orbit = body.orbit
        self.assertEqual('Kerbin', orbit.body.name)
        self.assertClose(47000000, orbit.apoapsis)
        self.assertClose(47000000, orbit.periapsis)
        self.assertClose(47000000 - 600000, orbit.apoapsis_altitude)
        self.assertClose(47000000 - 600000, orbit.periapsis_altitude)
        self.assertClose(47000000, orbit.semi_major_axis)
        self.assertClose(47000000, orbit.semi_minor_axis)
        self.assertClose(47000000, orbit.radius)
        self.assertClose(274.1, orbit.speed, error=0.5)
        self.check_radius_and_speed(body, orbit)
        #self.check_time_to_apoapsis_and_periapsis(body, orbit)
        self.assertTrue(math.isnan(orbit.time_to_soi_change))
        self.assertClose(0, orbit.eccentricity)
        self.assertClose(6 * (math.pi/180), orbit.inclination)
        self.assertClose(78 * (math.pi/180), orbit.longitude_of_ascending_node)
        self.assertClose(38 * (math.pi/180), orbit.argument_of_periapsis)
        self.check_anomalies(body, orbit)

    def test_eeloo_orbiting_sun(self):
        body = self.conn.space_center.bodies['Eeloo']
        orbit = body.orbit
        self.assertEqual('Sun', orbit.body.name)
        self.assertClose(113549713200, orbit.apoapsis)
        self.assertClose(66687926800, orbit.periapsis)
        self.assertClose(113549713200 - 261600000, orbit.apoapsis_altitude)
        self.assertClose(66687926800 - 261600000, orbit.periapsis_altitude)
        sma = 0.5 * (113549713200 + 66687926800)
        e = 0.26
        self.assertClose(sma, orbit.semi_major_axis)
        self.assertClose(sma * math.sqrt(1 - (e*e)), orbit.semi_minor_axis)
        #self.check_radius_and_speed(body, orbit)
        #self.check_time_to_apoapsis_and_periapsis(body, orbit)
        self.assertTrue(math.isnan(orbit.time_to_soi_change))
        self.assertClose(e, orbit.eccentricity)
        self.assertClose(6.15 * (math.pi/180), orbit.inclination)
        self.assertClose(50 * (math.pi/180), orbit.longitude_of_ascending_node)
        self.assertClose(260 * (math.pi/180), orbit.argument_of_periapsis)
        self.check_anomalies(body, orbit)

    def test_reference_plane(self):
        kerbin = self.conn.space_center.bodies['Kerbin']
        ref = kerbin.non_rotating_reference_frame
        normal = kerbin.orbit.reference_plane_normal(ref)
        direction = kerbin.orbit.reference_plane_direction(ref)
        self.assertClose((0,1,0), normal)
        self.assertClose((1,0,0), direction)

if __name__ == "__main__":
    unittest.main()
