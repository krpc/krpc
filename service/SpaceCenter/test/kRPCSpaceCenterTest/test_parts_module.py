import unittest
import testingtools
import krpc
import time

class TestPartsModule(testingtools.TestCase):

    @classmethod
    def setUpClass(cls):
        if testingtools.connect().space_center.active_vessel.name != 'Parts':
            testingtools.new_save()
            testingtools.launch_vessel_from_vab('Parts')
            testingtools.remove_other_vessels()
        cls.conn = testingtools.connect(name='TestParts')
        cls.vessel = cls.conn.space_center.active_vessel
        cls.parts = cls.vessel.parts

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_command_module(self):
        part = self.parts.with_title('Mk1-2 Command Pod')[0]
        module = next(iter(filter(lambda m: m.name == 'ModuleCommand', part.modules)))
        self.assertEqual('ModuleCommand', module.name)
        self.assertEqual(part, module.part)
        self.assertEqual({'State': 'Operational'}, module.fields)
        self.assertTrue(module.has_field('State'))
        self.assertFalse(module.has_field('DoesntExist'))
        self.assertEqual('Operational', module.get_field('State'))
        self.assertRaises(krpc.client.RPCError, module.get_field, 'DoesntExist')
        self.assertEqual(['Control From Here', 'Rename Vessel'], sorted(module.events))
        self.assertTrue(module.has_event('Control From Here'))
        self.assertFalse(module.has_event('DoesntExist'))
        module.trigger_event('Control From Here')
        self.assertRaises(krpc.client.RPCError, module.trigger_event, 'DoesntExist')
        self.assertEqual(0, len(module.actions))
        self.assertFalse(module.has_action('DoesntExist'))
        self.assertRaises(krpc.client.RPCError, module.set_action, 'DoesntExist', True)
        self.assertRaises(krpc.client.RPCError, module.set_action, 'DoesntExist', False)

    def test_solar_panel(self):
        part = self.parts.with_title('SP-L 1x6 Photovoltaic Panels')[0]
        module = next(iter(filter(lambda m: m.name == 'ModuleDeployableSolarPanel', part.modules)))
        self.assertEqual('ModuleDeployableSolarPanel', module.name)
        self.assertEqual(part, module.part)
        self.assertEqual({'Energy Flow': '0', 'Status': 'Retracted', 'Sun Exposure': '0'}, module.fields)
        self.assertTrue(module.has_field('Status'))
        self.assertFalse(module.has_field('DoesntExist'))
        self.assertEqual('Retracted', module.get_field('Status'))
        self.assertRaises(krpc.client.RPCError, module.get_field, 'DoesntExist')
        self.assertEqual(['Extend Panels'], sorted(module.events))
        self.assertTrue(module.has_event('Extend Panels'))
        self.assertFalse(module.has_event('DoesntExist'))
        self.assertRaises(krpc.client.RPCError, module.trigger_event, 'DoesntExist')
        self.assertEqual(['Extend Panel', 'Retract Panel', 'Toggle Panels'], sorted(module.actions))
        self.assertFalse(module.has_action('DoesntExist'))
        self.assertRaises(krpc.client.RPCError, module.set_action, 'DoesntExist', True)
        self.assertRaises(krpc.client.RPCError, module.set_action, 'DoesntExist', False)

    def test_events(self):
        part = self.parts.with_title('Illuminator Mk1')[0]
        module = next(iter(filter(lambda m: m.name == 'ModuleLight', part.modules)))
        self.assertTrue(module.has_event('Lights On'))
        self.assertFalse(module.has_event('Lights Off'))
        module.trigger_event('Lights On')
        time.sleep(0.25)
        self.assertFalse(module.has_event('Lights On'))
        self.assertTrue(module.has_event('Lights Off'))
        module.trigger_event('Lights Off')
        time.sleep(0.25)
        self.assertTrue(module.has_event('Lights On'))
        self.assertFalse(module.has_event('Lights Off'))

    def test_actions(self):
        part = self.parts.with_title('Illuminator Mk1')[0]
        module = next(iter(filter(lambda m: m.name == 'ModuleLight', part.modules)))
        self.assertTrue(module.has_event('Lights On'))
        self.assertFalse(module.has_event('Lights Off'))
        module.set_action('ToggleLight', True)
        time.sleep(0.25)
        self.assertFalse(module.has_event('Lights On'))
        self.assertTrue(module.has_event('Lights Off'))
        module.set_action('ToggleLight', False)
        time.sleep(0.25)
        self.assertTrue(module.has_event('Lights On'))
        self.assertFalse(module.has_event('Lights Off'))

if __name__ == "__main__":
    unittest.main()
