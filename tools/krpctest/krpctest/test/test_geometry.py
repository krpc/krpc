import unittest
import math
import krpctest
from krpctest.geometry import rad2deg, norm, normalize, dot, cross
from krpctest.geometry import quaternion_axis_angle, quaternion_vector_mult, quaternion_mult, quaternion_conjugate

class TestGeometry(krpctest.TestCase):

    def test_rad2deg(self):
        self.assertClose(0, 0)
        self.assertClose(90, rad2deg(math.pi/2))
        self.assertClose(180, rad2deg(math.pi))
        self.assertClose(360, rad2deg(2*math.pi))

    def test_norm(self):
        self.assertClose(0, norm((0, 0, 0)))
        self.assertClose(1, norm((1, 0, 0)))
        self.assertClose(2, norm((2, 0, 0)))
        self.assertClose(math.sqrt(2), norm((1, 1, 0)))

    def test_normalize(self):
        self.assertClose((1, 0, 0), normalize((1, 0, 0)))
        self.assertClose((1, 0, 0), normalize((2, 0, 0)))
        self.assertClose((1/math.sqrt(2), 1/math.sqrt(2), 0), normalize((1, 1, 0)))
        self.assertClose((1/math.sqrt(2), 0, 1/math.sqrt(2)), normalize((1, 0, 1)))

    def test_dot(self):
        self.assertClose(0, dot((1, 0, 0), (0, 1, 0)))
        self.assertClose(1, dot((1, 0, 0), (1, 0, 0)))

    def test_cross(self):
        self.assertClose((1, 0, 0), cross((0, 1, 0), (0, 0, 1)))

    def test_quaternion_axis_angle(self):
        self.assertEqual((0, 0, 0, 1), quaternion_axis_angle((1, 0, 0), 0))
        self.assertEqual((0, 0, 0, 1), quaternion_axis_angle((0, 1, 0), 0))
        self.assertEqual((0, 0, 0, 1), quaternion_axis_angle((0, 0, 1), 0))
        self.assertClose((0, 0, 0, -1), quaternion_axis_angle((1, 0, 0), 2*math.pi))
        self.assertClose((0, 0, 0, -1), quaternion_axis_angle((0, 1, 0), 2*math.pi))
        self.assertClose((0, 0, 0, -1), quaternion_axis_angle((0, 0, 1), 2*math.pi))

    def test_quaternion_vector_mult(self):
        self.assertEqual((1, 0, 0), quaternion_vector_mult((0, 0, 0, 1), (1, 0, 0)))
        self.assertEqual((0, 1, 0), quaternion_vector_mult((0, 0, 0, 1), (0, 1, 0)))
        self.assertEqual((0, 0, 1), quaternion_vector_mult((0, 0, 0, 1), (0, 0, 1)))
        q = quaternion_axis_angle((1, 0, 0), math.pi)
        self.assertEqual((1, 0, 0), quaternion_vector_mult(q, (1, 0, 0)))
        self.assertClose((0, -1, 0), quaternion_vector_mult(q, (0, 1, 0)))
        self.assertClose((0, 0, -1), quaternion_vector_mult(q, (0, 0, 1)))

    def test_quaternion_conjugate(self):
        self.assertEqual((-1, -2, -3, 4), quaternion_conjugate((1, 2, 3, 4)))

    def test_quaternion_mult(self):
        self.assertEqual((24, 48, 48, -6), quaternion_mult((1, 2, 3, 4), (5, 6, 7, 8)))
        self.assertEqual((40, 16, -35, -27), quaternion_mult((5, 1, 0, -2), (3, -6, 1, 9)))

if __name__ == '__main__':
    unittest.main()
