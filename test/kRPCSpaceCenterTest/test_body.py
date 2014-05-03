#!/usr/bin/env python2

import unittest
import testingtools
from testingtools import load_save
import krpc

class TestBody(testingtools.TestCase):

    def test_basic(self):
        load_save('flight')
        ksp = krpc.connect()

        kerbin = ksp.space_center.body('Kerbin')
        self.assertEqual('Kerbin', kerbin.name)
        self.assertClose(5.2915e22, kerbin.mass, error=0.0001e22)
        self.assertClose(3.5316e12, kerbin.gravitational_parameter, error=0.0001e12)
        self.assertClose(9.81, kerbin.surface_gravity)
        self.assertClose(21600, kerbin.rotational_period)
        self.assertClose(600000, kerbin.equatorial_radius)
        self.assertClose(8.4159e7, kerbin.sphere_of_influence, error=0.0001e7)
        self.assertClose(1.36e10, kerbin.orbit.apoapsis, error=0.0001e10)
        self.assertClose(1.36e10, kerbin.orbit.periapsis, error=0.0001e10)
        self.assertEqual(True, kerbin.has_atmosphere)
        self.assertClose(101.325, kerbin.atmosphere_pressure)
        self.assertClose(5000, kerbin.atmosphere_scale_height)
        self.assertClose(70000, kerbin.atmosphere_max_altitude)

        mun = ksp.space_center.body('Mun')
        self.assertEqual('Mun', mun.name)
        self.assertClose(9.76e20, mun.mass, error=0.0001e20)
        self.assertClose(6.5138e10, mun.gravitational_parameter, error=0.0001e10)
        self.assertClose(1.6285, mun.surface_gravity)
        self.assertClose(1.3898e5, mun.rotational_period, error=0.0001e5)
        self.assertClose(200000, mun.equatorial_radius)
        self.assertClose(2.4296e6, mun.sphere_of_influence, error=0.0001e6)
        self.assertClose(1.2e7, mun.orbit.apoapsis, error=0.0001e7)
        self.assertClose(1.2e7, mun.orbit.periapsis, error=0.0001e7)
        self.assertEqual(False, mun.has_atmosphere)
        self.assertClose(0, mun.atmosphere_pressure)
        self.assertClose(0, mun.atmosphere_scale_height)
        self.assertClose(0, mun.atmosphere_max_altitude)

        minmus = ksp.space_center.body('Minmus')
        self.assertEqual('Minmus', minmus.name)
        self.assertClose(4.7e7, minmus.orbit.apoapsis, error=0.0001e7)
        self.assertClose(4.7e7, minmus.orbit.periapsis, error=0.0001e7)
        self.assertClose(6, minmus.orbit.inclination)
        self.assertEqual(False, mun.has_atmosphere)

        sun = ksp.space_center.body('Sun')
        self.assertEqual('Sun', sun.name)
        self.assertClose(1.7566e28, sun.mass, error=0.0001e28)
        self.assertClose(1.1723e18, sun.gravitational_parameter, error=0.0001e18)
        self.assertClose(2.616e8, sun.equatorial_radius, error=0.0001e8)
        self.assertEqual(float('inf'), sun.sphere_of_influence)
        self.assertEqual(None, sun.orbit)
        self.assertEqual(False, mun.has_atmosphere)

        duna = ksp.space_center.body('Duna')
        self.assertEqual(True, duna.has_atmosphere)
        self.assertClose(20.265, duna.atmosphere_pressure)
        self.assertClose(3000, duna.atmosphere_scale_height)
        self.assertClose(50000, duna.atmosphere_max_altitude)

        self.assertRaises(krpc.client.RPCError, ksp.space_center.body, 'Foo')

        self.assertEqual(kerbin, mun.orbit.body)
        self.assertEqual(kerbin, minmus.orbit.body)
        self.assertEqual(sun, kerbin.orbit.body)
        self.assertEqual(sun, duna.orbit.body)

        self.assertNotEqual(sun, mun.orbit.body)
        self.assertNotEqual(sun, minmus.orbit.body)
        self.assertNotEqual(mun, kerbin.orbit.body)

if __name__ == "__main__":
    unittest.main()
