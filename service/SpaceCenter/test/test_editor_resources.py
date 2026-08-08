import unittest

import krpctest


class TestEditorResources(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.enter_editor("VAB", craft="Resources")
        cls.vessel = cls.connect().space_center.editor.vessel

    @classmethod
    def tearDownClass(cls):
        cls.leave_editor()

    def test_vessel_resources(self):
        resources = self.vessel.resources
        self.assertGreater(len(resources.all), 0)
        for name in ("MonoPropellant", "LiquidFuel", "Oxidizer", "SolidFuel"):
            self.assertIn(name, resources.names)
            self.assertTrue(resources.has_resource(name))
            self.assertGreater(resources.max(name), 0)
            self.assertGreater(resources.amount(name), 0)
            self.assertLessEqual(resources.amount(name), resources.max(name))
        self.assertFalse(resources.has_resource("Not a resource"))

    def test_resource_members(self):
        resource = self.vessel.resources.with_resource("SolidFuel")[0]
        self.assertEqual("SolidFuel", resource.name)
        self.assertGreater(resource.max, 0)
        self.assertGreater(resource.amount, 0)
        self.assertAlmostEqual(7.5, resource.density, places=3)
        self.assertIn(resource.part, self.vessel.parts.all)

    def test_part_resources(self):
        # Every part's resources add up to the vessel's.
        total = 0
        for part in self.vessel.parts.all:
            total += part.resources.amount("SolidFuel")
        self.assertAlmostEqual(
            self.vessel.resources.amount("SolidFuel"), total, places=2
        )

    def test_stage_resources(self):
        # The first decouple stage holds no more of a resource than the whole vessel.
        stage = self.vessel.decouple_stages[0]
        resources = stage.resources()
        self.assertLessEqual(
            resources.amount("SolidFuel"), self.vessel.resources.amount("SolidFuel") + 1
        )
        self.assertGreater(len(self.vessel.stages[0].resources().all), 0)

    def test_enabled(self):
        resource = self.vessel.resources.with_resource("MonoPropellant")[0]
        self.assertTrue(resource.enabled)
        try:
            resource.enabled = False
            self.assertFalse(resource.enabled)
        finally:
            resource.enabled = True
        self.assertTrue(resource.enabled)

    def test_flow_mode_and_density_are_available(self):
        space_center = self.connect().space_center
        self.assertAlmostEqual(
            5, space_center.Resources.density("LiquidFuel"), places=3
        )
        self.assertIsNotNone(space_center.Resources.flow_mode("LiquidFuel"))


class TestEditorResourcesMatchFlight(krpctest.TestCase):
    """The same vessel must report the same resources in the editor and once launched."""

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.enter_editor("VAB", craft="Resources")
        vessel = cls.connect().space_center.editor.vessel
        cls.editor_names = sorted(vessel.resources.names)
        cls.editor_amounts = {
            name: vessel.resources.amount(name) for name in cls.editor_names
        }
        cls.editor_maxes = {
            name: vessel.resources.max(name) for name in cls.editor_names
        }
        cls.leave_editor()
        cls.launch_vessel_from_vab("Resources")
        cls.remove_other_vessels()
        cls.resources = cls.connect().space_center.active_vessel.resources

    def test_same_names(self):
        self.assertEqual(self.editor_names, sorted(self.resources.names))

    def test_same_amounts(self):
        for name in self.editor_names:
            self.assertAlmostEqual(
                self.editor_amounts[name], self.resources.amount(name), delta=0.5
            )
            self.assertAlmostEqual(
                self.editor_maxes[name], self.resources.max(name), delta=0.5
            )


if __name__ == "__main__":
    unittest.main()
