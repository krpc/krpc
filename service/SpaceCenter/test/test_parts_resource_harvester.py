import unittest
import time
import krpctest

class TestPartsResourceHarvester(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        if krpctest.connect().space_center.active_vessel.name != 'PartsHarvester':
            krpctest.new_save()
            krpctest.launch_vessel_from_vab('PartsHarvester')
            krpctest.remove_other_vessels()
        cls.conn = krpctest.connect(cls)
        cls.State = cls.conn.space_center.ResourceHarvesterState
        parts = cls.conn.space_center.active_vessel.parts
        cls.drill = parts.with_title('\'Drill-O-Matic\' Mining Excavator')[0].resource_harvester

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def check_inactive_properties(self):
        self.assertEqual(0, self.drill.extraction_rate)
        self.assertEqual(0, self.drill.thermal_efficiency)
        self.assertGreater(self.drill.core_temperature, 0)
        self.assertEqual(500, self.drill.optimum_core_temperature)

    def test_operate(self):
        self.assertEqual(self.State.retracted, self.drill.state)
        self.assertFalse(self.drill.deployed)
        self.assertFalse(self.drill.active)
        self.check_inactive_properties()

        self.drill.deployed = True
        time.sleep(0.1)

        self.assertEqual(self.State.deploying, self.drill.state)
        self.assertFalse(self.drill.deployed)
        self.assertFalse(self.drill.active)
        self.check_inactive_properties()

        while self.drill.state == self.State.deploying:
            time.sleep(0.1)

        self.assertEqual(self.State.deployed, self.drill.state)
        self.assertTrue(self.drill.deployed)
        self.assertFalse(self.drill.active)
        self.check_inactive_properties()

        self.drill.active = True
        time.sleep(3)

        self.assertEqual(self.State.active, self.drill.state)
        self.assertTrue(self.drill.deployed)
        self.assertTrue(self.drill.active)
        self.assertGreater(self.drill.extraction_rate, 0)
        self.assertGreater(self.drill.thermal_efficiency, 0)
        self.assertLess(self.drill.thermal_efficiency, 100)
        self.assertGreater(self.drill.core_temperature, 0)
        self.assertEqual(500, self.drill.optimum_core_temperature)

        self.drill.active = False
        time.sleep(3)

        self.assertEqual(self.State.deployed, self.drill.state)
        self.assertTrue(self.drill.deployed)
        self.assertFalse(self.drill.active)
        self.check_inactive_properties()

        self.drill.deployed = False
        time.sleep(0.1)

        self.assertEqual(self.State.retracting, self.drill.state)
        self.assertFalse(self.drill.deployed)
        self.assertFalse(self.drill.active)
        self.check_inactive_properties()

        while self.drill.state == self.State.retracting:
            time.sleep(0.1)

        self.assertEqual(self.State.retracted, self.drill.state)
        self.assertFalse(self.drill.deployed)
        self.assertFalse(self.drill.active)
        self.check_inactive_properties()

    def test_activate_when_not_deployed(self):
        self.assertFalse(self.drill.deployed)
        self.assertFalse(self.drill.active)
        self.drill.active = True
        self.assertFalse(self.drill.deployed)
        self.assertFalse(self.drill.active)

if __name__ == '__main__':
    unittest.main()
