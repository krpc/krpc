import unittest
import testingtools
from testingtools import load_save
import krpc
import time

class TestTime(testingtools.TestCase):

    def test_basic(self):
        load_save('basic')
        ksp = krpc.connect()
        ut = ksp.space_center.ut
        met = ksp.space_center.active_vessel.met
        time.sleep(3)
        self.assertClose(ut+3, ksp.space_center.ut, error=0.25)
        self.assertClose(met+3, ksp.space_center.active_vessel.met, error=0.25)
        self.assertGreater(ksp.space_center.ut, ksp.space_center.active_vessel.met)

if __name__ == "__main__":
    unittest.main()
