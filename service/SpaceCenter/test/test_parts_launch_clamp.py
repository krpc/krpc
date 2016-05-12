import unittest
import krpctest

class TestPartsLaunchClamp(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        krpctest.new_save()
        krpctest.launch_vessel_from_vab('PartsSolarPanel')
        krpctest.remove_other_vessels()
        cls.conn = krpctest.connect(cls)
        cls.clamp = cls.conn.space_center.active_vessel.parts.launch_clamps[0]

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_launch_clamp(self):
        #TODO: improve this test
        self.clamp.release()

if __name__ == '__main__':
    unittest.main()
