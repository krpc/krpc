import unittest
import krpctest


class TestPartsSolarPanel(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab('PartsSolarPanel')
        cls.remove_other_vessels()
        vessel = cls.connect().space_center.active_vessel
        parts = vessel.parts
        cls.control = vessel.control
        cls.state = cls.connect().space_center.SolarPanelState
        cls.panels = parts.solar_panels
        cls.deployable_panel = parts.with_title(
            'Gigantor XL Solar Array')[0].solar_panel
        cls.fixed_panel = parts.with_title(
            'OX-STAT Photovoltaic Panels')[0].solar_panel

    def test_fixed_panel(self):
        self.assertFalse(self.fixed_panel.deployable)
        self.assertTrue(self.fixed_panel.deployed)
        self.assertEqual(self.state.extended, self.fixed_panel.state)
        self.assertGreater(self.fixed_panel.energy_flow, 0)
        self.assertGreater(self.fixed_panel.sun_exposure, 0)

    def test_extendable_panel(self):
        panel = self.deployable_panel
        self.assertTrue(panel.deployable)
        self.assertFalse(panel.deployed)
        self.assertEqual(self.state.retracted, panel.state)
        self.assertEqual(0, panel.energy_flow)
        self.assertEqual(0, panel.sun_exposure)
        self.assertFalse(self.control.solar_panels)

        panel.deployed = True
        self.wait()

        self.assertTrue(panel.deployed)
        self.assertEqual(self.state.extending, panel.state)
        self.assertEqual(0, panel.energy_flow)
        self.assertEqual(0, panel.sun_exposure)
        self.assertFalse(self.control.solar_panels)

        while panel.state == self.state.extending:
            self.wait()

        self.assertTrue(panel.deployed)
        self.assertEqual(self.state.extended, panel.state)
        self.wait()
        self.assertGreater(panel.energy_flow, 0)
        self.assertGreater(panel.sun_exposure, 0)

        panel.deployed = False
        self.wait()

        self.assertFalse(panel.deployed)
        self.assertEqual(self.state.retracting, panel.state)
        self.assertEqual(0, panel.energy_flow)
        self.assertEqual(0, panel.sun_exposure)

        while panel.state == self.state.retracting:
            self.wait()

        self.assertFalse(panel.deployed)
        self.assertEqual(self.state.retracted, panel.state)
        self.assertEqual(0, panel.energy_flow)
        self.assertEqual(0, panel.sun_exposure)
        self.assertFalse(self.control.solar_panels)

    def test_control_panels(self):
        self.assertFalse(self.control.solar_panels)
        self.control.solar_panels = True
        for panel in self.panels:
            if panel.deployable:
                while panel.state == self.state.extending:
                    self.wait()
            self.assertTrue(panel.deployed)
        self.assertTrue(self.control.solar_panels)
        self.control.solar_panels = False
        for panel in self.panels:
            if panel.deployable:
                while panel.state != self.state.retracted:
                    self.wait()
                self.assertFalse(panel.deployed)
        self.assertFalse(self.control.solar_panels)


class TestPartsSolarPanelBreak(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab('PartsSolarPanel')
        cls.remove_other_vessels()
        vessel = cls.connect().space_center.active_vessel
        parts = vessel.parts
        cls.control = vessel.control
        cls.state = cls.connect().space_center.SolarPanelState
        cls.panel = parts.with_title(
            'SP-L 1x6 Photovoltaic Panels')[0].solar_panel

    def test_break_panel(self):
        self.assertEqual(self.state.retracted, self.panel.state)
        self.panel.deployed = True
        while self.panel.state == self.state.extending:
            self.wait()
        self.control.activate_next_stage()
        self.wait(1)
        self.assertEqual(self.state.broken, self.panel.state)


if __name__ == '__main__':
    unittest.main()
