import unittest
import krpctest


class TestPartsResourceConverter(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        if cls.connect().space_center.active_vessel.name != 'PartsHarvester':
            cls.launch_vessel_from_vab('PartsHarvester')
            cls.remove_other_vessels()
        space_center = cls.connect().space_center
        cls.converter_state = space_center.ResourceConverterState
        cls.harvester_state = space_center.ResourceHarvesterState
        parts = space_center.active_vessel.parts
        cls.drill = parts.with_title(
            '\'Drill-O-Matic\' Mining Excavator')[0].resource_harvester
        cls.converter = parts.with_title(
            'Convert-O-Tron 250')[0].resource_converter
        cls.infos = [
            {
                'name': 'Lf+Ox',
                'inputs': ['Ore', 'ElectricCharge'],
                'outputs': ['LiquidFuel', 'Oxidizer']
            },
            {
                'name': 'Monoprop',
                'inputs': ['Ore', 'ElectricCharge'],
                'outputs': ['MonoPropellant']
            },
            {
                'name': 'LiquidFuel',
                'inputs': ['Ore', 'ElectricCharge'],
                'outputs': ['LiquidFuel']
            },
            {
                'name': 'Oxidizer',
                'inputs': ['Ore', 'ElectricCharge'],
                'outputs': ['Oxidizer']
            }
        ]

    def test_properties(self):
        self.assertEqual(len(self.infos), self.converter.count)
        for i, info in enumerate(self.infos):
            self.assertFalse(self.converter.active(i))
            self.assertEqual(info['name'], self.converter.name(i))
            self.assertEqual(
                self.converter_state.idle, self.converter.state(i))
            self.assertEqual('Inactive', self.converter.status_info(i))
            self.assertEqual(info['inputs'], self.converter.inputs(i))
            self.assertEqual(info['outputs'], self.converter.outputs(i))

    def test_operate(self):
        self.drill.deployed = True
        while not self.drill.deployed:
            self.wait()
        self.drill.active = True
        while not self.drill.active:
            self.wait()
        index = 1
        self.assertFalse(self.converter.active(index))
        self.assertEqual(
            self.converter_state.idle, self.converter.state(index))
        self.assertEqual('Inactive', self.converter.status_info(index))
        self.converter.start(index)
        while self.converter.state(index) != self.converter_state.running:
            self.wait()
        self.assertTrue(self.converter.active(index))
        self.assertEqual(
            self.converter_state.running, self.converter.state(index))
        self.converter.stop(index)
        while self.converter.state(index) != self.converter_state.idle:
            self.wait()
        self.assertFalse(self.converter.active(index))
        self.assertEqual(
            self.converter_state.idle, self.converter.state(index))
        self.assertEqual('Inactive', self.converter.status_info(index))
        self.drill.deployed = False
        while self.drill.state != self.harvester_state.retracted:
            self.wait()


if __name__ == '__main__':
    unittest.main()
