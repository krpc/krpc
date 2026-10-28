import contextlib
import math
import time

import ksatest

AU = 1.496e11


def norm(vector):
    return math.sqrt(sum(x * x for x in vector))


def dot(a, b):
    return sum(x * y for x, y in zip(a, b))


class TestReferenceFrame(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        cls.vehicle = cls.ksa.active_vehicle
        cls.flight = cls.vehicle.flight
        cls.earth = cls.ksa.bodies["Earth"]
        cls.inertial = cls.earth.non_rotating_reference_frame
        cls.rotating = cls.earth.reference_frame

    def assertVectorAlmostEqual(self, expected, actual, delta):
        self.assertEqual(len(expected), len(actual))
        for x, y in zip(expected, actual):
            self.assertAlmostEqual(x, y, delta=delta)

    def test_vehicle_position(self):
        radius = self.earth.radius + self.flight.mean_altitude
        self.assertAlmostEqual(
            radius, norm(self.vehicle.position(self.inertial)), delta=1
        )
        self.assertAlmostEqual(
            radius, norm(self.vehicle.position(self.rotating)), delta=1
        )

    def test_vehicle_velocity(self):
        self.assertAlmostEqual(
            self.vehicle.orbit.speed,
            norm(self.vehicle.velocity(self.inertial)),
            delta=0.01,
        )
        self.assertAlmostEqual(
            self.flight.speed, norm(self.vehicle.velocity(self.rotating)), delta=0.01
        )
        for frame in (
            self.vehicle.reference_frame,
            self.vehicle.surface_reference_frame,
            self.vehicle.orbital_reference_frame,
        ):
            self.assertVectorAlmostEqual((0, 0, 0), self.vehicle.velocity(frame), 1e-6)

    def test_vehicle_frame(self):
        frame = self.vehicle.reference_frame
        self.assertVectorAlmostEqual((0, 0, 0), self.vehicle.position(frame), 1e-6)
        self.assertVectorAlmostEqual((1, 0, 0), self.vehicle.direction(frame), 1e-9)
        self.assertVectorAlmostEqual((0, 0, 0, 1), self.vehicle.rotation(frame), 1e-9)

    def test_direction_matches_attitude(self):
        # The surface frame is east, north, up
        direction = self.vehicle.direction(self.vehicle.surface_reference_frame)
        pitch = math.radians(self.flight.pitch)
        heading = math.radians(self.flight.heading)
        expected = (
            math.cos(pitch) * math.sin(heading),
            math.cos(pitch) * math.cos(heading),
            math.sin(pitch),
        )
        self.assertVectorAlmostEqual(expected, direction, 1e-3)

    def test_surface_up(self):
        up = self.ksa.transform_direction(
            (0, 0, 1), self.vehicle.surface_reference_frame, self.rotating
        )
        position = self.vehicle.position(self.rotating)
        self.assertAlmostEqual(1, dot(up, position) / norm(position), delta=1e-9)

    def test_orbital_frame(self):
        velocity = self.vehicle.velocity(self.inertial)
        prograde = tuple(x / norm(velocity) for x in velocity)
        self.assertVectorAlmostEqual(
            (1, 0, 0),
            self.ksa.transform_direction(
                prograde, self.inertial, self.vehicle.orbital_reference_frame
            ),
            1e-9,
        )

    def test_body_frames(self):
        for frame in (self.inertial, self.rotating):
            self.assertVectorAlmostEqual((0, 0, 0), self.earth.position(frame), 1e-6)
            self.assertVectorAlmostEqual((0, 0, 0), self.earth.velocity(frame), 1e-6)
        self.assertVectorAlmostEqual(
            (0, 0, self.earth.rotational_speed),
            self.earth.angular_velocity(self.inertial),
            1e-12,
        )
        self.assertVectorAlmostEqual(
            (0, 0, 0), self.earth.angular_velocity(self.rotating), 1e-12
        )
        distance = norm(self.ksa.bodies["Sol"].position(self.inertial))
        self.assertAlmostEqual(1, distance / AU, delta=0.02)

    def test_surface_position(self):
        below = self.earth.surface_position(
            self.flight.latitude, self.flight.longitude, self.rotating
        )
        position = self.vehicle.position(self.rotating)
        self.assertAlmostEqual(self.earth.radius, norm(below), delta=1)
        self.assertAlmostEqual(
            1, dot(below, position) / (norm(below) * norm(position)), delta=1e-9
        )

    def test_transform_position(self):
        with self.paused():
            expected = self.vehicle.position(self.inertial)
            actual = self.ksa.transform_position(
                (0, 0, 0), self.vehicle.reference_frame, self.inertial
            )
            position = (1e6, 2e6, 3e6)
            round_trip = self.ksa.transform_position(
                self.ksa.transform_position(position, self.rotating, self.inertial),
                self.inertial,
                self.rotating,
            )
        self.assertVectorAlmostEqual(expected, actual, 1e-3)
        self.assertVectorAlmostEqual(position, round_trip, 1e-3)

    def test_transform_velocity(self):
        with self.paused():
            expected = self.vehicle.velocity(self.inertial)
            actual = self.ksa.transform_velocity(
                (0, 0, 0), (0, 0, 0), self.vehicle.reference_frame, self.inertial
            )
        self.assertVectorAlmostEqual(expected, actual, 1e-6)
        # A point fixed to the surface moves with the body's rotation
        position = (self.earth.radius, 0, 0)
        velocity = self.ksa.transform_velocity(
            position, (0, 0, 0), self.rotating, self.inertial
        )
        self.assertAlmostEqual(
            self.earth.radius * self.earth.rotational_speed, norm(velocity), delta=1e-3
        )

    def test_transform_rotation(self):
        with self.paused():
            rotation = self.vehicle.rotation(self.inertial)
            expected = self.vehicle.rotation(self.rotating)
            actual = self.ksa.transform_rotation(rotation, self.inertial, self.rotating)
        self.assertVectorAlmostEqual(expected, actual, 1e-9)

    def test_equality(self):
        self.assertEqual(self.rotating, self.earth.reference_frame)
        self.assertNotEqual(self.rotating, self.inertial)

    @contextlib.contextmanager
    def paused(self):
        """Pause the game, so that calls see the same game time."""
        self.ksa.simulation_speed = 0
        try:
            # The game stops at the end of the step in progress, and calls in the same
            # server update see the same time whether it has stopped or not
            def stopped():
                ut = self.ksa.ut
                time.sleep(0.1)
                return self.ksa.ut == ut

            ksatest.wait_until(stopped)
            yield
        finally:
            self.ksa.simulation_speed = 1

    def test_relative_position(self):
        frame = self.ksa.ReferenceFrame.create_relative(
            self.rotating, position=(0, 0, 1000)
        )
        with self.paused():
            expected = self.vehicle.position(self.rotating)
            actual = self.vehicle.position(frame)
        self.assertVectorAlmostEqual(
            (expected[0], expected[1], expected[2] - 1000), actual, 1e-3
        )

    def test_relative_rotation(self):
        # A quarter turn about z takes the frame's x axis to the parent's y axis
        half = math.sqrt(0.5)
        frame = self.ksa.ReferenceFrame.create_relative(
            self.rotating, rotation=(0, 0, half, half)
        )
        with self.paused():
            x, y, z = self.vehicle.position(self.rotating)
            actual = self.vehicle.position(frame)
        self.assertVectorAlmostEqual((y, -x, z), actual, 1e-3)
        self.assertVectorAlmostEqual(
            (0, 1, 0),
            self.ksa.transform_direction((1, 0, 0), frame, self.rotating),
            1e-9,
        )

    def test_relative_velocity(self):
        frame = self.ksa.ReferenceFrame.create_relative(
            self.inertial, velocity=(10, 0, 0)
        )
        with self.paused():
            expected = self.vehicle.velocity(self.inertial)
            actual = self.vehicle.velocity(frame)
        self.assertVectorAlmostEqual(
            (expected[0] - 10, expected[1], expected[2]), actual, 1e-6
        )

    def test_relative_angular_velocity(self):
        # The inertial frame spun up to the body's rotation is the rotating frame, at the
        # instant their axes line up
        spin = self.earth.angular_velocity(self.inertial)
        with self.paused():
            rotation = self.ksa.transform_rotation(
                (0, 0, 0, 1), self.rotating, self.inertial
            )
            frame = self.ksa.ReferenceFrame.create_relative(
                self.inertial, rotation=rotation, angular_velocity=spin
            )
            expected = self.vehicle.velocity(self.rotating)
            actual = self.vehicle.velocity(frame)
        self.assertVectorAlmostEqual(expected, actual, 1e-3)

    def test_hybrid_surface_velocity(self):
        # The body's rotating frame, with the axes of the vehicle's surface frame
        frame = self.ksa.ReferenceFrame.create_hybrid(
            self.rotating, rotation=self.vehicle.surface_reference_frame
        )
        with self.paused():
            velocity = self.vehicle.velocity(frame)
            expected = self.ksa.transform_direction(
                self.vehicle.velocity(self.rotating),
                self.rotating,
                self.vehicle.surface_reference_frame,
            )
            position = self.vehicle.position(frame)
            up = self.vehicle.position(self.rotating)
        self.assertVectorAlmostEqual(expected, velocity, 1e-3)
        self.assertAlmostEqual(self.flight.speed, norm(velocity), delta=0.1)
        self.assertAlmostEqual(norm(up), position[2], delta=1)

    def test_custom_frame_equality(self):
        create = self.ksa.ReferenceFrame.create_relative
        self.assertEqual(
            create(self.rotating, position=(1, 2, 3)),
            create(self.rotating, position=(1, 2, 3)),
        )
        self.assertNotEqual(
            create(self.rotating, position=(1, 2, 3)),
            create(self.inertial, position=(1, 2, 3)),
        )
        self.assertNotEqual(create(self.rotating, position=(1, 2, 3)), self.rotating)
        hybrid = self.ksa.ReferenceFrame.create_hybrid
        self.assertEqual(
            hybrid(self.rotating, self.inertial), hybrid(self.rotating, self.inertial)
        )
