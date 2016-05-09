import unittest
import krpctest
import krpc
import time

class TestPartsDecoupler(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        krpctest.new_save()
        krpctest.launch_vessel_from_vab('PartsDecoupler')
        krpctest.remove_other_vessels()
        cls.conn = krpctest.connect(name='TestPartsDecoupler')
        cls.sc = cls.conn.space_center
        cls.vessel = cls.sc.active_vessel
        cls.parts = cls.vessel.parts
        cls.state = cls.sc.SolarPanelState

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_stack_decoupler(self):
        decoupler = next(iter(filter(lambda e: e.part.title == 'TR-18A Stack Decoupler', self.parts.decouplers)))
        self.assertEqual(decoupler.impulse, 2500)
        self.assertEqual(decoupler.decoupled, False)
        self.assertEqual(decoupler.part.vessel, self.vessel)
        new_vessel = decoupler.decouple()
        self.assertEqual(decoupler.decoupled, True)
        self.assertNotEqual(decoupler.part.vessel, self.vessel)
        self.assertNotEqual(new_vessel, self.vessel)
        self.assertEqual(
            ['fuelTank', 'stackDecoupler'],
            sorted(part.name for part in new_vessel.parts.all))

    def test_radial_decoupler(self):
        decoupler = next(iter(filter(lambda e: e.part.title == 'TT-70 Radial Decoupler', self.parts.decouplers)))
        self.assertEqual(decoupler.impulse, 2600)
        self.assertEqual(decoupler.decoupled, False)
        self.assertEqual(decoupler.part.vessel, self.vessel)
        new_vessel = decoupler.decouple()
        self.assertEqual(decoupler.decoupled, True)
        self.assertNotEqual(decoupler.part.vessel, self.vessel)
        self.assertNotEqual(new_vessel, self.vessel)
        self.assertEqual(
            ['fuelTank', 'fuelTank', 'radialDecoupler2'],
            sorted(part.name for part in new_vessel.parts.all))

if __name__ == "__main__":
    unittest.main()
