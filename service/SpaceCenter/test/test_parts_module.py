import unittest
import krpctest
import krpc


class TestPartsModule(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        if cls.connect().space_center.active_vessel.name != 'Parts':
            cls.launch_vessel_from_vab('Parts')
            cls.remove_other_vessels()
        cls.vessel = cls.connect().space_center.active_vessel
        cls.parts = cls.vessel.parts

    def test_command_module(self):
        part = self.parts.with_title('Mk1-2 Command Pod')[0]
        module = next(m for m in part.modules if m.name == 'ModuleCommand')
        self.assertEqual('ModuleCommand', module.name)
        self.assertEqual(part, module.part)
        self.assertEqual({
            'Command State': 'Operational',
            'Comm First Hop Dist': 'NA',
            'Comm Signal': 'NA'
        }, module.fields)
        self.assertTrue(module.has_field('Command State'))
        self.assertFalse(module.has_field('DoesntExist'))
        self.assertEqual('Operational', module.get_field('Command State'))
        self.assertRaises(
            krpc.client.RPCError, module.get_field, 'DoesntExist')
        self.assertItemsEqual(['Control From Here', 'Rename Vessel'],
                              module.events)
        self.assertTrue(module.has_event('Control From Here'))
        self.assertFalse(module.has_event('DoesntExist'))
        module.trigger_event('Control From Here')
        self.assertRaises(krpc.client.RPCError, module.trigger_event,
                          'DoesntExist')
        self.assertEqual(['Toggle Hibernation'], module.actions)
        self.assertFalse(module.has_action('DoesntExist'))
        self.assertRaises(krpc.client.RPCError, module.set_action,
                          'DoesntExist', True)
        self.assertRaises(krpc.client.RPCError, module.set_action,
                          'DoesntExist', False)

    def test_solar_panel(self):
        part = self.parts.with_title('SP-L 1x6 Photovoltaic Panels')[0]
        module = next(m for m in part.modules
                      if m.name == 'ModuleDeployableSolarPanel')
        self.assertEqual('ModuleDeployableSolarPanel', module.name)
        self.assertEqual(part, module.part)
        self.assertEqual({'Energy Flow': '0',
                          'Status': 'Retracted',
                          'Sun Exposure': '0'}, module.fields)
        self.assertTrue(module.has_field('Status'))
        self.assertFalse(module.has_field('DoesntExist'))
        self.assertEqual('Retracted', module.get_field('Status'))
        self.assertRaises(
            krpc.client.RPCError, module.get_field, 'DoesntExist')
        self.assertItemsEqual(['Extend Solar Panel'], module.events)
        self.assertTrue(module.has_event('Extend Solar Panel'))
        self.assertFalse(module.has_event('DoesntExist'))
        self.assertRaises(krpc.client.RPCError, module.trigger_event,
                          'DoesntExist')
        self.assertItemsEqual(['Extend Solar Panel', 'Retract Solar Panel',
                               'Toggle Solar Panel'], module.actions)
        self.assertFalse(module.has_action('DoesntExist'))
        self.assertRaises(krpc.client.RPCError, module.set_action,
                          'DoesntExist', True)
        self.assertRaises(krpc.client.RPCError, module.set_action,
                          'DoesntExist', False)

    def test_set_field_int(self):
        part = self.parts.with_title('LY-10 Small Landing Gear')[0]
        module = next(m for m in part.modules if m.name == 'ModuleWheelBrakes')
        self.assertEqual({'Brakes': '100'}, module.fields)
        module.set_field_int('Brakes', 50)
        self.wait(1)
        self.assertEqual({'Brakes': '50'}, module.fields)
        module.set_field_int('Brakes', 100)
        self.assertEqual({'Brakes': '100'}, module.fields)

    def test_events(self):
        part = self.parts.with_title('Illuminator Mk1')[0]
        module = next(m for m in part.modules if m.name == 'ModuleLight')
        self.assertTrue(module.has_event('Lights On'))
        self.assertFalse(module.has_event('Lights Off'))
        module.trigger_event('Lights On')
        self.wait()
        self.assertFalse(module.has_event('Lights On'))
        self.assertTrue(module.has_event('Lights Off'))
        module.trigger_event('Lights Off')
        self.wait()
        self.assertTrue(module.has_event('Lights On'))
        self.assertFalse(module.has_event('Lights Off'))

    def test_actions(self):
        part = self.parts.with_title('Illuminator Mk1')[0]
        module = next(m for m in part.modules if m.name == 'ModuleLight')
        self.assertTrue(module.has_event('Lights On'))
        self.assertFalse(module.has_event('Lights Off'))
        module.set_action('ToggleLight', True)
        self.wait()
        self.assertFalse(module.has_event('Lights On'))
        self.assertTrue(module.has_event('Lights Off'))
        module.set_action('ToggleLight', False)
        self.wait()
        self.assertTrue(module.has_event('Lights On'))
        self.assertFalse(module.has_event('Lights Off'))


if __name__ == '__main__':
    unittest.main()
