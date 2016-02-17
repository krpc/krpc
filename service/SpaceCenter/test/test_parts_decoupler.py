import unittest
import testingtools
import krpc
import time

class TestPartsDecoupler(testingtools.TestCase):

    @classmethod
    def setUpClass(cls):
        testingtools.new_save()
        testingtools.launch_vessel_from_vab('PartsDecoupler')
        testingtools.remove_other_vessels()
        cls.conn = testingtools.connect(name='TestPartsDecoupler')
        cls.vessel = cls.conn.space_center.active_vessel
        cls.parts = cls.vessel.parts
        cls.state = cls.conn.space_center.SolarPanelState

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_stack_decoupler(self):
        decoupler = next(iter(filter(lambda e: e.part.title == 'TR-18A Stack Decoupler', self.parts.decouplers)))
        self.assertEqual(decoupler.impulse, 2500)
        self.assertEqual(decoupler.decoupled, False)
        self.assertEqual(decoupler.part.vessel, self.vessel)
        decoupler.decouple()
        time.sleep(0.5)
        self.assertEqual(decoupler.decoupled, True)
        self.assertNotEqual(decoupler.part.vessel, self.vessel)

    def test_radial_decoupler(self):
        decoupler = next(iter(filter(lambda e: e.part.title == 'TT-70 Radial Decoupler', self.parts.decouplers)))
        self.assertEqual(decoupler.impulse, 2600)
        self.assertEqual(decoupler.decoupled, False)
        self.assertEqual(decoupler.part.vessel, self.vessel)
        decoupler.decouple()
        time.sleep(0.5)
        self.assertEqual(decoupler.decoupled, True)
        self.assertNotEqual(decoupler.part.vessel, self.vessel)

if __name__ == "__main__":
    unittest.main()
