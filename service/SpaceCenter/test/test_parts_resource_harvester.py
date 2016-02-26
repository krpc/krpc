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
        cls.state = cls.conn.space_center.ResourceHarvesterState
        cls.drill = next(iter(filter(lambda e: e.part.title == '\'Drill-O-Matic\' Mining Excavator', cls.parts.resource_harvesters)))

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def check_inactive_properties(self):
        self.assertEqual(0, self.drill.extraction_rate)
        self.assertEqual(0, self.drill.thermal_efficiency)
        self.assertGreater(self.drill.core_temperature, 0)
        self.assertEqual(500, self.drill.optimum_core_temperature)

    def test_operate(self):
        self.assertEqual(self.state.retracted, self.drill.state)
        self.assertFalse(self.drill.deployed)
        self.assertFalse(self.drill.active)
        self.check_inactive_properties()

        self.drill.deployed = True
        time.sleep(0.1)

        self.assertEqual(self.state.deploying, self.drill.state)
        self.assertFalse(self.drill.deployed)
        self.assertFalse(self.drill.active)
        self.check_inactive_properties()

        while self.drill.state == self.state.deploying:
            time.sleep(0.1)

        self.assertEqual(self.state.deployed, self.drill.state)
        self.assertTrue(self.drill.deployed)
        self.assertFalse(self.drill.active)
        self.check_inactive_properties()

        self.drill.active = True
        time.sleep(3)

        self.assertEqual(self.state.active, self.drill.state)
        self.assertTrue(self.drill.deployed)
        self.assertTrue(self.drill.active)
        self.assertGreater(self.drill.extraction_rate, 0)
        self.assertGreater(self.drill.thermal_efficiency, 0)
        self.assertLess(self.drill.thermal_efficiency, 100)
        self.assertGreater(self.drill.core_temperature, 0)
        self.assertEqual(500, self.drill.optimum_core_temperature)

        self.drill.active = False
        time.sleep(3)

        self.assertEqual(self.state.deployed, self.drill.state)
        self.assertTrue(self.drill.deployed)
        self.assertFalse(self.drill.active)
        self.check_inactive_properties()

        self.drill.deployed = False
        time.sleep(0.1)

        self.assertEqual(self.state.retracting, self.drill.state)
        self.assertFalse(self.drill.deployed)
        self.assertFalse(self.drill.active)
        self.check_inactive_properties()

        while self.drill.state == self.state.retracting:
            time.sleep(0.1)

        self.assertEqual(self.state.retracted, self.drill.state)
        self.assertFalse(self.drill.deployed)
        self.assertFalse(self.drill.active)
        self.check_inactive_properties()

    def test_activate_when_not_deployed(self):
        self.assertFalse(self.drill.deployed)
        self.assertFalse(self.drill.active)
        self.drill.active = True
        self.assertFalse(self.drill.deployed)
        self.assertFalse(self.drill.active)

if __name__ == "__main__":
    unittest.main()
