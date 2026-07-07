import unittest
import krpctest
from krpctest import assert_resources_equivalent


class FakeResources:

    def __init__(self, resources):
        self._resources = resources
        self.names = list(resources.keys())

    def amount(self, name):
        return self._resources[name][0]

    def max(self, name):
        return self._resources[name][1]


class TestResourcesEquivalence(unittest.TestCase):

    def test_detects_name_discrepancies(self):
        expected = FakeResources({"LiquidFuel": (100, 200)})
        actual = FakeResources({"Oxidizer": (100, 200)})
        with self.assertRaises(AssertionError):
            assert_resources_equivalent(self, expected, actual)

    def test_detects_amount_discrepancies(self):
        expected = FakeResources({"LiquidFuel": (100, 200)})
        actual = FakeResources({"LiquidFuel": (99, 200)})
        with self.assertRaises(AssertionError):
            assert_resources_equivalent(self, expected, actual, delta=0)

    def test_detects_max_discrepancies(self):
        expected = FakeResources({"LiquidFuel": (100, 200)})
        actual = FakeResources({"LiquidFuel": (100, 199)})
        with self.assertRaises(AssertionError):
            assert_resources_equivalent(self, expected, actual, delta=0)


class ResourcesTest:

    density = {"MonoPropellant": 4, "LiquidFuel": 5, "Oxidizer": 5, "SolidFuel": 7.5}


class TestResources(krpctest.TestCase, ResourcesTest):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        active_vessel = cls.connect().space_center.active_vessel
        if active_vessel is None or active_vessel.name != "Resources":
            cls.launch_vessel_from_vab("Resources")
        cls.vessel = cls.connect().space_center.active_vessel
        cls.num_stages = len(cls.expected.keys()) - 1

    expected = {
        -1: {
            "ElectricCharge": (150, 150),
            "MonoPropellant": (15, 30),
            "LiquidFuel": (0, 0),
            "Oxidizer": (0, 0),
            "SolidFuel": (0, 0),
        },
        0: {
            "ElectricCharge": (0, 0),
            "MonoPropellant": (0, 0),
            "LiquidFuel": (0, 0),
            "Oxidizer": (0, 0),
            "SolidFuel": (0, 0),
        },
        1: {
            "ElectricCharge": (0, 0),
            "MonoPropellant": (0, 0),
            "LiquidFuel": (720, 1440),
            "Oxidizer": (1000, 1760),
            "SolidFuel": (0, 0),
        },
        2: {
            "ElectricCharge": (0, 0),
            "MonoPropellant": (0, 0),
            "LiquidFuel": (0, 0),
            "Oxidizer": (0, 0),
            "SolidFuel": (0, 0),
        },
        3: {
            "ElectricCharge": (0, 0),
            "MonoPropellant": (0, 0),
            "LiquidFuel": (720 + 720 + 300, 1440 + 1440 + 720),
            "Oxidizer": (1000 + 1000 + 400, 1760 + 1760 + 880),
            "SolidFuel": (13, 15),
        },
        4: {
            "ElectricCharge": (0, 0),
            "MonoPropellant": (0, 0),
            "LiquidFuel": (0, 0),
            "Oxidizer": (0, 0),
            "SolidFuel": (300 * 4 + 3 * 8, 850 * 4 + 8 * 8),
        },
        5: {
            "ElectricCharge": (0, 0),
            "MonoPropellant": (0, 0),
            "LiquidFuel": (0, 0),
            "Oxidizer": (0, 0),
            "SolidFuel": (0, 0),
        },
    }

    expected_names = {
        -1: set(["ElectricCharge", "MonoPropellant"]),
        0: set(),
        1: set(["LiquidFuel", "Oxidizer", "ElectricCharge"]),
        2: set(),
        3: set(["LiquidFuel", "Oxidizer", "ElectricCharge", "SolidFuel"]),
        4: set(["SolidFuel"]),
        5: set(),
    }

    expected_names_cumulative = {
        -1: set(
            ["ElectricCharge", "MonoPropellant", "SolidFuel", "LiquidFuel", "Oxidizer"]
        ),
        0: set(["ElectricCharge", "SolidFuel", "LiquidFuel", "Oxidizer"]),
        1: set(["ElectricCharge", "SolidFuel", "LiquidFuel", "Oxidizer"]),
        2: set(["ElectricCharge", "SolidFuel", "LiquidFuel", "Oxidizer"]),
        3: set(["ElectricCharge", "LiquidFuel", "Oxidizer", "SolidFuel"]),
        4: set(["SolidFuel"]),
        5: set(),
    }

    def test_equality(self):
        self.assertEqual(self.vessel.resources, self.vessel.resources)

    def test_names(self):
        self.assertEqual(
            set(
                [
                    "ElectricCharge",
                    "MonoPropellant",
                    "LiquidFuel",
                    "Oxidizer",
                    "SolidFuel",
                ]
            ),
            set(self.vessel.resources.names),
        )

    def test_per_stage_amounts(self):
        for stage in range(-1, self.num_stages):
            resources = self.vessel.resources_in_decouple_stage(
                stage=stage, cumulative=False
            )
            self.assertEqual(self.expected_names[stage], set(resources.names))
            for name in resources.names:
                self.assertTrue(resources.has_resource(name))
                self.assertAlmostEqual(
                    self.expected[stage][name][0], resources.amount(name), delta=1
                )
                self.assertAlmostEqual(
                    self.expected[stage][name][1], resources.max(name), delta=1
                )

    def test_decouple_stage_resources_migration_default(self):
        for stage in range(self.num_stages):
            legacy = self.vessel.resources_in_decouple_stage(stage=stage)
            migrated = self.vessel.decouple_stage_at(stage).resources()
            assert_resources_equivalent(self, legacy, migrated)

    def test_decouple_stage_at_zero_value_stage_iteration(self):
        """Raw stage 0 may have no decoupling parts; DecoupleStageAt must not throw."""
        stage_obj = self.vessel.decouple_stage_at(0)
        self.assertEqual(0, stage_obj.number)
        self.assertIsInstance(stage_obj.parts, list)
        legacy = self.vessel.resources_in_decouple_stage(stage=0, cumulative=False)
        migrated = stage_obj.resources(cumulative=False)
        assert_resources_equivalent(self, legacy, migrated)

    def test_decouple_stage_at_iterates_all_raw_stage_numbers(self):
        for stage in range(self.num_stages):
            stage_obj = self.vessel.decouple_stage_at(stage)
            self.assertEqual(stage, stage_obj.number)

    def test_per_stage_amounts_cumulative(self):
        for stage in range(self.num_stages):
            resources = self.vessel.resources_in_decouple_stage(stage=stage)
            self.assertEqual(
                self.expected_names_cumulative[stage], set(resources.names)
            )
            for name in resources.names:
                expected_amount = sum(
                    self.expected[x][name][0] for x in range(stage, self.num_stages - 1)
                )
                expected_max = sum(
                    self.expected[x][name][1] for x in range(stage, self.num_stages - 1)
                )
                self.assertAlmostEqual(expected_amount, resources.amount(name), delta=1)
                self.assertAlmostEqual(expected_max, resources.max(name), delta=1)

    def test_total_amounts(self):
        resources = self.vessel.resources
        self.assertEqual(
            set(
                [
                    "SolidFuel",
                    "ElectricCharge",
                    "MonoPropellant",
                    "LiquidFuel",
                    "Oxidizer",
                ]
            ),
            set(resources.names),
        )
        for name in resources.names:
            expected_amount = sum(
                self.expected[stage][name][0] for stage in range(-1, self.num_stages)
            )
            expected_max = sum(
                self.expected[stage][name][1] for stage in range(-1, self.num_stages)
            )
            self.assertAlmostEqual(expected_amount, resources.amount(name), delta=1)
            self.assertAlmostEqual(expected_max, resources.max(name), delta=1)

    def test_vessel_mass(self):
        mass = self.vessel.dry_mass
        self.assertAlmostEqual(26905, mass, places=2)
        resources = self.vessel.resources
        self.assertEqual(
            set(
                [
                    "SolidFuel",
                    "ElectricCharge",
                    "MonoPropellant",
                    "LiquidFuel",
                    "Oxidizer",
                ]
            ),
            set(resources.names),
        )
        for name in resources.names:
            amount = sum(
                self.expected[stage][name][0] for stage in range(-1, self.num_stages)
            )
            if name in self.density:
                mass += amount * self.density[name]
        self.assertAlmostEqual(mass, self.vessel.mass, places=2)

    def test_part_resources(self):
        mode = self.connect().space_center.ResourceFlowMode
        resources = next(iter(self.vessel.parts.with_name("solidBooster1-1"))).resources
        self.assertEqual(set(["SolidFuel"]), set(resources.names))
        self.assertTrue(resources.has_resource("SolidFuel"))
        self.assertFalse(resources.has_resource("LiquidFuel"))
        self.assertEqual(set(["SolidFuel"]), set(resources.names))
        self.assertEqual(300, resources.amount("SolidFuel"))
        self.assertEqual(850, resources.max("SolidFuel"))
        self.assertTrue(resources.enabled)

        part_resources = resources.all
        self.assertEqual(1, len(part_resources))
        r = part_resources[0]
        self.assertEqual("SolidFuel", r.name)
        self.assertEqual(300, r.amount)
        self.assertEqual(850, r.max)
        self.assertEqual(self.density["SolidFuel"], r.density)
        self.assertEqual(mode.none, r.flow_mode)
        self.assertTrue(r.enabled)

        resources = next(iter(self.vessel.parts.with_name("Rockomax16.BW"))).resources
        self.assertEqual(set(["LiquidFuel", "Oxidizer"]), set(resources.names))
        self.assertTrue(resources.has_resource("LiquidFuel"))
        self.assertTrue(resources.has_resource("Oxidizer"))
        self.assertFalse(resources.has_resource("SolidFuel"))
        self.assertEqual(300, resources.amount("LiquidFuel"))
        self.assertEqual(720, resources.max("LiquidFuel"))
        self.assertEqual(400, resources.amount("Oxidizer"))
        self.assertEqual(880, resources.max("Oxidizer"))
        self.assertTrue(resources.enabled)

        part_resources = resources.all
        self.assertEqual(2, len(part_resources))
        for r in part_resources:
            if r.name == "LiquidFuel":
                self.assertEqual("LiquidFuel", r.name)
                self.assertEqual(300, r.amount)
                self.assertEqual(720, r.max)
                self.assertEqual(self.density["LiquidFuel"], r.density)
                self.assertEqual(mode.adjacent, r.flow_mode)
                self.assertTrue(r.enabled)
            else:
                self.assertEqual("Oxidizer", r.name)
                self.assertEqual(400, r.amount)
                self.assertEqual(880, r.max)
                self.assertEqual(self.density["Oxidizer"], r.density)
                self.assertEqual(mode.adjacent, r.flow_mode)
                self.assertTrue(r.enabled)

        part_resources = resources.with_resource("LiquidFuel")
        self.assertEqual(1, len(part_resources))
        self.assertEqual("LiquidFuel", part_resources[0].name)


class TestResourcesStaticMethods(krpctest.TestCase, ResourcesTest):

    @classmethod
    def setUpClass(cls):
        cls.resources = cls.connect().space_center.Resources

    def test_density(self):
        for name, expected in self.density.items():
            self.assertEqual(expected, self.resources.density(name))
        self.assertRaises(ValueError, self.resources.density, "Foo")

    def test_flow_mode(self):
        mode = self.connect().space_center.ResourceFlowMode
        self.assertEqual(mode.stage, self.resources.flow_mode("ElectricCharge"))
        self.assertEqual(mode.vessel, self.resources.flow_mode("IntakeAir"))
        self.assertEqual(mode.stage, self.resources.flow_mode("MonoPropellant"))
        self.assertEqual(mode.stage, self.resources.flow_mode("XenonGas"))
        self.assertEqual(mode.adjacent, self.resources.flow_mode("LiquidFuel"))
        self.assertEqual(mode.adjacent, self.resources.flow_mode("Oxidizer"))
        self.assertEqual(mode.none, self.resources.flow_mode("SolidFuel"))
        self.assertEqual(mode.vessel, self.resources.flow_mode("Ore"))
        self.assertEqual(mode.none, self.resources.flow_mode("Ablator"))
        self.assertRaises(ValueError, self.resources.flow_mode, "Foo")


if __name__ == "__main__":
    unittest.main()
