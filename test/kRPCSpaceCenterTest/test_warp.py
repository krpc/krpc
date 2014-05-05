import unittest
import testingtools
from testingtools import load_save
import krpc

class TestWarp(testingtools.TestCase):

    def test_basic(self):
        load_save('basic')
        ksp = krpc.connect()
        t = ksp.space_center.ut + (5*60)
        ksp.space_center.warp_to(t)
        self.assertClose(t, ksp.space_center.ut, error=2)

if __name__ == "__main__":
    unittest.main()
