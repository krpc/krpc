import unittest

from krpctools.game.ksp import TestCase


class TestPartsSolarPanelNearFutureSolar(TestCase):
    """The SpaceCenter SolarPanel class on Near Future Solar curved panels.

    The test craft carries four deployable and four fixed curved panels, each set in
    4x symmetry around the tank, so some of each set face the sun.
    """

    mods = ["NearFutureSolar"]

    @classmethod
    def setUpClass(cls):
        vessel = cls.launch("PartsSolarPanelNearFutureSolar")
        parts = vessel.parts
        cls.control = vessel.control
        cls.state = cls.connect().space_center.DeployableState
        cls.panels = parts.solar_panels
        cls.deployable_panels = [
            part.solar_panel
            for part in parts.with_name("nfs-panel-deploying-curved-25-1")
        ]
        cls.fixed_panels = [
            part.solar_panel for part in parts.with_name("nfs-panel-static-curved-25-1")
        ]

    def test_panels(self):
        self.assertEqual(4, len(self.deployable_panels))
        self.assertEqual(4, len(self.fixed_panels))
        self.assertCountEqual(self.deployable_panels + self.fixed_panels, self.panels)

    def test_fixed_panels(self):
        for panel in self.fixed_panels:
            self.assertFalse(panel.deployable)
            self.assertTrue(panel.deployed)
            self.assertEqual(self.state.deployed, panel.state)
            self.assertRaises(RuntimeError, setattr, panel, "deployed", False)
        self.wait_until(
            lambda: any(panel.energy_flow > 0 for panel in self.fixed_panels),
            message="fixed panels generating",
        )
        self.assertTrue(any(panel.sun_exposure > 0 for panel in self.fixed_panels))

    def assert_not_generating(self, panels):
        for panel in panels:
            self.assertEqual(0, panel.energy_flow)
            self.assertEqual(0, panel.sun_exposure)

    def test_extendable_panels(self):
        panels = self.deployable_panels
        for panel in panels:
            self.assertTrue(panel.deployable)
            self.assertFalse(panel.deployed)
            self.assertEqual(self.state.retracted, panel.state)
        self.assert_not_generating(panels)
        self.assertFalse(self.control.solar_panels)

        for panel in panels:
            panel.deployed = True
        self.wait()
        for panel in panels:
            self.assertTrue(panel.deployed)
            self.assertEqual(self.state.deploying, panel.state)
        self.assert_not_generating(panels)
        # A deploying panel counts as deployed, and so do the fixed panels
        self.assertTrue(self.control.solar_panels)

        for panel in panels:
            self.wait_while(
                lambda p=panel: p.state == self.state.deploying, message="deploying"
            )
            self.assertTrue(panel.deployed)
            self.assertEqual(self.state.deployed, panel.state)
        self.assertTrue(self.control.solar_panels)
        self.wait_until(
            lambda: any(panel.energy_flow > 0 for panel in panels),
            message="deployed panels generating",
        )
        self.assertTrue(any(panel.sun_exposure > 0 for panel in panels))

        for panel in panels:
            panel.deployed = False
        self.wait()
        for panel in panels:
            self.assertFalse(panel.deployed)
            self.assertEqual(self.state.retracting, panel.state)
        self.assert_not_generating(panels)

        for panel in panels:
            self.wait_while(
                lambda p=panel: p.state == self.state.retracting, message="retracting"
            )
            self.assertFalse(panel.deployed)
            self.assertEqual(self.state.retracted, panel.state)
        self.assert_not_generating(panels)
        self.assertFalse(self.control.solar_panels)

    def test_control_panels(self):
        self.assertFalse(self.control.solar_panels)
        self.control.solar_panels = True
        for panel in self.deployable_panels:
            self.wait_while(
                lambda p=panel: p.state == self.state.deploying, message="deploying"
            )
            self.assertTrue(panel.deployed)
        self.assertTrue(self.control.solar_panels)
        self.control.solar_panels = False
        for panel in self.deployable_panels:
            self.wait_until(
                lambda p=panel: p.state == self.state.retracted, message="retracted"
            )
            self.assertFalse(panel.deployed)
        self.assertFalse(self.control.solar_panels)


if __name__ == "__main__":
    unittest.main()
