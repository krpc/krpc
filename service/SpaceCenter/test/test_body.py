import unittest
import math
import krpc
import krpctest
from krpctest.geometry import norm, normalize, dot, rad2deg


class TestBody(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.space_center = cls.connect().space_center

    def test_equality(self):
        bodies = self.space_center.bodies
        bodies2 = self.space_center.bodies
        for key, body in bodies.items():
            self.assertEqual(bodies2[key], body)

    def test_kerbin(self):
        kerbin = self.space_center.bodies['Kerbin']
        self.assertEqual('Kerbin', kerbin.name)
        self.assertAlmostEqual(5.2915158e22, kerbin.mass, delta=0.0001e22)
        self.assertAlmostEqual(3.5316000e12,
                               kerbin.gravitational_parameter, delta=0.0001e12)
        self.assertAlmostEqual(9.81, kerbin.surface_gravity, places=2)
        self.assertAlmostEqual(21549.425, kerbin.rotational_period, delta=0.1)
        self.assertAlmostEqual((2*3.14159) / 21549.425,
                               kerbin.rotational_speed, delta=0.1)
        self.assertAlmostEqual(math.pi/2, kerbin.initial_rotation, delta=0.1)
        self.assertDegreesAlmostEqual(
            rad2deg(kerbin.initial_rotation +
                    (self.space_center.ut * kerbin.rotational_speed)),
            rad2deg(kerbin.rotation_angle), delta=0.1)
        self.assertAlmostEqual(600000, kerbin.equatorial_radius)
        self.assertAlmostEqual(8.4159e7,
                               kerbin.sphere_of_influence, delta=0.0001e7)
        self.assertAlmostEqual(1.36e10, kerbin.orbit.apoapsis, delta=0.0001e10)
        self.assertAlmostEqual(1.36e10,
                               kerbin.orbit.periapsis, delta=0.0001e10)
        self.assertTrue(kerbin.has_atmosphere)
        self.assertAlmostEqual(70000, kerbin.atmosphere_depth)
        self.assertTrue(kerbin.has_atmospheric_oxygen)
        self.assertEqual(['Badlands', 'Deserts', 'Grasslands', 'Highlands',
                          'Ice Caps', 'Mountains', 'Northern Ice Shelf',
                          'Shores', 'Southern Ice Shelf', 'Tundra', 'Water'],
                         sorted(kerbin.biomes))
        self.assertEqual('Water', kerbin.biome_at(0, 0))
        self.assertEqual('Grasslands', kerbin.biome_at(42, 4))
        self.assertEqual(18000, kerbin.flying_high_altitude_threshold)
        self.assertEqual(250000, kerbin.space_high_altitude_threshold)

    def test_mun(self):
        mun = self.space_center.bodies['Mun']
        self.assertEqual('Mun', mun.name)
        self.assertAlmostEqual(9.7599063e20, mun.mass, delta=0.0000001e20)
        self.assertAlmostEqual(6.5138398e10,
                               mun.gravitational_parameter, delta=0.0000001e10)
        self.assertAlmostEqual(1.6290, mun.surface_gravity, places=4)
        self.assertAlmostEqual(1.3898e5, mun.rotational_period, delta=0.0001e5)
        self.assertAlmostEqual((2 * 3.14159) / 1.3898e5,
                               mun.rotational_speed, delta=0.0001e5)
        self.assertAlmostEqual(math.pi * 5/4, mun.initial_rotation, delta=0.1)
        self.assertAlmostEqual(
            mun.initial_rotation +
            (self.space_center.ut * mun.rotational_speed),
            mun.rotation_angle, delta=0.1)
        self.assertAlmostEqual(200000, mun.equatorial_radius)
        self.assertAlmostEqual(2.4296e6,
                               mun.sphere_of_influence, delta=0.0001e6)
        self.assertAlmostEqual(1.2e7, mun.orbit.apoapsis, delta=0.0001e7)
        self.assertAlmostEqual(1.2e7, mun.orbit.periapsis, delta=0.0001e7)
        self.assertFalse(mun.has_atmosphere)
        self.assertAlmostEqual(0, mun.atmosphere_depth)
        self.assertFalse(mun.has_atmospheric_oxygen)
        self.assertEqual(
            ['Canyons', 'East Crater', 'East Farside Crater', 'Farside Basin',
             'Farside Crater', 'Highland Craters', 'Highlands', 'Lowlands',
             'Midland Craters', 'Midlands', 'Northern Basin',
             'Northwest Crater', 'Polar Crater', 'Polar Lowlands', 'Poles',
             'Southwest Crater', 'Twin Craters'],
            sorted(mun.biomes))
        self.assertEqual('Lowlands', mun.biome_at(0, 0))
        self.assertEqual('Highlands', mun.biome_at(42, 4))
        self.assertEqual(18000, mun.flying_high_altitude_threshold)
        self.assertEqual(60000, mun.space_high_altitude_threshold)

    def test_minmus(self):
        minmus = self.space_center.bodies['Minmus']
        self.assertEqual('Minmus', minmus.name)
        self.assertAlmostEqual(4.7e7, minmus.orbit.apoapsis, delta=0.0001e7)
        self.assertAlmostEqual(4.7e7, minmus.orbit.periapsis, delta=0.0001e7)
        self.assertAlmostEqual(6 * (math.pi/180), minmus.orbit.inclination)
        self.assertFalse(minmus.has_atmosphere)
        self.assertAlmostEqual(0, minmus.atmosphere_depth)
        self.assertFalse(minmus.has_atmospheric_oxygen)
        self.assertEqual(
            ['Flats', 'Great Flats', 'Greater Flats', 'Highlands',
             'Lesser Flats', 'Lowlands', 'Midlands', 'Poles', 'Slopes'],
            sorted(minmus.biomes))
        self.assertEqual('Greater Flats', minmus.biome_at(0, 0))
        self.assertEqual('Midlands', minmus.biome_at(42, 4))
        self.assertEqual(18000, minmus.flying_high_altitude_threshold)
        self.assertEqual(30000, minmus.space_high_altitude_threshold)

    def test_sun(self):
        sun = self.space_center.bodies['Sun']
        self.assertEqual('Sun', sun.name)
        self.assertAlmostEqual(1.7565459e28, sun.mass, delta=0.0000001e28)
        self.assertAlmostEqual(1.1723328e18,
                               sun.gravitational_parameter, delta=0.0000001e18)
        self.assertAlmostEqual(2.616e8, sun.equatorial_radius, delta=0.0001e8)
        self.assertEqual(float('inf'), sun.sphere_of_influence)
        self.assertIsNone(sun.orbit)
        self.assertTrue(sun.has_atmosphere)
        self.assertAlmostEqual(600000, sun.atmosphere_depth)
        self.assertFalse(sun.has_atmospheric_oxygen)
        self.assertRaises(krpc.client.RPCError, getattr, sun, 'biomes')
        self.assertRaises(krpc.client.RPCError, sun.biome_at, 0, 0)
        self.assertRaises(krpc.client.RPCError, sun.biome_at, 42, 4)
        self.assertEqual(18000, sun.flying_high_altitude_threshold)
        self.assertEqual(1e9, sun.space_high_altitude_threshold)

    def test_duna(self):
        duna = self.space_center.bodies['Duna']
        self.assertTrue(duna.has_atmosphere)
        self.assertAlmostEqual(50000, duna.atmosphere_depth)
        self.assertFalse(duna.has_atmospheric_oxygen)

    def test_system(self):
        bodies = self.space_center.bodies
        kerbin = bodies['Kerbin']
        mun = bodies['Mun']
        minmus = bodies['Minmus']
        sun = bodies['Sun']
        duna = bodies['Duna']
        ike = bodies['Ike']

        self.assertEqual(kerbin, mun.orbit.body)
        self.assertEqual(kerbin, minmus.orbit.body)
        self.assertEqual(sun, kerbin.orbit.body)
        self.assertEqual(sun, duna.orbit.body)

        self.assertNotEqual(sun, mun.orbit.body)
        self.assertNotEqual(sun, minmus.orbit.body)
        self.assertNotEqual(mun, kerbin.orbit.body)

        self.assertEqual(set([mun, minmus]), set(kerbin.satellites))
        self.assertEqual(set([ike]), set(duna.satellites))
        self.assertEqual(set(), set(mun.satellites))

    def test_position(self):
        for body in self.space_center.bodies.values():

            # Check body position in body's reference frame
            pos = body.position(body.reference_frame)
            self.assertAlmostEqual((0, 0, 0), pos)

            # Check body position in parent body's reference frame
            if body.orbit is not None:
                pos = body.position(body.orbit.body.reference_frame)
                if body.orbit.inclination == 0:
                    self.assertAlmostEqual(0, pos[1])
                else:
                    self.assertNotAlmostEqual(0, pos[1])
                # TODO: large error
                self.assertAlmostEqual(body.orbit.radius, norm(pos), delta=100)

    def test_velocity(self):
        for body in self.space_center.bodies.values():
            if body.orbit is None:
                continue

            # Check body velocity in body's reference frame
            v = body.velocity(body.reference_frame)
            self.assertAlmostEqual((0, 0, 0), v)

            # Check body velocity in parent body's non-rotating reference frame
            v = body.velocity(body.orbit.body.non_rotating_reference_frame)
            self.assertAlmostEqual(body.orbit.speed, norm(v), places=2)

            # Check body velocity in parent body's reference frame
            v = body.velocity(body.orbit.body.reference_frame)
            if body.orbit.inclination == 0:
                self.assertAlmostEqual(0, v[1])
            else:
                self.assertNotAlmostEqual(0, v[1])
            angular_velocity = body.orbit.body.angular_velocity(
                body.orbit.body.non_rotating_reference_frame)
            self.assertAlmostEqual(0, angular_velocity[0])
            self.assertAlmostEqual(0, angular_velocity[2])
            rotational_speed = dot((0, 1, 0), angular_velocity)
            position = list(body.position(body.orbit.body.reference_frame))
            position[1] = 0
            radius = norm(position)
            rotational_speed *= radius
            # TODO: large error
            self.assertAlmostEqual(
                abs(rotational_speed + body.orbit.speed), norm(v), delta=500)

    def test_rotation(self):
        for body in self.space_center.bodies.values():
            # Check body's rotation relative to itself is zero
            r = body.rotation(body.reference_frame)
            # TODO: better test for identity quaternion
            self.assertAlmostEqual((0, 0, 0), (r[0], r[1], r[2]), places=3)

            # TODO: more thorough testing

    def test_angular_velocity(self):
        for body in self.space_center.bodies.values():
            # Check body's angular velocity relative to itself is zero
            av = body.angular_velocity(body.reference_frame)
            self.assertAlmostEqual((0, 0, 0), av)

            # Check body's angular velocity relative
            # to it's own non-rotating reference frame
            av = body.angular_velocity(body.non_rotating_reference_frame)
            self.assertAlmostEqual((0, -1, 0), normalize(av))
            self.assertAlmostEqual(body.rotational_speed, norm(av))


if __name__ == '__main__':
    unittest.main()
