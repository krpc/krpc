import unittest

from krpctools.game.geometry import norm
from krpctools.game.ksp import TestCase


class TestBodyKopernicus(TestCase):
    """The stock system loaded through Kopernicus, with its star renamed Kerbol. The
    test overlay for Kopernicus gives the system, as a planet pack that replaces the
    star does."""

    mods = ["Kopernicus"]

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.space_center = cls.connect().space_center
        cls.bodies = cls.space_center.bodies
        cls.star = cls.bodies["Kerbol"]
        cls.kerbin = cls.bodies["Kerbin"]

    def test_bodies(self):
        self.assertNotIn("Sun", self.bodies)
        self.assertEqual(17, len(self.bodies))
        self.assertEqual("Kerbol", self.star.name)
        self.assertIsNone(self.star.orbit)
        self.assertEqual(self.star, self.kerbin.orbit.body)
        self.assertAlmostEqual(600000, self.kerbin.equatorial_radius)

    def test_star_velocity(self):
        frame = self.kerbin.non_rotating_reference_frame
        velocity = self.star.velocity(frame)
        self.assertAlmostEqual(self.kerbin.orbit.speed, norm(velocity), places=2)
        self.assertAlmostEqual(
            (0, 0, 0), self.star.velocity(self.star.non_rotating_reference_frame)
        )

    def test_orbit_around_star(self):
        orbit = self.kerbin.orbit
        frame = self.star.non_rotating_reference_frame
        self.assertAlmostEqual(orbit.speed, norm(orbit.velocity(frame)), places=2)
        ut = self.space_center.ut + 1000
        self.assertAlmostEqual(
            orbit.radius_at(ut), norm(orbit.position_at(ut, frame)), delta=1
        )
        self.assertAlmostEqual(
            orbit.speed_at(ut), norm(orbit.velocity_at(ut, frame)), places=2
        )

    def test_orbital_reference_frame(self):
        frame = self.kerbin.orbital_reference_frame
        # The frame's y axis points along Kerbin's orbital velocity, around the star
        velocity = self.kerbin.velocity(self.star.non_rotating_reference_frame)
        direction = self.space_center.transform_direction(
            (0, 1, 0), frame, self.star.non_rotating_reference_frame
        )
        speed = norm(velocity)
        for i in range(3):
            self.assertAlmostEqual(velocity[i] / speed, direction[i], places=3)

    def test_orbit_from_state_vectors(self):
        frame = self.star.non_rotating_reference_frame
        position = self.kerbin.position(frame)
        velocity = self.kerbin.velocity(frame)
        orbit = self.space_center.Orbit.create_from_position_and_velocity(
            self.star, position, velocity, self.space_center.ut, frame
        )
        self.assertEqual(self.star, orbit.body)
        self.assertAlmostEqual(
            self.kerbin.orbit.semi_major_axis,
            orbit.semi_major_axis,
            delta=self.kerbin.orbit.semi_major_axis * 1e-6,
        )


if __name__ == "__main__":
    unittest.main()
