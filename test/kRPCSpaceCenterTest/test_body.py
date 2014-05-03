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
        self.assertEqual(5.291579262810909e+22, kerbin.mass)
        self.assertEqual(3.5316e12, kerbin.gravitational_parameter)
        self.assertEqual(600000, kerbin.equatorial_radius)
        self.assertEqual(84159286.47963049, kerbin.sphere_of_influence)
        self.assertEqual(13599840256, kerbin.orbit.apoapsis)
        self.assertEqual(13599840256, kerbin.orbit.periapsis)

        mun = ksp.space_center.body('Mun')
        self.assertEqual('Mun', mun.name)
        self.assertEqual(9.760023602154736e+20, mun.mass)
        self.assertEqual(65138397520.7807, mun.gravitational_parameter)
        self.assertEqual(200000, mun.equatorial_radius)
        self.assertEqual(2429559.1165647474, mun.sphere_of_influence)
        self.assertEqual(1.2e7, mun.orbit.apoapsis)
        self.assertEqual(1.2e7, mun.orbit.periapsis)

        minmus = ksp.space_center.body('Minmus')
        self.assertEqual('Minmus', minmus.name)
        self.assertEqual(4.7e7, minmus.orbit.apoapsis)
        self.assertEqual(4.7e7, minmus.orbit.periapsis)
        self.assertEqual(6, minmus.orbit.inclination)

        sun = ksp.space_center.body('Sun')
        self.assertEqual('Sun', sun.name)
        self.assertEqual(1.7565669685832947e+28, sun.mass)
        self.assertEqual(1.1723327948324908e+18, sun.gravitational_parameter)
        self.assertEqual(2.616e8, sun.equatorial_radius)
        self.assertEqual(float('inf'), sun.sphere_of_influence)
        self.assertEqual(None, sun.orbit)

        self.assertRaises(krpc.client.RPCError, ksp.space_center.body, 'Foo')

        self.assertEqual(kerbin, mun.orbit.body)
        self.assertEqual(kerbin, minmus.orbit.body)
        self.assertEqual(sun, kerbin.orbit.body)

        self.assertNotEqual(sun, mun.orbit.body)
        self.assertNotEqual(sun, minmus.orbit.body)
        self.assertNotEqual(mun, kerbin.orbit.body)

if __name__ == "__main__":
    unittest.main()
