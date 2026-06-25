import unittest
import krpctest


class TestPartsResourceHarvester(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        # Always reload the save and relaunch so the drills are tested on a
        # pristine vessel rather than whatever state a previous test left behind.
        # Launch from the VAB and teleport the craft to the Greater Flats on Minmus,
        # a flat expanse with good ore concentration where the drills can actually
        # extract (the KSC launchpad has negligible ore).
        cls.new_save(always_load=True)
        space_center = cls.connect().space_center
        cls.launch_vessel_from_vab("PartsHarvester")
        cls.set_landed("Minmus", -4.794139, -11.575088)
        cls.remove_other_vessels()
        cls.state = space_center.ResourceHarvesterState
        cls.vessel = space_center.active_vessel
        parts = cls.vessel.parts
        cls.control = cls.vessel.control
        cls.drills = parts.resource_harvesters
        cls.drill = parts.with_name("RadialDrill")[0].resource_harvester

    def check_inactive_properties(self):
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

        ore_before = self.vessel.resources.amount("Ore")
        self.drill.active = True
        self.wait(3)

        self.assertEqual(self.state.active, self.drill.state)
        self.assertTrue(self.drill.deployed)
        self.assertTrue(self.drill.active)
        # extraction_rate reads a PAW-gated KSP field ("n/a" headless), so verify
        # the drill is mining by checking the Ore amount increases instead.
        self.assertGreater(self.vessel.resources.amount("Ore"), ore_before)
        self.assertGreater(self.drill.thermal_efficiency, 0)
        self.assertLess(self.drill.thermal_efficiency, 1)
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

    def test_control(self):
        for drill in self.drills:
            self.assertEqual(self.state.retracted, drill.state)
            self.assertFalse(drill.deployed)
            self.assertFalse(drill.active)

        self.control.resource_harvesters = True
        self.wait()

        self.assertFalse(self.control.resource_harvesters)
        for drill in self.drills:
            self.assertEqual(self.state.deploying, drill.state)
            self.assertFalse(drill.deployed)
            self.assertFalse(drill.active)

        for drill in self.drills:
            while drill.state == self.state.deploying:
                self.wait()

        self.assertTrue(self.control.resource_harvesters)
        for drill in self.drills:
            self.assertEqual(self.state.deployed, drill.state)
            self.assertTrue(drill.deployed)
            self.assertFalse(drill.active)

        ore_before = self.vessel.resources.amount("Ore")
        self.control.resource_harvesters_active = True
        self.wait(3)

        self.assertTrue(self.control.resource_harvesters_active)
        # extraction_rate reads a PAW-gated KSP field ("n/a" headless), so verify
        # the drills are mining by checking the Ore amount increases instead.
        self.assertGreater(self.vessel.resources.amount("Ore"), ore_before)
        for drill in self.drills:
            self.assertEqual(self.state.active, drill.state)
            self.assertTrue(drill.deployed)
            self.assertTrue(drill.active)
            self.assertGreater(drill.thermal_efficiency, 0)
            self.assertLess(drill.thermal_efficiency, 1)
            self.assertGreater(drill.core_temperature, 0)
            self.assertEqual(500, drill.optimum_core_temperature)

        self.control.resource_harvesters_active = False
        self.wait(3)

        self.assertFalse(self.control.resource_harvesters_active)
        for drill in self.drills:
            self.assertEqual(self.state.deployed, drill.state)
            self.assertTrue(drill.deployed)
            self.assertFalse(drill.active)

        self.control.resource_harvesters = False
        self.wait()

        self.assertFalse(self.control.resource_harvesters)
        for drill in self.drills:
            self.assertEqual(self.state.retracting, drill.state)
            self.assertFalse(drill.deployed)
            self.assertFalse(drill.active)

        for drill in self.drills:
            while drill.state == self.state.retracting:
                self.wait()

        self.assertFalse(self.control.resource_harvesters)
        for drill in self.drills:
            self.assertEqual(self.state.retracted, drill.state)
            self.assertFalse(drill.deployed)
            self.assertFalse(drill.active)


if __name__ == "__main__":
    unittest.main()
