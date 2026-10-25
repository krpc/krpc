import math

import ksatest


class TestOrbit(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        cls.orbit = cls.find_vehicle("Gemini7").orbit
        cls.earth = cls.ksa.bodies["Earth"]

    def test_start_orbit(self):
        # Gemini7StartingSituation in the game's Situations.xml
        semi_major_axis = (self.orbit.apoapsis + self.orbit.periapsis) / 2
        self.assertAlmostEqual(6713e3, semi_major_axis, delta=1e3)
        self.assertAlmostEqual(0.0077, self.orbit.eccentricity, delta=1e-4)
        self.assertAlmostEqual(math.radians(28.87), self.orbit.inclination, delta=1e-3)
        self.assertAlmostEqual(
            math.radians(60.48), self.orbit.longitude_of_ascending_node, delta=1e-3
        )
        self.assertAlmostEqual(
            math.radians(30.0015), self.orbit.argument_of_periapsis, delta=1e-3
        )
        self.assertEqual(self.earth, self.orbit.body)

    def test_consistency(self):
        apoapsis = self.orbit.apoapsis
        periapsis = self.orbit.periapsis
        self.assertGreaterEqual(apoapsis, periapsis)
        self.assertAlmostEqual(
            (apoapsis - periapsis) / (apoapsis + periapsis),
            self.orbit.eccentricity,
            places=6,
        )
        self.assertAlmostEqual(
            apoapsis - self.earth.radius, self.orbit.apoapsis_altitude, delta=1
        )
        self.assertAlmostEqual(
            periapsis - self.earth.radius, self.orbit.periapsis_altitude, delta=1
        )

    def test_semi_major_axis(self):
        self.assertAlmostEqual(
            (self.orbit.apoapsis + self.orbit.periapsis) / 2,
            self.orbit.semi_major_axis,
            delta=1,
        )

    def test_speed(self):
        # The vis-viva equation
        flight = self.find_vehicle("Gemini7").flight
        r = flight.mean_altitude + self.earth.radius
        mu = self.earth.gravitational_parameter
        speed = math.sqrt(mu * (2 / r - 1 / self.orbit.semi_major_axis))
        self.assertAlmostEqual(speed, self.orbit.speed, delta=1)

    def test_period(self):
        semi_major_axis = (self.orbit.apoapsis + self.orbit.periapsis) / 2
        mu = self.earth.gravitational_parameter
        period = 2 * math.pi * math.sqrt(semi_major_axis**3 / mu)
        self.assertAlmostEqual(period, self.orbit.period, delta=1e-6 * period)

    def test_time_to_apsides(self):
        period = self.orbit.period
        to_apoapsis = self.orbit.time_to_apoapsis
        to_periapsis = self.orbit.time_to_periapsis
        self.assertGreaterEqual(to_apoapsis, 0)
        self.assertLess(to_apoapsis, period)
        self.assertGreaterEqual(to_periapsis, 0)
        self.assertLess(to_periapsis, period)
        # The apsides are half an orbit apart
        self.assertAlmostEqual(
            period / 2, (to_apoapsis - to_periapsis) % period, delta=1
        )
