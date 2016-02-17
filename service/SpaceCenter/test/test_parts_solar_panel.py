import unittest
import testingtools
import krpc
import time

class TestPartsSolarPanel(testingtools.TestCase):

    @classmethod
    def setUpClass(cls):
        testingtools.new_save()
        testingtools.launch_vessel_from_vab('PartsSolarPanel')
        testingtools.remove_other_vessels()
        cls.conn = testingtools.connect(name='TestPartsSolarPanel')
        cls.vessel = cls.conn.space_center.active_vessel
        cls.parts = cls.vessel.parts
        cls.state = cls.conn.space_center.SolarPanelState

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_fixed_panel(self):
        panel = next(iter(filter(lambda e: e.part.title == 'OX-STAT Photovoltaic Panels', self.parts.solar_panels)))
        self.assertTrue(panel.deployed)
        self.assertEqual(panel.state, self.state.extended)
        self.assertGreater(panel.energy_flow, 0)
        self.assertGreater(panel.sun_exposure, 0)

    def test_extendable_panel(self):
        panel = next(iter(filter(lambda e: e.part.title == 'Gigantor XL Solar Array', self.parts.solar_panels)))
        self.assertFalse(panel.deployed)
        self.assertEqual(panel.state, self.state.retracted)
        self.assertEqual(panel.energy_flow, 0)
        self.assertEqual(panel.sun_exposure, 0)

        panel.deployed = True
        time.sleep(0.1)

        self.assertTrue(panel.deployed)
        self.assertEqual(panel.state, self.state.extending)
        self.assertEqual(panel.energy_flow, 0)
        self.assertEqual(panel.sun_exposure, 0)

        while panel.state == self.state.extending:
            pass
        time.sleep(0.1)

        self.assertTrue(panel.deployed)
        self.assertEqual(panel.state, self.state.extended)
        self.assertGreater(panel.energy_flow, 0)
        self.assertGreater(panel.sun_exposure, 0)

        panel.deployed = False
        time.sleep(0.1)

        self.assertFalse(panel.deployed)
        self.assertEqual(panel.state, self.state.retracting)
        self.assertEqual(panel.energy_flow, 0)
        self.assertEqual(panel.sun_exposure, 0)

        while panel.state == self.state.retracting:
            pass
        time.sleep(0.1)

        self.assertFalse(panel.deployed)
        self.assertEqual(panel.state, self.state.retracted)
        self.assertEqual(panel.energy_flow, 0)
        self.assertEqual(panel.sun_exposure, 0)

    def test_break_panel(self):
        panel = next(iter(filter(lambda e: e.part.title == 'OX-4L 1x6 Photovoltaic Panels', self.parts.solar_panels)))

        self.assertEqual(panel.state, self.state.retracted)
        panel.deployed = True
        while panel.state == self.state.extending:
            pass
        time.sleep(0.1)

        self.vessel.control.activate_next_stage()
        time.sleep(1)

        self.assertEqual(panel.state, self.state.broken)

if __name__ == "__main__":
    unittest.main()
