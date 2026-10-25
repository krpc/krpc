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
