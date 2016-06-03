import unittest
import krpctest

class TestPartsResourceConverter(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        if krpctest.connect().space_center.active_vessel.name != 'PartsHarvester':
            krpctest.new_save()
            krpctest.launch_vessel_from_vab('PartsHarvester')
            krpctest.remove_other_vessels()
        cls.conn = krpctest.connect(cls)
        cls.ConverterState = cls.conn.space_center.ResourceConverterState
        cls.HarvesterState = cls.conn.space_center.ResourceHarvesterState
        parts = cls.conn.space_center.active_vessel.parts
        cls.drill = parts.with_title('\'Drill-O-Matic\' Mining Excavator')[0].resource_harvester
        cls.converter = parts.with_title('Convert-O-Tron 250')[0].resource_converter
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

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_properties(self):
        self.assertEqual(len(self.infos), self.converter.count)
        for i, info in enumerate(self.infos):
            self.assertFalse(self.converter.active(i))
            self.assertEqual(info['name'], self.converter.name(i))
            self.assertEqual(self.ConverterState.idle, self.converter.state(i))
            self.assertEqual('Inactive', self.converter.status_info(i))
            self.assertEqual(info['inputs'], self.converter.inputs(i))
            self.assertEqual(info['outputs'], self.converter.outputs(i))

    def test_operate(self):
        self.drill.deployed = True
        while not self.drill.deployed:
            pass
        self.drill.active = True
        while not self.drill.active:
            pass
        index = 1
        self.assertFalse(self.converter.active(index))
        self.assertEqual(self.ConverterState.idle, self.converter.state(index))
        self.assertEqual('Inactive', self.converter.status_info(index))
        self.converter.start(index)
        while self.converter.state(index) != self.ConverterState.running:
            pass
        self.assertTrue(self.converter.active(index))
        self.assertEqual(self.ConverterState.running, self.converter.state(index))
        self.converter.stop(index)
        while self.converter.state(index) != self.ConverterState.idle:
            pass
        self.assertFalse(self.converter.active(index))
        self.assertEqual(self.ConverterState.idle, self.converter.state(index))
        self.assertEqual('Inactive', self.converter.status_info(index))
        self.drill.deployed = False
        while self.drill.state != self.HarvesterState.retracted:
            pass

if __name__ == '__main__':
    unittest.main()
