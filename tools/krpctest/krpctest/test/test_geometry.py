import unittest
import math
import krpctest
from krpctest.geometry import \
    rad2deg, norm, normalize, dot, cross, quaternion_axis_angle, \
    quaternion_vector_mult, quaternion_mult, quaternion_conjugate


class TestGeometry(krpctest.TestCase):

    def test_rad2deg(self):
        self.assertAlmostEqual(0, 0)
        self.assertAlmostEqual(90, rad2deg(math.pi/2))
        self.assertAlmostEqual(180, rad2deg(math.pi))
        self.assertAlmostEqual(360, rad2deg(2*math.pi))

    def test_norm(self):
        self.assertAlmostEqual(0, norm((0, 0, 0)))
        self.assertAlmostEqual(1, norm((1, 0, 0)))
        self.assertAlmostEqual(2, norm((2, 0, 0)))
        self.assertAlmostEqual(math.sqrt(2), norm((1, 1, 0)))

    def test_normalize(self):
        self.assertAlmostEqual((1, 0, 0), normalize((1, 0, 0)))
        self.assertAlmostEqual((1, 0, 0), normalize((2, 0, 0)))
        self.assertAlmostEqual((1/math.sqrt(2), 1/math.sqrt(2), 0),
                               normalize((1, 1, 0)))
        self.assertAlmostEqual((1/math.sqrt(2), 0, 1/math.sqrt(2)),
                               normalize((1, 0, 1)))

    def test_dot(self):
        self.assertAlmostEqual(0, dot((1, 0, 0), (0, 1, 0)))
        self.assertAlmostEqual(1, dot((1, 0, 0), (1, 0, 0)))

    def test_cross(self):
        self.assertAlmostEqual((1, 0, 0), cross((0, 1, 0), (0, 0, 1)))

    def test_quaternion_axis_angle(self):
        self.assertAlmostEqual(
            (0, 0, 0, 1), quaternion_axis_angle((1, 0, 0), 0))
        self.assertAlmostEqual(
            (0, 0, 0, 1), quaternion_axis_angle((0, 1, 0), 0))
        self.assertAlmostEqual(
            (0, 0, 0, 1), quaternion_axis_angle((0, 0, 1), 0))
        self.assertAlmostEqual(
            (0, 0, 0, -1), quaternion_axis_angle((1, 0, 0), 2*math.pi))
        self.assertAlmostEqual(
            (0, 0, 0, -1), quaternion_axis_angle((0, 1, 0), 2*math.pi))
        self.assertAlmostEqual(
            (0, 0, 0, -1), quaternion_axis_angle((0, 0, 1), 2*math.pi))

    def test_quaternion_vector_mult(self):
        self.assertAlmostEqual((1, 0, 0),
                               quaternion_vector_mult((0, 0, 0, 1), (1, 0, 0)))
        self.assertAlmostEqual((0, 1, 0),
                               quaternion_vector_mult((0, 0, 0, 1), (0, 1, 0)))
        self.assertAlmostEqual((0, 0, 1),
                               quaternion_vector_mult((0, 0, 0, 1), (0, 0, 1)))
        q = quaternion_axis_angle((1, 0, 0), math.pi)
        self.assertAlmostEqual(
            (1, 0, 0), quaternion_vector_mult(q, (1, 0, 0)))
        self.assertAlmostEqual(
            (0, -1, 0), quaternion_vector_mult(q, (0, 1, 0)))
        self.assertAlmostEqual(
            (0, 0, -1), quaternion_vector_mult(q, (0, 0, 1)))

    def test_quaternion_conjugate(self):
        self.assertAlmostEqual(
            (-1, -2, -3, 4), quaternion_conjugate((1, 2, 3, 4)))

    def test_quaternion_mult(self):
        self.assertAlmostEqual(
            (24, 48, 48, -6), quaternion_mult((1, 2, 3, 4), (5, 6, 7, 8)))
        self.assertAlmostEqual(
            (40, 16, -35, -27), quaternion_mult((5, 1, 0, -2), (3, -6, 1, 9)))


if __name__ == '__main__':
    unittest.main()
