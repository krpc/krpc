import unittest
from typing import Any

from krpctools.game.ksp import TestCase

# The b9PartSwitchTest tank, from the B9PartSwitch config overlay, is a 100 kg part whose
# subtypes add a tank mass and resources. The craft saves it with MonoPropellant selected.
SUBTYPES = [
    # Name, dry mass in kg, resources
    ("LFO", 150, {"LiquidFuel": 180, "Oxidizer": 220}),
    ("MonoPropellant", 260, {"MonoPropellant": 600}),
    ("Structural", 600, {}),
]
DENSITIES = {"LiquidFuel": 5, "Oxidizer": 5, "MonoPropellant": 4}
SAVED = 1


class B9PartSwitchTestCase(TestCase):
    """Checks on the test tank, which tests reach through self.tank."""

    mods = ["B9PartSwitch"]
    tank: Any

    def field(self, name):
        module = next(m for m in self.tank.modules if m.name == "ModuleB9PartSwitch")
        return next(f for f in module.field_list if f.name == name)

    def check_subtype(self, index):
        name, dry_mass, resources = SUBTYPES[index]
        self.assertEqual(index, self.field("currentSubtypeIndex").int_value)
        self.assertEqual(name, self.field("currentSubtypeTitle").value)
        tank_resources = self.tank.resources
        self.assertCountEqual(resources.keys(), tank_resources.names)
        for resource, amount in resources.items():
            self.assertAlmostEqual(amount, tank_resources.max(resource), places=3)
            self.assertAlmostEqual(amount, tank_resources.amount(resource), places=3)
        wet_mass = dry_mass + sum(DENSITIES[r] * a for r, a in resources.items())
        self.assertAlmostEqual(dry_mass, self.tank.dry_mass, places=1)
        self.assertAlmostEqual(wet_mass, self.tank.mass, places=1)


class TestPartsB9PartSwitchEditor(B9PartSwitchTestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.enter_editor("VAB", craft="PartsB9PartSwitch")

    @classmethod
    def tearDownClass(cls):
        cls.leave_editor()

    def setUp(self):
        editor = self.connect().space_center.editor
        editor.load_vessel("VAB", "PartsB9PartSwitch")
        self.wait()
        self.tank = editor.vessel.parts.with_name("b9PartSwitchTest")[0]

    def test_saved_subtype(self):
        self.check_subtype(SAVED)

    def test_switch_subtype(self):
        for index in (0, 2, SAVED):
            self.field("currentSubtypeIndex").int_value = index
            self.wait()
            self.check_subtype(index)


class TestPartsB9PartSwitchFlight(B9PartSwitchTestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("PartsB9PartSwitch")
        cls.remove_other_vessels()
        vessel = cls.connect().space_center.active_vessel
        cls.tank = vessel.parts.with_name("b9PartSwitchTest")[0]

    def test_saved_subtype(self):
        self.check_subtype(SAVED)


if __name__ == "__main__":
    unittest.main()
