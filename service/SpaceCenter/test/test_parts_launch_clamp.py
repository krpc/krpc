import unittest
import krpctest
import krpc
import time

class TestPartsLaunchClamp(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        krpctest.new_save()
        krpctest.launch_vessel_from_vab('PartsSolarPanel')
        krpctest.remove_other_vessels()
        cls.conn = krpctest.connect(name='TestPartsLaunchClamp')
        cls.vessel = cls.conn.space_center.active_vessel
        cls.parts = cls.vessel.parts

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_launch_clamp(self):
        clamp = self.parts.launch_clamps[0]
        clamp.release()

if __name__ == "__main__":
    unittest.main()
