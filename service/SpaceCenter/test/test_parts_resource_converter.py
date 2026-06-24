import unittest
import krpctest


class TestPartsResourceConverter(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        # Always reload the save and relaunch so the converter is tested on a
        # pristine vessel (cold core, full Ore tank) rather than whatever state a
        # previous test left behind.
        cls.new_save(always_load=True)
        cls.launch_vessel_from_vab("PartsHarvester")
        cls.remove_other_vessels()
        space_center = cls.connect().space_center
        cls.converter_state = space_center.ResourceConverterState
        parts = space_center.active_vessel.parts
        cls.converter = parts.with_name("ISRU")[0].resource_converter
        cls.infos = [
            {
                "name": "Lf+Ox",
                "inputs": ["Ore", "ElectricCharge"],
                "outputs": ["LiquidFuel", "Oxidizer"],
            },
            {
                "name": "Monoprop",
                "inputs": ["Ore", "ElectricCharge"],
                "outputs": ["MonoPropellant"],
            },
            {
                "name": "LiquidFuel",
                "inputs": ["Ore", "ElectricCharge"],
                "outputs": ["LiquidFuel"],
            },
            {
                "name": "Oxidizer",
                "inputs": ["Ore", "ElectricCharge"],
                "outputs": ["Oxidizer"],
            },
        ]

    def test_properties(self):
        self.assertGreater(self.converter.thermal_efficiency, 0)
        self.assertLess(self.converter.thermal_efficiency, 1)
        self.assertGreater(self.converter.core_temperature, 0)
        self.assertEqual(1000, self.converter.optimum_core_temperature)
        self.assertEqual(len(self.infos), self.converter.count)
        for i, info in enumerate(self.infos):
            self.assertFalse(self.converter.active(i))
            self.assertEqual(info["name"], self.converter.name(i))
            self.assertEqual(self.converter_state.idle, self.converter.state(i))
            self.assertEqual("Inactive", self.converter.status_info(i))
            self.assertEqual(info["inputs"], self.converter.inputs(i))
            self.assertEqual(info["outputs"], self.converter.outputs(i))

    def test_operate(self):
        # Relies on the Ore preloaded in the craft's tank; the drill is exercised
        # by test_parts_resource_harvester.py, not here.
        index = 1
        self.assertFalse(self.converter.active(index))
        self.assertEqual(self.converter_state.idle, self.converter.state(index))
        self.assertEqual("Inactive", self.converter.status_info(index))
        self.converter.start(index)
        self.wait_until(
            lambda: self.converter.state(index) == self.converter_state.running,
            message="converter to start running",
        )
        self.assertTrue(self.converter.active(index))
        self.assertEqual(self.converter_state.running, self.converter.state(index))
        self.assertGreater(self.converter.core_temperature, 0)
        self.assertEqual(1000, self.converter.optimum_core_temperature)
        self.converter.stop(index)
        self.wait_until(
            lambda: self.converter.state(index) == self.converter_state.idle,
            message="converter to become idle",
        )
        self.assertFalse(self.converter.active(index))
        self.assertEqual(self.converter_state.idle, self.converter.state(index))
        self.assertEqual("Inactive", self.converter.status_info(index))


if __name__ == "__main__":
    unittest.main()
