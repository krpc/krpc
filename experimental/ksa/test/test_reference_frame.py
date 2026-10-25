import math

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
        self.assertVectorAlmostEqual(
            self.vehicle.position(self.inertial),
            self.ksa.transform_position(
                (0, 0, 0), self.vehicle.reference_frame, self.inertial
            ),
            1e-3,
        )
        position = (1e6, 2e6, 3e6)
        round_trip = self.ksa.transform_position(
            self.ksa.transform_position(position, self.rotating, self.inertial),
            self.inertial,
            self.rotating,
        )
        self.assertVectorAlmostEqual(position, round_trip, 1e-3)

    def test_transform_velocity(self):
        # Paused, so both calls see the same game time
        self.ksa.simulation_speed = 0
        try:
            self.assertVectorAlmostEqual(
                self.vehicle.velocity(self.inertial),
                self.ksa.transform_velocity(
                    (0, 0, 0), (0, 0, 0), self.vehicle.reference_frame, self.inertial
                ),
                1e-6,
            )
        finally:
            self.ksa.simulation_speed = 1
        # A point fixed to the surface moves with the body's rotation
        position = (self.earth.radius, 0, 0)
        velocity = self.ksa.transform_velocity(
            position, (0, 0, 0), self.rotating, self.inertial
        )
        self.assertAlmostEqual(
            self.earth.radius * self.earth.rotational_speed, norm(velocity), delta=1e-3
        )

    def test_transform_rotation(self):
        rotation = self.vehicle.rotation(self.inertial)
        self.assertVectorAlmostEqual(
            self.vehicle.rotation(self.rotating),
            self.ksa.transform_rotation(rotation, self.inertial, self.rotating),
            1e-9,
        )

    def test_equality(self):
        self.assertEqual(self.rotating, self.earth.reference_frame)
        self.assertNotEqual(self.rotating, self.inertial)
