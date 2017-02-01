import unittest
import krpctest


class TestPartsDecoupler(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab('PartsDecoupler')
        cls.remove_other_vessels()
        cls.vessel = cls.connect().space_center.active_vessel
        cls.stack_decoupler = cls.vessel.parts.with_title(
            'TR-18A Stack Decoupler')[0].decoupler
        cls.radial_decoupler = cls.vessel.parts.with_title(
            'TT-70 Radial Decoupler')[0].decoupler
        cls.disabled_decoupler = cls.vessel.parts.with_title(
            'TR-18D Stack Separator')[0].decoupler

    def test_stack_decoupler(self):
        self.assertEqual(2500, self.stack_decoupler.impulse)
        self.assertFalse(self.stack_decoupler.decoupled)
        self.assertTrue(self.stack_decoupler.staged)
        self.assertEqual(self.vessel, self.stack_decoupler.part.vessel)
        new_vessel = self.stack_decoupler.decouple()
        self.assertTrue(self.stack_decoupler.decoupled)
        self.assertNotEqual(self.vessel, self.stack_decoupler.part.vessel)
        self.assertNotEqual(self.vessel, new_vessel)
        self.assertItemsEqual(
            ['FL-T400 Fuel Tank', 'TR-18A Stack Decoupler'],
            [part.title for part in new_vessel.parts.all])

    def test_radial_decoupler(self):
        self.assertEqual(2600, self.radial_decoupler.impulse)
        self.assertFalse(self.radial_decoupler.decoupled)
        self.assertTrue(self.radial_decoupler.staged)
        self.assertEqual(self.vessel, self.radial_decoupler.part.vessel)
        new_vessel = self.radial_decoupler.decouple()
        self.assertTrue(self.radial_decoupler.decoupled)
        self.assertNotEqual(self.vessel, self.radial_decoupler.part.vessel)
        self.assertNotEqual(self.vessel, new_vessel)
        self.assertItemsEqual(
            ['FL-T400 Fuel Tank', 'FL-T400 Fuel Tank',
             'TT-70 Radial Decoupler'],
            [part.title for part in new_vessel.parts.all])

    def test_disabled_decoupler(self):
        self.assertFalse(self.disabled_decoupler.staged)


if __name__ == '__main__':
    unittest.main()
