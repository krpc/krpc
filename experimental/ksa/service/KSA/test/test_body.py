import math

import ksatest

G = 6.6743e-11


class TestBody(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        cls.earth = cls.ksa.bodies["Earth"]

    def test_equality(self):
        self.assertEqual(self.ksa.bodies["Earth"], self.earth)
        self.assertNotEqual(self.ksa.bodies["Sol"], self.earth)

    def test_earth(self):
        self.assertEqual("Earth", self.earth.name)
        self.assertAlmostEqual(6371e3, self.earth.radius, delta=1)
        self.assertAlmostEqual(5.972e24, self.earth.mass, delta=0.001e24)
        self.assertAlmostEqual(
            G * self.earth.mass,
            self.earth.gravitational_parameter,
            delta=1e-3 * G * self.earth.mass,
        )
        self.assertGreater(self.earth.atmosphere_radius, self.earth.radius)

    def test_sol(self):
        sol = self.ksa.bodies["Sol"]
        self.assertGreater(sol.radius, 0)
        self.assertGreater(sol.mass, self.earth.mass)

    def test_rotation(self):
        # Earth's sidereal day
        self.assertAlmostEqual(86164.1, self.earth.rotational_period, delta=1)
        self.assertAlmostEqual(
            2 * math.pi / self.earth.rotational_period,
            self.earth.rotational_speed,
            delta=1e-9,
        )

    def test_surface_gravity(self):
        self.assertAlmostEqual(
            self.earth.gravitational_parameter / self.earth.radius**2,
            self.earth.surface_gravity,
            delta=1e-6,
        )
        self.assertAlmostEqual(9.82, self.earth.surface_gravity, delta=0.01)

    def test_sphere_of_influence(self):
        luna = self.ksa.bodies["Luna"]
        self.assertGreater(self.earth.sphere_of_influence, 3.8e8)
        self.assertLess(luna.sphere_of_influence, self.earth.sphere_of_influence)

    def test_hierarchy(self):
        sol = self.ksa.bodies["Sol"]
        luna = self.ksa.bodies["Luna"]
        self.assertIsNone(sol.parent)
        self.assertEqual(sol, self.earth.parent)
        self.assertEqual(self.earth, luna.parent)
        self.assertIn(luna, self.earth.satellites)
        self.assertIn(self.earth, sol.satellites)

    def test_star(self):
        self.assertTrue(self.ksa.bodies["Sol"].is_star)
        self.assertFalse(self.earth.is_star)

    def test_atmosphere(self):
        self.assertTrue(self.earth.has_atmosphere)
        self.assertAlmostEqual(101325, self.earth.sea_level_pressure, delta=1000)
        self.assertAlmostEqual(1.225, self.earth.sea_level_density, delta=0.05)
        self.assertAlmostEqual(8000, self.earth.scale_height, delta=1500)
        self.assertAlmostEqual(
            self.earth.sea_level_pressure, self.earth.pressure_at(0), delta=1
        )
        self.assertAlmostEqual(
            self.earth.sea_level_density, self.earth.density_at(0), delta=0.001
        )
        self.assertLess(self.earth.pressure_at(50e3), self.earth.pressure_at(10e3))
        self.assertLess(self.earth.density_at(50e3), self.earth.density_at(10e3))

    def test_no_atmosphere(self):
        luna = self.ksa.bodies["Luna"]
        self.assertFalse(luna.has_atmosphere)
        self.assertEqual(0, luna.sea_level_pressure)
        self.assertEqual(0, luna.sea_level_density)
        self.assertEqual(0, luna.scale_height)
        self.assertEqual(0, luna.pressure_at(0))
        self.assertEqual(0, luna.density_at(0))

    def test_ocean(self):
        self.assertTrue(self.earth.has_ocean)
        self.assertAlmostEqual(1025, self.earth.ocean_density, delta=30)
        luna = self.ksa.bodies["Luna"]
        self.assertFalse(luna.has_ocean)
        self.assertEqual(0, luna.ocean_density)

    def test_biomes(self):
        biomes = self.earth.biomes
        self.assertGreater(len(biomes), 0)
        self.assertEqual(len(biomes), len(set(biomes)))
        for latitude, longitude in ((0, 0), (45, 90), (-80, -120)):
            self.assertIn(self.earth.biome_at(latitude, longitude), biomes)
        self.assertEqual([], self.ksa.bodies["Sol"].biomes)
        self.assertIsNone(self.ksa.bodies["Sol"].biome_at(0, 0))

    def test_position_queries(self):
        # In the non-rotating frame, the surface turns about 0.004 degrees a second
        # between the calls
        frames = (
            (self.earth.reference_frame, 6),
            (self.earth.non_rotating_reference_frame, 3),
        )
        for frame, places in frames:
            for latitude, longitude in ((0, 0), (30, 60), (-45, -150)):
                with self.subTest(latitude=latitude, longitude=longitude):
                    position = self.earth.surface_position(
                        latitude, longitude, self.earth.reference_frame
                    )
                    position = self.ksa.transform_position(
                        position, self.earth.reference_frame, frame
                    )
                    self.assertAlmostEqual(
                        latitude,
                        self.earth.latitude_at_position(position, frame),
                        places=places,
                    )
                    self.assertAlmostEqual(
                        longitude,
                        self.earth.longitude_at_position(position, frame),
                        places=places,
                    )
                    self.assertAlmostEqual(
                        0, self.earth.altitude_at_position(position, frame), delta=0.01
                    )

    def test_vehicle_position_queries(self):
        vehicle = self.find_vehicle("Gemini7")
        frame = self.earth.non_rotating_reference_frame
        position = vehicle.position(frame)
        flight = vehicle.flight
        self.assertAlmostEqual(
            flight.latitude, self.earth.latitude_at_position(position, frame), places=3
        )
        self.assertAlmostEqual(
            flight.longitude,
            self.earth.longitude_at_position(position, frame),
            places=3,
        )
        self.assertAlmostEqual(
            flight.mean_altitude,
            self.earth.altitude_at_position(position, frame),
            delta=1,
        )
