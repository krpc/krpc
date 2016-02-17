import unittest
import testingtools
import krpc
import time

class TestPartsResourceHarvester(testingtools.TestCase):

    @classmethod
    def setUpClass(cls):
        if testingtools.connect().space_center.active_vessel.name != 'PartsHarvester':
            testingtools.new_save()
            testingtools.launch_vessel_from_vab('PartsHarvester')
            testingtools.remove_other_vessels()
        cls.conn = testingtools.connect(name='TestPartsResourceHarvester')
        cls.vessel = cls.conn.space_center.active_vessel
        cls.parts = cls.vessel.parts

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_operate(self):
        drill = next(iter(self.parts.resource_harvesters))
        self.assertFalse(drill.deployed)
        self.assertFalse(drill.active)
        drill.deploy()
        time.sleep(0.1)
        self.assertTrue(drill.deployed)
        self.assertFalse(drill.active)
        drill.start()
        time.sleep(0.1)
        self.assertTrue(drill.deployed)
        self.assertTrue(drill.active)
        drill.stop()
        time.sleep(0.1)
        self.assertTrue(drill.deployed)
        self.assertFalse(drill.active)
        drill.retract()
        time.sleep(0.1)
        self.assertFalse(drill.deployed)
        self.assertFalse(drill.active)

if __name__ == "__main__":
    unittest.main()
