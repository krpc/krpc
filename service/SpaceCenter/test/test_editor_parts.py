import unittest

import krpctest


class TestEditorParts(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.enter_editor("VAB", craft="Parts")
        cls.parts = cls.connect().space_center.editor.vessel.parts

    @classmethod
    def tearDownClass(cls):
        cls.leave_editor()

    def test_all(self):
        parts = self.parts.all
        self.assertGreater(len(parts), 0)
        self.assertCountEqual(parts, self.parts.all)

    def test_root(self):
        root = self.parts.root
        self.assertIsNone(root.parent)
        self.assertIn(root, self.parts.all)
        # Every part reaches the root by following its parents.
        for part in self.parts.all:
            while part.parent is not None:
                part = part.parent
            self.assertEqual(root, part)

    def test_tree(self):
        parts = self.parts.all
        root = self.parts.root
        # Every part is a child of its parent, and the tree covers the whole vessel.
        seen = []
        stack = [root]
        while stack:
            part = stack.pop()
            seen.append(part)
            for child in part.children:
                self.assertEqual(part, child.parent)
                stack.append(child)
        self.assertEqual(len(parts), len(seen))

    def test_with_name(self):
        name = self.parts.root.name
        found = self.parts.with_name(name)
        self.assertGreater(len(found), 0)
        for part in found:
            self.assertEqual(name, part.name)
        self.assertEqual([], self.parts.with_name("NotAPart"))

    def test_with_title(self):
        title = self.parts.root.title
        self.assertGreater(len(self.parts.with_title(title)), 0)
        self.assertEqual([], self.parts.with_title("Not A Part"))

    def test_with_module(self):
        self.assertGreater(len(self.parts.with_module("ModuleCommand")), 0)
        self.assertEqual([], self.parts.with_module("NotAModule"))

    def test_typed_collections(self):
        # The vessel carries at least one of each of these.
        self.assertGreater(len(self.parts.engines), 0)
        self.assertGreater(len(self.parts.decouplers), 0)
        self.assertGreater(len(self.parts.solar_panels), 0)
        self.assertGreater(len(self.parts.parachutes), 0)
        self.assertGreater(len(self.parts.antennas), 0)

    def test_static_part_members(self):
        part = self.parts.root
        self.assertGreater(len(part.name), 0)
        self.assertGreater(len(part.title), 0)
        self.assertGreater(part.cost, 0)
        self.assertGreater(part.mass, 0)
        self.assertGreater(part.dry_mass, 0)
        self.assertGreaterEqual(part.mass, part.dry_mass)
        self.assertGreater(part.impact_tolerance, 0)
        self.assertGreater(part.max_temperature, 0)
        self.assertGreater(part.max_skin_temperature, 0)
        self.assertFalse(part.massless)
        self.assertGreater(len(part.modules), 0)
        self.assertIsNotNone(part.config)

    def test_flight_only_part_members_raise(self):
        part = self.parts.root
        for name in (
            "vessel",
            "crew",
            "available_seats",
            "temperature",
            "skin_temperature",
            "thermal_mass",
            "dynamic_pressure",
            "shielded",
            "moment_of_inertia",
            "reference_frame",
        ):
            self.assertRaises(RuntimeError, getattr, part, name)
        self.assertRaises(RuntimeError, getattr, self.parts, "controlling")

    def test_engine_design_members(self):
        engine = self.parts.engines[0]
        self.assertGreater(engine.max_vacuum_thrust, 0)
        self.assertGreater(engine.vacuum_specific_impulse, 0)
        self.assertGreater(engine.kerbin_sea_level_specific_impulse, 0)
        self.assertGreater(len(engine.propellant_names), 0)
        limit = engine.thrust_limit
        try:
            engine.thrust_limit = 0.5
            self.assertAlmostEqual(0.5, engine.thrust_limit, places=3)
        finally:
            engine.thrust_limit = limit
        # Members that need a running engine are not available in the editor.
        for name in ("thrust", "available_thrust", "throttle", "has_fuel"):
            self.assertRaises(RuntimeError, getattr, engine, name)


class TestEditorPartsMatchFlight(krpctest.TestCase):
    """The same vessel, inspected in the editor and after launching it, must report the
    same parts tree."""

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.enter_editor("VAB", craft="Staging")
        cls.editor_parts = cls.connect().space_center.editor.vessel.parts
        cls.editor_names = sorted(part.name for part in cls.editor_parts.all)
        cls.editor_root = cls.editor_parts.root.name
        cls.editor_stages = sorted(part.stage for part in cls.editor_parts.all)
        cls.leave_editor()
        cls.launch_vessel_from_vab("Staging")
        cls.remove_other_vessels()
        cls.flight_parts = cls.connect().space_center.active_vessel.parts

    def test_same_parts(self):
        self.assertEqual(
            self.editor_names, sorted(part.name for part in self.flight_parts.all)
        )

    def test_same_root(self):
        self.assertEqual(self.editor_root, self.flight_parts.root.name)

    def test_same_staging(self):
        self.assertEqual(
            self.editor_stages, sorted(part.stage for part in self.flight_parts.all)
        )


if __name__ == "__main__":
    unittest.main()
