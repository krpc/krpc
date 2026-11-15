import math
import unittest

from krpctools.game.geometry import norm
from krpctools.game.ksp import TestCase

# Earth in Real Solar System
EARTH_RADIUS = 6371000
EARTH_GM = 3.9860043543609598e14
EARTH_ROTATIONAL_PERIOD = 86164.098903691
EARTH_SEMI_MAJOR_AXIS = 149598261150.4425
SUN_GM = 1.3271244004193938e20

# The launch pad at Cape Canaveral, where Real Solar System puts the space center
PAD_LATITUDE = 28.608389
PAD_LONGITUDE = -80.604333


class TestRealSolarSystem(TestCase):
    """The bodies, orbits and reference frames of Real Solar System, the solar system at
    real scale."""

    mods = ["RealSolarSystem"]

    @classmethod
    def setUpClass(cls):
        cls.vessel = cls.launch("Basic")
        cls.space_center = cls.connect().space_center
        cls.bodies = cls.space_center.bodies
        cls.earth = cls.bodies["Earth"]
        cls.moon = cls.bodies["Moon"]
        cls.sun = cls.bodies["Sun"]

    def test_bodies(self):
        self.assertNotIn("Kerbin", self.bodies)
        self.assertIn("Mars", self.bodies)
        self.assertEqual(self.sun, self.earth.orbit.body)
        self.assertEqual(self.earth, self.moon.orbit.body)
        self.assertEqual(set([self.moon]), set(self.earth.satellites))
        self.assertIsNone(self.sun.orbit)

    def test_earth(self):
        earth = self.earth
        self.assertAlmostEqual(EARTH_RADIUS, earth.equatorial_radius)
        self.assertAlmostEqual(
            EARTH_GM, earth.gravitational_parameter, delta=EARTH_GM * 1e-9
        )
        self.assertAlmostEqual(5.972e24, earth.mass, delta=0.001e24)
        self.assertAlmostEqual(
            EARTH_GM / EARTH_RADIUS**2, earth.surface_gravity, delta=1e-6
        )
        self.assertAlmostEqual(EARTH_ROTATIONAL_PERIOD, earth.rotational_period)
        self.assertAlmostEqual(
            2 * math.pi / EARTH_ROTATIONAL_PERIOD, earth.rotational_speed, places=9
        )
        self.assertTrue(earth.has_atmosphere)
        self.assertTrue(earth.has_atmospheric_oxygen)
        self.assertAlmostEqual(140000, earth.atmosphere_depth)
        # The Laplace radius of the sphere of influence
        self.assertAlmostEqual(
            EARTH_SEMI_MAJOR_AXIS * (EARTH_GM / SUN_GM) ** 0.4,
            earth.sphere_of_influence,
            delta=1e7,
        )
        self.assertIn("Grasslands", earth.biomes)
        self.assertIn("Water", earth.biomes)

    def test_earth_orbit(self):
        orbit = self.earth.orbit
        self.assertAlmostEqual(
            EARTH_SEMI_MAJOR_AXIS,
            orbit.semi_major_axis,
            delta=EARTH_SEMI_MAJOR_AXIS * 1e-6,
        )
        # A sidereal year
        self.assertAlmostEqual(365.256 * 86400, orbit.period, delta=86400)
        self.assertAlmostEqual(
            orbit.radius, norm(self.earth.position(self.sun.reference_frame)), delta=100
        )
        self.assertAlmostEqual(
            orbit.speed,
            norm(self.earth.velocity(self.sun.non_rotating_reference_frame)),
            places=2,
        )

    def test_atmosphere(self):
        earth = self.earth
        self.assertAlmostEqual(101325, earth.pressure_at(0), delta=10)
        # The standard atmosphere gives 26.4 kPa and 0.41 kg/m^3 at 10 km
        self.assertAlmostEqual(26400, earth.pressure_at(10000), delta=26400 * 0.05)
        self.assertAlmostEqual(0.41, earth.density_at(10000), delta=0.41 * 0.1)
        self.assertEqual(0, earth.pressure_at(earth.atmosphere_depth + 1))


class TestRealSolarSystemLaunchPad(TestCase):
    mods = ["RealSolarSystem"]

    @classmethod
    def setUpClass(cls):
        cls.vessel = cls.launch("Basic")
        cls.space_center = cls.connect().space_center
        cls.earth = cls.space_center.bodies["Earth"]

    def test_position(self):
        flight = self.vessel.flight(self.earth.reference_frame)
        self.assertAlmostEqual(PAD_LATITUDE, flight.latitude, delta=0.05)
        self.assertAlmostEqual(PAD_LONGITUDE, flight.longitude, delta=0.05)
        position = self.vessel.position(self.earth.reference_frame)
        self.assertAlmostEqual(EARTH_RADIUS, norm(position), delta=1000)

    def test_speed(self):
        # At rest on the pad, the vessel moves with the surface as Earth turns
        flight = self.vessel.flight(self.earth.reference_frame)
        self.assertAlmostEqual(0, flight.speed, delta=0.5)
        flight = self.vessel.flight(self.earth.non_rotating_reference_frame)
        surface_speed = (
            2
            * math.pi
            * EARTH_RADIUS
            * math.cos(math.radians(PAD_LATITUDE))
            / EARTH_ROTATIONAL_PERIOD
        )
        self.assertAlmostEqual(surface_speed, flight.speed, delta=2)

    def test_atmosphere(self):
        flight = self.vessel.flight()
        # The pad stands a little above sea level
        self.assertGreater(flight.mean_altitude, 0)
        self.assertAlmostEqual(
            self.earth.pressure_at(flight.mean_altitude),
            flight.static_pressure,
            delta=10,
        )
        self.assertAlmostEqual(1.2, flight.atmosphere_density, delta=0.1)


class TestRealSolarSystemOrbit(TestCase):
    mods = ["RealSolarSystem"]

    @classmethod
    def setUpClass(cls):
        cls.vessel = cls.launch("Basic")
        cls.space_center = cls.connect().space_center
        cls.bodies = cls.space_center.bodies
        cls.earth = cls.bodies["Earth"]
        cls.pad_part_positions = cls.part_positions()

    @classmethod
    def part_positions(cls):
        frame = cls.vessel.reference_frame
        return [part.position(frame) for part in cls.vessel.parts.all]

    def test_low_earth_orbit(self):
        altitude = 400000
        self.set_circular_orbit("Earth", altitude)
        orbit = self.vessel.orbit
        radius = EARTH_RADIUS + altitude
        self.assertEqual(self.earth, orbit.body)
        self.assertAlmostEqual(radius, orbit.semi_major_axis, delta=10)
        self.assertAlmostEqual(0, orbit.eccentricity, places=4)
        self.assertAlmostEqual(
            2 * math.pi * math.sqrt(radius**3 / EARTH_GM), orbit.period, delta=1
        )
        self.assertAlmostEqual(math.sqrt(EARTH_GM / radius), orbit.speed, delta=1)
        self.assertTrue(math.isnan(orbit.time_to_soi_change))
        position = self.vessel.position(self.earth.reference_frame)
        self.assertAlmostEqual(radius, norm(position), delta=10)
        flight = self.vessel.flight(self.earth.reference_frame)
        self.assertAlmostEqual(altitude, flight.mean_altitude, delta=10)
        velocity = self.vessel.velocity(self.earth.non_rotating_reference_frame)
        self.assertAlmostEqual(orbit.speed, norm(velocity), delta=0.1)

    def test_escape(self):
        # Apoapsis beyond the sphere of influence, so the orbit leaves Earth for the Sun
        sma = 1.2e9
        self.set_orbit("Earth", sma, 0.8, 0, 0, 0, 0, 0)
        orbit = self.vessel.orbit
        self.assertGreater(orbit.apoapsis, self.earth.sphere_of_influence)
        self.assertFalse(math.isnan(orbit.time_to_soi_change))
        self.assertGreater(orbit.time_to_soi_change, 0)
        self.assertEqual(self.bodies["Sun"], orbit.next_orbit.body)

    def test_orbit_far_from_the_origin(self):
        # Around Mars, 1.5 AU from the Sun
        mars = self.bodies["Mars"]
        altitude = 500000
        self.set_circular_orbit("Mars", altitude)
        orbit = self.vessel.orbit
        self.assertEqual(mars, orbit.body)
        radius = mars.equatorial_radius + altitude
        position = self.vessel.position(mars.reference_frame)
        self.assertAlmostEqual(radius, norm(position), delta=10)
        # Through the Sun's frame, where the positions are 2e11 m from its center
        sun = self.bodies["Sun"]
        vessel_position = self.vessel.position(sun.non_rotating_reference_frame)
        mars_position = mars.position(sun.non_rotating_reference_frame)
        self.assertGreater(norm(mars_position), 1e11)
        relative = [a - b for a, b in zip(vessel_position, mars_position)]
        self.assertAlmostEqual(radius, norm(relative), delta=100)
        vessel_velocity = self.vessel.velocity(sun.non_rotating_reference_frame)
        mars_velocity = mars.velocity(sun.non_rotating_reference_frame)
        relative = [a - b for a, b in zip(vessel_velocity, mars_velocity)]
        self.assertAlmostEqual(orbit.speed, norm(relative), delta=0.1)
        # The parts keep their places in the vessel's frame, as the game keeps the vessel
        # near the origin of world space. Single precision world positions 2e11 m out would
        # be kilometers apart.
        for pad, position in zip(self.pad_part_positions, self.part_positions()):
            for a, b in zip(pad, position):
                self.assertAlmostEqual(a, b, delta=0.05)


if __name__ == "__main__":
    unittest.main()
