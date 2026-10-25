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
