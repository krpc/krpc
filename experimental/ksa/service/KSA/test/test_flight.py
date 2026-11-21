import math

import ksatest


class TestFlight(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        vehicle = cls.find_vehicle("Gemini7")
        cls.flight = vehicle.flight
        cls.orbit = vehicle.orbit

    def test_altitude(self):
        altitude = self.flight.mean_altitude
        self.assertGreaterEqual(altitude, self.orbit.periapsis_altitude - 1)
        self.assertLessEqual(altitude, self.orbit.apoapsis_altitude + 1)
        # Terrain is within 11 km of the mean radius
        self.assertAlmostEqual(altitude, self.flight.surface_altitude, delta=11e3)

    def test_speed(self):
        # Orbital speed, less up to the speed of the Earth's surface
        speed = self.flight.speed
        self.assertGreater(speed, 7000)
        self.assertLess(speed, 8300)
        # Radial speed in an orbit with an eccentricity of 0.0077 is under 60 m/s
        self.assertLess(abs(self.flight.vertical_speed), 100)
        self.assertAlmostEqual(
            speed,
            math.hypot(self.flight.horizontal_speed, self.flight.vertical_speed),
            delta=1,
        )

    def test_attitude_ranges(self):
        self.assertGreaterEqual(self.flight.pitch, -90)
        self.assertLessEqual(self.flight.pitch, 90)
        self.assertGreaterEqual(self.flight.heading, 0)
        self.assertLess(self.flight.heading, 360)
        self.assertGreaterEqual(self.flight.roll, -180)
        self.assertLessEqual(self.flight.roll, 180)

    def test_position(self):
        self.assertGreaterEqual(self.flight.latitude, -90)
        self.assertLessEqual(self.flight.latitude, 90)
        self.assertGreaterEqual(self.flight.longitude, -180)
        self.assertLessEqual(self.flight.longitude, 180)
        # The orbit is inclined 28.87 degrees to the equator
        self.assertLessEqual(abs(self.flight.latitude), 28.87 + 0.1)

    def test_atmosphere(self):
        # Above the atmosphere, in orbit
        self.assertLess(self.flight.static_pressure, 1)
        self.assertLess(self.flight.atmosphere_density, 1e-6)
        self.assertLess(self.flight.dynamic_pressure, 1)

    def test_g_force(self):
        # Coasting in orbit is free fall
        self.assertLess(self.flight.g_force, 0.01)

    def test_surface_directions(self):
        vehicle = self.find_vehicle("Gemini7")
        frame = vehicle.orbit.body.reference_frame
        prograde = self.flight.prograde(frame)
        normal = self.flight.normal(frame)
        radial = self.flight.radial(frame)
        velocity = vehicle.velocity(frame)
        position = vehicle.position(frame)
        speed = math.sqrt(dot(velocity, velocity))
        for axis in (prograde, normal, radial):
            self.assertAlmostEqual(1, dot(axis, axis), places=6)
        self.assertAlmostEqual(1, dot(prograde, velocity) / speed, places=3)
        self.assertAlmostEqual(0, dot(prograde, normal), places=6)
        self.assertAlmostEqual(0, dot(normal, position) / norm(position), places=3)
        self.assertAlmostEqual(0, dot(radial, prograde), places=6)
        self.assertGreater(dot(radial, position), 0)

    def test_biome(self):
        biomes = self.find_vehicle("Gemini7").orbit.body.biomes
        self.assertIn(self.flight.biome, biomes)


def dot(a, b):
    return sum(x * y for x, y in zip(a, b))


def norm(a):
    return math.sqrt(dot(a, a))
