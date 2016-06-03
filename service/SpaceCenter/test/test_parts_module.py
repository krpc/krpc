import unittest
import time
import krpctest
import krpc

class TestPartsModule(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        if krpctest.connect().space_center.active_vessel.name != 'Parts':
            krpctest.new_save()
            krpctest.launch_vessel_from_vab('Parts')
            krpctest.remove_other_vessels()
        cls.conn = krpctest.connect(cls)
        cls.vessel = cls.conn.space_center.active_vessel
        cls.parts = cls.vessel.parts

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_command_module(self):
        part = self.parts.with_title('Mk1-2 Command Pod')[0]
        module = next(m for m in part.modules if m.name == 'ModuleCommand')
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
        self.assertEqual([], module.actions)
        self.assertFalse(module.has_action('DoesntExist'))
        self.assertRaises(krpc.client.RPCError, module.set_action, 'DoesntExist', True)
        self.assertRaises(krpc.client.RPCError, module.set_action, 'DoesntExist', False)

    def test_solar_panel(self):
        part = self.parts.with_title('SP-L 1x6 Photovoltaic Panels')[0]
        module = next(m for m in part.modules if m.name == 'ModuleDeployableSolarPanel')
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

    def test_set_field_int(self):
        part = self.parts.with_title('LY-10 Small Landing Gear')[0]
        module = next(m for m in part.modules if m.name == 'ModuleWheelBrakes')
        self.assertEqual({'Brakes': '100'}, module.fields)
        module.set_field_int('Brakes', 50)
        time.sleep(1)
        self.assertEqual({'Brakes': '50'}, module.fields)
        module.set_field_int('Brakes', 100)
        self.assertEqual({'Brakes': '100'}, module.fields)

    def test_events(self):
        part = self.parts.with_title('Illuminator Mk1')[0]
        module = next(m for m in part.modules if m.name == 'ModuleLight')
        self.assertTrue(module.has_event('Lights On'))
        self.assertFalse(module.has_event('Lights Off'))
        module.trigger_event('Lights On')
        time.sleep(0.1)
        self.assertFalse(module.has_event('Lights On'))
        self.assertTrue(module.has_event('Lights Off'))
        module.trigger_event('Lights Off')
        time.sleep(0.1)
        self.assertTrue(module.has_event('Lights On'))
        self.assertFalse(module.has_event('Lights Off'))

    def test_actions(self):
        part = self.parts.with_title('Illuminator Mk1')[0]
        module = next(m for m in part.modules if m.name == 'ModuleLight')
        self.assertTrue(module.has_event('Lights On'))
        self.assertFalse(module.has_event('Lights Off'))
        module.set_action('ToggleLight', True)
        time.sleep(0.1)
        self.assertFalse(module.has_event('Lights On'))
        self.assertTrue(module.has_event('Lights Off'))
        module.set_action('ToggleLight', False)
        time.sleep(0.1)
        self.assertTrue(module.has_event('Lights On'))
        self.assertFalse(module.has_event('Lights Off'))

if __name__ == '__main__':
    unittest.main()
