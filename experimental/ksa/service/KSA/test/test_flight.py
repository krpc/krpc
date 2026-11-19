import math
import time

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
        self.assertEqual(0, self.flight.static_air_temperature)
        self.assertEqual(0, self.flight.speed_of_sound)
        self.assertEqual(0, self.flight.mach)
        self.assertEqual(self.ksa.OutsideAir.no_atmosphere, self.flight.outside_air)

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


class TestBuoyancy(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        cls.earth = cls.ksa.bodies["Earth"]

    def weight(self, vehicle):
        r = vehicle.flight.mean_altitude + self.earth.radius
        return vehicle.mass * self.earth.gravitational_parameter / (r * r)

    def test_in_orbit(self):
        vehicle = self.find_vehicle("Gemini7")
        flight = vehicle.flight
        self.assertEqual(0, flight.depth)
        self.assertEqual(0, flight.submerged_portion)
        self.assertLess(flight.buoyant_force, self.weight(vehicle) * 1e-6)


class TestSurfaceBuoyancy(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        cls.earth = cls.ksa.bodies["Earth"]
        cls.vehicle = cls.find_vehicle("Gemini7")
        cls.rocket = cls.find_vehicle("Rocket")
        # The middle of the Atlantic, and Kansas
        cls.ksa.Debug.set_landed(cls.vehicle, cls.earth, 0, -30)
        cls.ksa.Debug.set_landed(cls.rocket, cls.earth, 39, -98)
        time.sleep(10)

    @classmethod
    def tearDownClass(cls):
        cls.load_save()

    def test_on_land(self):
        # The air the rocket displaces holds up a small part of its weight
        flight = self.rocket.flight
        self.assertEqual(0, flight.depth)
        self.assertEqual(0, flight.submerged_portion)
        r = flight.mean_altitude + self.earth.radius
        weight = self.rocket.mass * self.earth.gravitational_parameter / (r * r)
        self.assertGreater(flight.buoyant_force, 0)
        self.assertLess(flight.buoyant_force, weight * 0.1)

    def test_in_ocean(self):
        flight = self.vehicle.flight
        self.assertTrue(self.earth.has_ocean)
        self.assertGreater(flight.submerged_portion, 0)
        self.assertLessEqual(flight.submerged_portion, 1)
        self.assertGreaterEqual(flight.depth, 0)
        self.assertGreater(flight.buoyant_force, 0)
        if self.vehicle.situation == self.ksa.Situation.floating:
            # Floating at rest, the buoyant force holds up the weight
            r = flight.mean_altitude + self.earth.radius
            weight = self.vehicle.mass * self.earth.gravitational_parameter / (r * r)
            self.assertAlmostEqual(weight, flight.buoyant_force, delta=weight * 0.1)


class TestAerodynamicsInOrbit(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        cls.vehicle = cls.find_vehicle("Gemini7")

    def test_no_loads(self):
        flight = self.vehicle.flight
        frame = self.vehicle.reference_frame
        self.assertEqual((0, 0, 0), flight.aerodynamic_force(frame))
        self.assertEqual((0, 0, 0), flight.aerodynamic_torque(frame))
        self.assertEqual(0, flight.drag)
        self.assertEqual(0, flight.lift)


class TestAerodynamicsInFlight(ksatest.TestCase):
    """Places Gemini7 10 km above Kansas, flying east at 250 m/s, and pauses the game."""

    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        cls.earth = cls.ksa.bodies["Earth"]
        cls.vehicle = cls.find_vehicle("Gemini7")
        cls.frame = cls.earth.reference_frame
        surface = cls.earth.surface_position(39, -98, cls.frame)
        east = cls.earth.surface_position(39, -97.99, cls.frame)
        up = tuple(x / norm(surface) for x in surface)
        position = tuple(x + 10e3 * u for x, u in zip(surface, up))
        east = tuple(e - s for e, s in zip(east, surface))
        velocity = tuple(250 * x / norm(east) for x in east)
        # Landed first, so the vehicle moves with the surface, as orbital speed this low
        # destroys it
        cls.ksa.Debug.set_landed(cls.vehicle, cls.earth, 39, -98)
        cls.ksa.Debug.set_position(cls.vehicle, position, cls.frame)
        cls.ksa.Debug.set_velocity(cls.vehicle, velocity, cls.frame)
        time.sleep(1)
        cls.ksa.simulation_speed = 0
        cls.flight = cls.vehicle.flight

    @classmethod
    def tearDownClass(cls):
        cls.ksa.simulation_speed = 1
        cls.load_save()

    def test_against_acceleration(self):
        # With the engines off, the air gives the vehicle its proper acceleration
        force = norm(self.flight.aerodynamic_force(self.frame))
        acceleration = self.flight.g_force * 9.80665
        self.assertGreater(force, 0)
        self.assertAlmostEqual(
            self.vehicle.mass * acceleration, force, delta=force * 0.05
        )

    def test_lift_and_drag(self):
        force = norm(self.flight.aerodynamic_force(self.frame))
        drag = self.flight.drag
        lift = self.flight.lift
        self.assertGreater(drag, 0)
        self.assertGreaterEqual(lift, 0)
        self.assertAlmostEqual(force, math.hypot(lift, drag), delta=force * 1e-3)
        # Drag over dynamic pressure gives a drag area within the vehicle's size, broadside
        # at most
        area = drag / self.flight.dynamic_pressure
        self.assertGreater(area, 1)
        self.assertLess(area, 100)

    def test_torque(self):
        torque = self.flight.aerodynamic_torque(self.vehicle.reference_frame)
        self.assertTrue(all(math.isfinite(x) for x in torque))

    def test_simulate_here(self):
        position = self.vehicle.position(self.frame)
        velocity = self.vehicle.velocity(self.frame)
        expected = self.flight.aerodynamic_force(self.frame)
        actual = self.flight.simulate_aerodynamic_force_at(
            self.earth, position, velocity, self.frame
        )
        for x, y in zip(expected, actual):
            self.assertAlmostEqual(x, y, delta=norm(expected) * 0.02)

    def test_simulate_elsewhere(self):
        position = self.vehicle.position(self.frame)
        velocity = self.vehicle.velocity(self.frame)
        here = norm(
            self.flight.simulate_aerodynamic_force_at(
                self.earth, position, velocity, self.frame
            )
        )
        # Higher, the air is thinner
        up = tuple(x / norm(position) for x in position)
        higher = tuple(x + 10e3 * u for x, u in zip(position, up))
        self.assertLess(
            norm(
                self.flight.simulate_aerodynamic_force_at(
                    self.earth, higher, velocity, self.frame
                )
            ),
            here / 2,
        )
        # Above the atmosphere, there is none
        space = tuple(x + 500e3 * u for x, u in zip(position, up))
        self.assertEqual(
            (0, 0, 0),
            self.flight.simulate_aerodynamic_force_at(
                self.earth, space, velocity, self.frame
            ),
        )
        # At rest relative to the surface, there is none
        self.assertEqual(
            (0, 0, 0),
            self.flight.simulate_aerodynamic_force_at(
                self.earth, position, (0, 0, 0), self.frame
            ),
        )
