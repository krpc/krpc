import unittest
import testingtools
from testingtools import load_save
from mathtools import vector, norm, dot
import math
import krpc

class TestOrbit(testingtools.TestCase):

    @classmethod
    def setUpClass(cls):
        load_save('basic')
        cls.conn = krpc.connect()

    def test_vessel_orbiting_kerbin(self):
        load_save('orbit-kerbin')
        ksp = krpc.connect()
        vessel = ksp.space_center.active_vessel
        orbit = vessel.orbit
        # inc   0
        # e     0
        # sma   70000
        # lan   0
        # w     0
        # mEp   0
        # epoch 0
        # body  Kerbin
        self.assertEqual('Kerbin', orbit.body.name)
        self.assertClose(100000 + 600000, orbit.apoapsis, error=10)
        self.assertClose(100000 + 600000, orbit.periapsis, error=10)
        self.assertClose(100000,  orbit.apoapsis_altitude, error=10)
        self.assertClose(100000, orbit.periapsis_altitude, error=10)
        self.assertClose(100000 + 600000, orbit.semi_major_axis, error=10)
        self.assertClose(100000 + 600000, orbit.semi_minor_axis, error=10)
        self.assertClose(700000, orbit.radius, error=10)
        self.assertClose(2246.1, orbit.speed, error=1)
        self.assertClose(603.48, orbit.time_to_apoapsis, error=1)
        self.assertClose(1582.5, orbit.time_to_periapsis, error=1)
        self.assertTrue(math.isnan(orbit.time_to_soi_change))
        self.assertClose(0, orbit.eccentricity, error=0.1)
        self.assertClose(0, orbit.inclination, error=0.1)
        self.assertClose(0, orbit.longitude_of_ascending_node, error=0.1)
        self.assertClose(0, orbit.argument_of_periapsis, error=0.1)
        #self.assertClose(0, orbit.mean_anomaly, error=0.1)
        #self.assertClose(0, orbit.eccentric_anomaly, error=0.1)
        self.assertEqual(None, orbit.next_orbit)

    def test_vessel_orbiting_bop(self):
        load_save('orbit-bop')
        ksp = krpc.connect()
        vessel = ksp.space_center.active_vessel
        orbit = vessel.orbit
        # inc   27
        # e     0.18
        # sma   320000
        # lan   38
        # w     241
        # mEp   2.3
        # epoch 0
        # body  Kerbin
        self.assertEqual('Bop', orbit.body.name)
        self.assertClose(377600,  orbit.apoapsis, error=10)
        self.assertClose(262400 , orbit.periapsis, error=10)
        self.assertClose(377600 - 65000,  orbit.apoapsis_altitude, error=10)
        self.assertClose(262400 - 65000, orbit.periapsis_altitude, error=10)
        sma = 0.5 * (377600 + 262400)
        e = 0.18
        self.assertClose(sma, orbit.semi_major_axis, error=10)
        self.assertClose(sma * math.sqrt(1 - (e*e)), orbit.semi_minor_axis, error=10)
        self.assertClose(366329, orbit.radius, error=10)
        self.assertClose(76, orbit.speed, error=1)
        self.assertClose(2698.33, orbit.time_to_apoapsis, error=1)
        self.assertClose(14102.17, orbit.time_to_periapsis, error=1)
        self.assertTrue(math.isnan(orbit.time_to_soi_change))
        self.assertClose(e, orbit.eccentricity, error=0.1)
        self.assertClose(27 * (math.pi/180), orbit.inclination, error=0.1)
        self.assertClose(38 * (math.pi/180), orbit.longitude_of_ascending_node, error=0.1)
        self.assertClose(241 * (math.pi/180), orbit.argument_of_periapsis, error=0.1)
        #self.assertClose(2.3 * (math.pi/180), orbit.mean_anomaly, error=0.1)
        #self.assertClose(2.3 * (math.pi/180), orbit.eccentric_anomaly, error=0.1)
        self.assertEqual(None, orbit.next_orbit)

    def test_vessel_orbiting_mun_on_escape_soi(self):
        load_save('orbit-mun-escape-soi')
        ksp = krpc.connect()
        vessel = ksp.space_center.active_vessel
        orbit = vessel.orbit
        # inc   0
        # e     0.52
        # sma   1800000
        # lan   13
        # w     67
        # mEp   6.25
        # epoch 0
        # body  Mun
        self.assertEqual('Mun', orbit.body.name)
        self.assertClose(2736000, orbit.apoapsis, error=10)
        self.assertClose(864000, orbit.periapsis, error=10)
        self.assertClose(2736000 - 200000, orbit.apoapsis_altitude, error=10)
        self.assertClose(864000 - 200000, orbit.periapsis_altitude, error=10)
        sma = (0.5 * (2736000 + 864000))
        e = 0.52
        self.assertClose(sma, orbit.semi_major_axis, error=10)
        self.assertClose(sma * math.sqrt(1 - (e*e)), orbit.semi_minor_axis, error=10)
        self.assertClose(865546, orbit.radius, error=10)
        self.assertClose(338.1, orbit.speed, error=1)
        self.assertClose(29987.92, orbit.time_to_apoapsis, error=1)
        self.assertClose(261.65, orbit.time_to_periapsis, error=1)
        self.assertClose(18464, orbit.time_to_soi_change,error=5)
        self.assertClose(e, orbit.eccentricity, error=0.1)
        self.assertClose(0, orbit.inclination, error=0.1)
        # TODO: fix this
        #self.assertClose(13 * (math.pi/180), orbit.longitude_of_ascending_node, error=0.1)
        #self.assertClose(67 * (math.pi/180), orbit.argument_of_periapsis, error=0.1)
        #self.assertClose(6.2 * (math.pi/180), orbit.mean_anomaly, error=0.1)
        #self.assertClose(6.2 * (math.pi/180), orbit.eccentric_anomaly, error=0.1)
        self.assertTrue(orbit.next_orbit is not None)

        orbit = orbit.next_orbit
        self.assertEqual('Kerbin', orbit.body.name)
        self.assertClose(25224000, orbit.apoapsis, error=1000)
        self.assertClose(12428000, orbit.periapsis, error=1000)

    """
    def test_vessel_orbiting_minmus_on_parabolic_arc(self):
        load_save('orbit-minmus-parabolic')
        ksp = krpc.connect()
        vessel = ksp.space_center.active_vessel
        orbit = vessel.orbit
        self.assertEqual('Minmus', orbit.body.name)
        self.assertClose(-175327.32795440647, orbit.apoapsis)
        self.assertClose(87187.64537168786, orbit.periapsis)
        self.assertClose(-235327.32795440647, orbit.apoapsis_altitude)
        self.assertClose(27187.64537168786, orbit.periapsis_altitude)
        self.assertClose(0, orbit.time_to_apoapsis, error=0.5)
        self.assertClose(1024.43, orbit.time_to_periapsis, error=0.5)
        self.assertClose(2.97839708101655, orbit.eccentricity)
        self.assertClose(168.280967855609, orbit.inclination)
        self.assertClose(181.171756205933, orbit.longitude_of_ascending_node)
        self.assertClose(165.50774557981, orbit.argument_of_periapsis)
        self.assertClose(-4.65482114687744, orbit.mean_anomaly_at_epoch)
        self.assertClose(0, orbit.radius)
        self.assertClose(0, orbit.speed)
    """

    def test_sun_orbit(self):
        sun = self.conn.space_center.bodies['Sun']
        self.assertIsNone(sun.orbit)

    def test_kerbin_orbiting_sun(self):
        kerbin = self.conn.space_center.bodies['Kerbin']
        orbit = kerbin.orbit
        self.assertEqual('Sun', orbit.body.name)
        self.assertClose(13599840256, orbit.apoapsis)
        self.assertClose(13599840256, orbit.periapsis)
        self.assertClose(13599840256 - 261600000, orbit.apoapsis_altitude)
        self.assertClose(13599840256 - 261600000, orbit.periapsis_altitude)
        self.assertClose(13599840256, orbit.semi_major_axis)
        self.assertClose(13599840256, orbit.semi_minor_axis)
        #self.assertClose(0, orbit.time_to_apoapsis, error=0.5)
        #self.assertClose(0, orbit.time_to_periapsis, error=0.5)
        self.assertClose(0, orbit.eccentricity)
        self.assertClose(0, orbit.inclination)
        self.assertClose(0, orbit.longitude_of_ascending_node)
        self.assertClose(0, orbit.argument_of_periapsis)
        #self.assertClose(0, orbit.mean_anomaly)
        #self.assertClose(0, orbit.eccentric_anomaly)
        self.assertClose(13599840256, orbit.radius)
        self.assertClose(9284.5, orbit.speed)

    def test_minmus_orbiting_kerbin(self):
        minmus = self.conn.space_center.bodies['Minmus']
        orbit = minmus.orbit
        self.assertEqual('Kerbin', orbit.body.name)
        self.assertClose(47000000, orbit.apoapsis)
        self.assertClose(47000000, orbit.periapsis)
        self.assertClose(47000000 - 600000, orbit.apoapsis_altitude)
        self.assertClose(47000000 - 600000, orbit.periapsis_altitude)
        self.assertClose(47000000, orbit.semi_major_axis)
        self.assertClose(47000000, orbit.semi_minor_axis)
        #self.assertClose(0, orbit.time_to_apoapsis, error=0.5)
        #self.assertClose(0, orbit.time_to_periapsis, error=0.5)
        self.assertClose(0, orbit.eccentricity)
        self.assertClose(6 * (math.pi/180), orbit.inclination)
        self.assertClose(78 * (math.pi/180), orbit.longitude_of_ascending_node)
        self.assertClose(38 * (math.pi/180), orbit.argument_of_periapsis)
        #self.assertClose(0, orbit.mean_anomaly)
        #self.assertClose(0, orbit.eccentric_anomaly)
        self.assertClose(47000000, orbit.radius)
        self.assertClose(274.1, orbit.speed, error=0.5)

    def test_eeloo_orbiting_sun(self):
        eeloo = self.conn.space_center.bodies['Eeloo']
        orbit = eeloo.orbit
        self.assertEqual('Sun', orbit.body.name)
        self.assertClose(113549713200, orbit.apoapsis)
        self.assertClose(66687926800, orbit.periapsis)
        self.assertClose(113549713200 - 261600000, orbit.apoapsis_altitude)
        self.assertClose(66687926800 - 261600000, orbit.periapsis_altitude)
        sma = 0.5 * (113549713200 + 66687926800)
        e = 0.26
        self.assertClose(sma, orbit.semi_major_axis)
        self.assertClose(sma * math.sqrt(1 - (e*e)), orbit.semi_minor_axis)
        #self.assertClose(0, orbit.time_to_apoapsis, error=0.5)
        #self.assertClose(0, orbit.time_to_periapsis, error=0.5)
        self.assertClose(e, orbit.eccentricity)
        self.assertClose(6.15 * (math.pi/180), orbit.inclination)
        self.assertClose(50 * (math.pi/180), orbit.longitude_of_ascending_node)
        self.assertClose(260 * (math.pi/180), orbit.argument_of_periapsis)
        #self.assertClose(0, orbit.mean_anomaly)
        #self.assertClose(0, orbit.eccentric_anomaly)
        #self.assertClose(13599840256, orbit.radius)
        #self.assertClose(9284.5, orbit.speed)

if __name__ == "__main__":
    unittest.main()
