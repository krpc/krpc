import unittest
import krpctest

class TestPartsDecoupler(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        krpctest.new_save()
        krpctest.launch_vessel_from_vab('PartsDecoupler')
        krpctest.remove_other_vessels()
        cls.conn = krpctest.connect(cls)
        cls.vessel = cls.conn.space_center.active_vessel
        cls.stack_decoupler = cls.vessel.parts.with_title('TR-18A Stack Decoupler')[0].decoupler
        cls.radial_decoupler = cls.vessel.parts.with_title('TT-70 Radial Decoupler')[0].decoupler

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_stack_decoupler(self):
        self.assertEqual(2500, self.stack_decoupler.impulse)
        self.assertFalse(self.stack_decoupler.decoupled)
        self.assertEqual(self.vessel, self.stack_decoupler.part.vessel)
        new_vessel = self.stack_decoupler.decouple()
        self.assertTrue(self.stack_decoupler.decoupled)
        self.assertNotEqual(self.vessel, self.stack_decoupler.part.vessel)
        self.assertNotEqual(self.vessel, new_vessel)
        self.assertEqual(
            ['fuelTank', 'stackSelf.Stack_Decoupler'],
            sorted(part.name for part in new_vessel.parts.all))

    def test_radial_decoupler(self):
        self.assertEqual(2600, self.radial_decoupler.impulse)
        self.assertFalse(self.radial_decoupler.decoupled)
        self.assertEqual(self.vessel, self.radial_decoupler.part.vessel)
        new_vessel = self.radial_decoupler.decouple()
        self.assertTrue(self.radial_decoupler.decoupled)
        self.assertNotEqual(self.vessel, self.radial_decoupler.part.vessel)
        self.assertNotEqual(self.vessel, new_vessel)
        self.assertEqual(
            ['fuelTank', 'fuelTank', 'radialSelf.Radial_Decoupler2'],
            sorted(part.name for part in new_vessel.parts.all))

if __name__ == '__main__':
    unittest.main()
