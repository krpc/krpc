import math
import time

import ksatest


def norm(vector):
    return math.sqrt(sum(x * x for x in vector))


class DebugCase(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.debug = cls.ksa.Debug
        cls.earth = cls.ksa.bodies["Earth"]

    @classmethod
    def tearDownClass(cls):
        cls.load_save()

    def setUp(self):
        self.load_save()
        self.vehicle = self.find_vehicle("Gemini7")

    def assertVectorAlmostEqual(self, expected, actual, delta):
        self.assertEqual(len(expected), len(actual))
        for x, y in zip(expected, actual):
            self.assertAlmostEqual(x, y, delta=delta)


class TestTeleport(DebugCase):
    def test_set_circular_orbit(self):
        self.debug.set_circular_orbit(self.vehicle, self.earth, 500000)
        orbit = self.vehicle.orbit
        self.assertEqual(self.earth, orbit.body)
        self.assertAlmostEqual(500000, orbit.periapsis_altitude, delta=100)
        self.assertAlmostEqual(500000, orbit.apoapsis_altitude, delta=100)
        self.assertLess(orbit.eccentricity, 1e-4)

    def test_set_orbit(self):
        ut = self.ksa.ut
        self.debug.set_orbit(self.vehicle, self.earth, 9e6, 0.1, 0.5, 1, 2, 0.3, ut)
        orbit = self.vehicle.orbit
        self.assertAlmostEqual(9e6, orbit.semi_major_axis, delta=10)
        self.assertAlmostEqual(0.1, orbit.eccentricity, delta=1e-6)
        self.assertAlmostEqual(0.5, orbit.inclination, delta=1e-6)
        self.assertAlmostEqual(1, orbit.longitude_of_ascending_node, delta=1e-6)
        self.assertAlmostEqual(2, orbit.argument_of_periapsis, delta=1e-6)
        expected = 0.3 + orbit.mean_motion * (self.ksa.ut - ut)
        self.assertAlmostEqual(expected, orbit.mean_anomaly, delta=1e-3)

    def test_set_orbit_around_the_moon(self):
        moon = self.ksa.bodies["Luna"]
        self.debug.set_orbit(self.vehicle, moon, 3e6, 0, 0, 0, 0, 0, self.ksa.ut)
        self.assertEqual(moon, self.vehicle.orbit.body)
        self.assertAlmostEqual(3e6, self.vehicle.orbit.semi_major_axis, delta=10)

    def test_invalid_orbit(self):
        with self.assertRaises(ValueError):
            self.debug.set_orbit(
                self.vehicle, self.earth, 9e6, 1, 0, 0, 0, 0, self.ksa.ut
            )

    def test_set_landed(self):
        self.debug.set_landed(self.vehicle, self.earth, 10, 20)
        flight = self.vehicle.flight
        self.assertAlmostEqual(10, flight.latitude, delta=0.01)
        self.assertAlmostEqual(20, flight.longitude, delta=0.01)
        self.assertLess(flight.surface_altitude, 50)
        self.assertLess(norm(self.vehicle.velocity(self.earth.reference_frame)), 5)

    def test_set_landed_on_the_star(self):
        with self.assertRaises(ValueError):
            self.debug.set_landed(self.vehicle, self.ksa.bodies["Sol"], 0, 0)


class TestState(DebugCase):
    def setUp(self):
        super().setUp()
        # The game pauses at the next frame
        self.ksa.simulation_speed = 0

        def paused():
            ut = self.ksa.ut
            time.sleep(0.1)
            return self.ksa.ut == ut

        ksatest.wait_until(paused)
        self.frame = self.earth.non_rotating_reference_frame

    def test_set_position(self):
        position = self.vehicle.position(self.frame)
        velocity = self.vehicle.velocity(self.frame)
        target = tuple(x * 1.01 for x in position)
        self.debug.set_position(self.vehicle, target, self.frame)
        self.assertVectorAlmostEqual(target, self.vehicle.position(self.frame), 1)
        self.assertVectorAlmostEqual(velocity, self.vehicle.velocity(self.frame), 0.01)

    def test_set_position_beside_a_vehicle(self):
        rocket = self.find_vehicle("Rocket")
        self.debug.set_position(self.vehicle, (0, 0, 100), rocket.reference_frame)
        self.assertVectorAlmostEqual(
            (0, 0, 100), self.vehicle.position(rocket.reference_frame), 1
        )

    def test_set_velocity(self):
        position = self.vehicle.position(self.frame)
        velocity = tuple(x * 1.01 for x in self.vehicle.velocity(self.frame))
        self.debug.set_velocity(self.vehicle, velocity, self.frame)
        self.assertVectorAlmostEqual(velocity, self.vehicle.velocity(self.frame), 0.01)
        self.assertVectorAlmostEqual(position, self.vehicle.position(self.frame), 1)

    def test_set_rotation(self):
        half = math.sqrt(0.5)
        rotation = (half, 0, 0, half)
        self.debug.set_rotation(self.vehicle, rotation, self.frame)
        actual = self.vehicle.rotation(self.frame)
        # A quaternion and its negation are the same rotation
        if sum(x * y for x, y in zip(rotation, actual)) < 0:
            actual = tuple(-x for x in actual)
        self.assertVectorAlmostEqual(rotation, actual, 1e-4)

    def test_set_angular_velocity(self):
        frame = self.frame
        self.debug.set_angular_velocity(self.vehicle, (0, 0.1, 0), frame)
        self.assertVectorAlmostEqual(
            (0, 0.1, 0), self.vehicle.angular_velocity(frame), 1e-4
        )
        self.debug.set_angular_velocity(self.vehicle, (0, 0, 0), frame)
        self.assertVectorAlmostEqual(
            (0, 0, 0), self.vehicle.angular_velocity(frame), 1e-4
        )


class TestVehicles(DebugCase):
    def test_empty_and_fill_resources(self):
        mass = self.vehicle.propellant_mass
        self.assertGreater(mass, 0)
        self.debug.empty_resources(self.vehicle)
        self.assertEqual(0, self.vehicle.propellant_mass)
        self.debug.fill_resources(self.vehicle)
        self.assertGreaterEqual(self.vehicle.propellant_mass, mass)

    def test_duplicate_vehicle(self):
        copy = self.debug.duplicate_vehicle(self.vehicle, "Gemini7Copy")
        self.assertEqual("Gemini7Copy", copy.name)
        self.assertNotEqual(self.vehicle, copy)
        self.assertIn(copy, self.ksa.vehicles)
        self.assertEqual(len(self.vehicle.parts), len(copy.parts))
        distance = norm(copy.position(self.vehicle.reference_frame))
        self.assertGreater(distance, 0)
        self.assertLess(distance, 1000)

    def test_duplicate_vehicle_name_in_use(self):
        with self.assertRaises(ValueError):
            self.debug.duplicate_vehicle(self.vehicle, "Rocket")

    def test_explode_vehicle(self):
        (kitten,) = self.vehicle.crew
        self.debug.explode_vehicle(self.vehicle)
        self.assertNotIn("Gemini7", [v.name for v in self.ksa.vehicles])
        self.assertTrue(kitten.killed_in_action)

    def test_velocity_arrows(self):
        self.debug.show_velocity_arrows(True)
        self.debug.show_velocity_arrows(False)


class TestCommands(DebugCase):
    def test_run_command(self):
        output = self.debug.run_command("help")
        self.assertGreater(len(output), 0)

    def test_run_command_changes_the_game(self):
        self.debug.run_command("destroy Gemini7")
        ksatest.wait_until(lambda: "Gemini7" not in [v.name for v in self.ksa.vehicles])

    def test_unknown_command(self):
        with self.assertRaises(ValueError):
            self.debug.run_command("nosuchcommand")

    def test_invalid_arguments(self):
        with self.assertRaises(ValueError):
            self.debug.run_command("destroy")
