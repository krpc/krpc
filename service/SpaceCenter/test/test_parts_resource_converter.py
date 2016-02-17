import unittest
import testingtools
import krpc
import time

class TestPartsResourceConverter(testingtools.TestCase):

    @classmethod
    def setUpClass(cls):
        if testingtools.connect().space_center.active_vessel.name != 'PartsHarvester':
            testingtools.new_save()
            testingtools.launch_vessel_from_vab('PartsHarvester')
            testingtools.remove_other_vessels()
        cls.conn = testingtools.connect(name='TestPartsResourceConverter')
        cls.vessel = cls.conn.space_center.active_vessel
        cls.parts = cls.vessel.parts

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    #TODO: add tests

if __name__ == "__main__":
    unittest.main()
