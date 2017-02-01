import unittest
import krpctest


class TestPartsResourceHarvester(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        if cls.connect().space_center.active_vessel.name != 'PartsHarvester':
            cls.launch_vessel_from_vab('PartsHarvester')
            cls.remove_other_vessels()
        cls.state = cls.connect().space_center.ResourceHarvesterState
        parts = cls.connect().space_center.active_vessel.parts
        cls.drill = parts.with_title(
            '\'Drill-O-Matic\' Mining Excavator')[0].resource_harvester

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
        self.wait()

        self.assertEqual(self.state.deploying, self.drill.state)
        self.assertFalse(self.drill.deployed)
        self.assertFalse(self.drill.active)
        self.check_inactive_properties()

        while self.drill.state == self.state.deploying:
            self.wait()

        self.assertEqual(self.state.deployed, self.drill.state)
        self.assertTrue(self.drill.deployed)
        self.assertFalse(self.drill.active)
        self.check_inactive_properties()

        self.drill.active = True
        self.wait(3)

        self.assertEqual(self.state.active, self.drill.state)
        self.assertTrue(self.drill.deployed)
        self.assertTrue(self.drill.active)
        self.assertGreater(self.drill.extraction_rate, 0)
        self.assertGreater(self.drill.thermal_efficiency, 0)
        self.assertLess(self.drill.thermal_efficiency, 100)
        self.assertGreater(self.drill.core_temperature, 0)
        self.assertEqual(500, self.drill.optimum_core_temperature)

        self.drill.active = False
        self.wait(3)

        self.assertEqual(self.state.deployed, self.drill.state)
        self.assertTrue(self.drill.deployed)
        self.assertFalse(self.drill.active)
        self.check_inactive_properties()

        self.drill.deployed = False
        self.wait()

        self.assertEqual(self.state.retracting, self.drill.state)
        self.assertFalse(self.drill.deployed)
        self.assertFalse(self.drill.active)
        self.check_inactive_properties()

        while self.drill.state == self.state.retracting:
            self.wait()

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


if __name__ == '__main__':
    unittest.main()
