import unittest
import testingtools
from mathtools import *
import math

class TestMathTools(testingtools.TestCase):

    def test_quaternion_axis_angle(self):
        self.assertEqual((0,0,0,1), quaternion_axis_angle((1,0,0), 0))
        self.assertEqual((0,0,0,1), quaternion_axis_angle((0,1,0), 0))
        self.assertEqual((0,0,0,1), quaternion_axis_angle((0,0,1), 0))
        self.assertClose((0,0,0,-1), quaternion_axis_angle((1,0,0), 2*math.pi))
        self.assertClose((0,0,0,-1), quaternion_axis_angle((0,1,0), 2*math.pi))
        self.assertClose((0,0,0,-1), quaternion_axis_angle((0,0,1), 2*math.pi))

    def test_quaternion_vector_mult(self):
        self.assertEqual((1,0,0), quaternion_vector_mult((0,0,0,1),(1,0,0)))
        self.assertEqual((0,1,0), quaternion_vector_mult((0,0,0,1),(0,1,0)))
        self.assertEqual((0,0,1), quaternion_vector_mult((0,0,0,1),(0,0,1)))
        q = quaternion_axis_angle((1,0,0),math.pi)
        self.assertEqual((1,0,0), quaternion_vector_mult(q,(1,0,0)))
        self.assertClose((0,-1,0), quaternion_vector_mult(q,(0,1,0)))
        self.assertClose((0,0,-1), quaternion_vector_mult(q,(0,0,1)))

    def test_quaternion_conjugate(self):
        self.assertEqual((-1,-2,-3,4),quaternion_conjugate((1,2,3,4)))

    def test_quaternion_mult(self):
        self.assertEqual((24,48,48,-6), quaternion_mult((1,2,3,4),(5,6,7,8)))
        self.assertEqual((40,16,-35,-27), quaternion_mult((5,1,0,-2),(3,-6,1,9)))

if __name__ == "__main__":
    unittest.main()
