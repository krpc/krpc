import os
import unittest

from krpctools.game.ksp import TestCase

CRAFT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "craft")

# RPM's patches give the Mk1 lander can a computer, and leave the fuel tank without one
POD = "landerCabinSmall"
TANK = "fuelTankSmallFlat"


class RasterPropMonitorTestCase(TestCase):
    mods = ["RasterPropMonitor"]

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        # Two pods, so the part and vessel variables differ
        cls.launch_vessel_from_vab("RasterPropMonitor", directory=CRAFT)
        cls.remove_other_vessels()
        cls.conn = cls.connect()
        cls.rpm = cls.conn.raster_prop_monitor
        cls.sc = cls.conn.space_center
        vessel = cls.sc.active_vessel
        assert vessel is not None
        cls.vessel = vessel
        cls.pods = vessel.parts.with_name(POD)
        assert len(cls.pods) == 2


class TestComputer(RasterPropMonitorTestCase):
    def test_available(self):
        self.assertTrue(self.rpm.available)

    def test_computers(self):
        computers = self.rpm.computers(self.vessel)
        self.assertCountEqual(self.pods, [computer.part for computer in computers])

    def test_computer(self):
        computer = self.rpm.computer(self.pods[0])
        self.assertEqual(self.pods[0], computer.part)
        self.assertIn(computer, self.rpm.computers(self.vessel))

    def test_computer_without_rpm(self):
        part = self.vessel.parts.with_name(TANK)[0]
        with self.assertRaises(ValueError):
            self.rpm.computer(part)


class TestPersistentVariables(RasterPropMonitorTestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.first, cls.second = (cls.rpm.computer(pod) for pod in cls.pods)

    def test_unset(self):
        self.assertIsNone(self.first.persistent_variable("krpcUnset"))
        self.assertIsNone(self.rpm.global_persistent_variable(self.vessel, "krpcUnset"))
        # A prop reads an unset variable as -1
        self.assertEqual(-1, self.first.number_variable("PERSISTENT_krpcUnset"))

    def test_set_and_remove(self):
        self.first.set_persistent_variable("krpcPart", 42.5)
        self.assertEqual(42.5, self.first.persistent_variable("krpcPart"))
        self.assertEqual(42.5, self.first.number_variable("PERSISTENT_krpcPart"))
        self.assertIsNone(self.second.persistent_variable("krpcPart"))
        self.assertIsNone(self.rpm.global_persistent_variable(self.vessel, "krpcPart"))
        self.first.remove_persistent_variable("krpcPart")
        self.assertIsNone(self.first.persistent_variable("krpcPart"))
        self.assertEqual(-1, self.first.number_variable("PERSISTENT_krpcPart"))

    def test_global(self):
        self.rpm.set_global_persistent_variable(self.vessel, "krpcGlobal", 3)
        self.assertEqual(
            3, self.rpm.global_persistent_variable(self.vessel, "krpcGlobal")
        )
        for computer in (self.first, self.second):
            self.assertEqual(3, computer.persistent_variable("krpcGlobal"))
            self.assertEqual(3, computer.number_variable("PERSISTENT_krpcGlobal"))
        self.rpm.remove_global_persistent_variable(self.vessel, "krpcGlobal")
        self.assertIsNone(
            self.rpm.global_persistent_variable(self.vessel, "krpcGlobal")
        )
        for computer in (self.first, self.second):
            self.assertIsNone(computer.persistent_variable("krpcGlobal"))
            self.assertEqual(-1, computer.number_variable("PERSISTENT_krpcGlobal"))

    def test_part_overrides_global(self):
        self.rpm.set_global_persistent_variable(self.vessel, "krpcBoth", 1)
        self.first.set_persistent_variable("krpcBoth", 2)
        self.assertEqual(2, self.first.persistent_variable("krpcBoth"))
        self.assertEqual(2, self.first.number_variable("PERSISTENT_krpcBoth"))
        self.assertEqual(1, self.second.persistent_variable("krpcBoth"))
        self.assertEqual(1, self.second.number_variable("PERSISTENT_krpcBoth"))
        self.assertEqual(
            1, self.rpm.global_persistent_variable(self.vessel, "krpcBoth")
        )
        self.first.remove_persistent_variable("krpcBoth")
        self.assertEqual(1, self.first.persistent_variable("krpcBoth"))
        self.assertEqual(1, self.first.number_variable("PERSISTENT_krpcBoth"))
        self.rpm.remove_global_persistent_variable(self.vessel, "krpcBoth")

    def test_remove_unset(self):
        self.first.remove_persistent_variable("krpcUnset")
        self.rpm.remove_global_persistent_variable(self.vessel, "krpcUnset")
        self.assertIsNone(self.first.persistent_variable("krpcUnset"))

    def test_empty_name(self):
        for name in ("", " "):
            with self.assertRaises(ValueError):
                self.first.persistent_variable(name)
            with self.assertRaises(ValueError):
                self.first.set_persistent_variable(name, 1)
            with self.assertRaises(ValueError):
                self.rpm.set_global_persistent_variable(self.vessel, name, 1)

    def test_changed(self):
        with self.first.persistent_variable_changed("krpcChanged") as event:
            with event.condition:
                self.first.set_persistent_variable("krpcChanged", 1)
                self.assertTrue(event.wait(5))
            with event.condition:
                # Setting the same value is no change
                self.first.set_persistent_variable("krpcChanged", 1)
                self.assertFalse(event.wait(0.5))
            with event.condition:
                self.first.remove_persistent_variable("krpcChanged")
                self.assertTrue(event.wait(5))

    def test_changed_by_global(self):
        with self.first.persistent_variable_changed("krpcChangedGlobal") as event:
            with event.condition:
                self.rpm.set_global_persistent_variable(
                    self.vessel, "krpcChangedGlobal", 1
                )
                self.assertTrue(event.wait(5))
        self.rpm.remove_global_persistent_variable(self.vessel, "krpcChangedGlobal")

    def test_global_changed(self):
        event = self.rpm.global_persistent_variable_changed(
            self.vessel, "krpcGlobalChanged"
        )
        with event:
            with event.condition:
                self.rpm.set_global_persistent_variable(
                    self.vessel, "krpcGlobalChanged", 5
                )
                self.assertTrue(event.wait(5))
            with event.condition:
                # A part's own value leaves the vessel's unchanged
                self.first.set_persistent_variable("krpcGlobalChanged", 6)
                self.assertFalse(event.wait(0.5))
        self.first.remove_persistent_variable("krpcGlobalChanged")
        self.rpm.remove_global_persistent_variable(self.vessel, "krpcGlobalChanged")


class TestVariables(RasterPropMonitorTestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.computer = cls.rpm.computer(cls.pods[0])
        cls.computer.set_persistent_variable("krpcValue", 1234.5)

    def test_number(self):
        altitude = self.vessel.flight().mean_altitude
        self.assertAlmostEqual(
            altitude, self.computer.number_variable("ALTITUDE"), delta=1
        )
        self.assertEqual(1234.5, self.computer.number_variable("PERSISTENT_krpcValue"))

    def test_string(self):
        self.assertEqual(self.vessel.name, self.computer.string_variable("NAME"))
        self.assertEqual(
            "1234.5", self.computer.string_variable("PERSISTENT_krpcValue")
        )

    def test_number_of_string(self):
        with self.assertRaises(RuntimeError):
            self.computer.number_variable("NAME")

    def test_type(self):
        VariableType = self.rpm.VariableType
        self.assertEqual(VariableType.number, self.computer.variable_type("ALTITUDE"))
        self.assertEqual(VariableType.string, self.computer.variable_type("NAME"))

    def test_unrecognized(self):
        with self.assertRaises(ValueError):
            self.computer.number_variable("KRPC_NOT_A_VARIABLE")
        with self.assertRaises(ValueError):
            self.computer.string_variable("KRPC_NOT_A_VARIABLE")
        with self.assertRaises(ValueError):
            self.computer.variable_type("KRPC_NOT_A_VARIABLE")
        with self.assertRaises(ValueError):
            self.computer.format_variables("{0}$&$KRPC_NOT_A_VARIABLE")

    def test_format(self):
        self.assertEqual(
            "Value 1234.5",
            self.computer.format_variables("Value {0:F1}$&$PERSISTENT_krpcValue"),
        )
        self.assertEqual(
            "1234.5 " + self.vessel.name,
            self.computer.format_variables("{0} {1}$&$PERSISTENT_krpcValue NAME"),
        )

    def test_format_rpm_formats(self):
        self.assertEqual(
            "1.2 k",
            self.computer.format_variables(
                "{0:SIP_6.1}$&$PERSISTENT_krpcValue"
            ).strip(),
        )

    def test_format_lines(self):
        self.assertEqual(
            "A 1234.5\nB\nC 1234.50",
            self.computer.format_variables(
                "A {0:F1}$&$PERSISTENT_krpcValue\nB  \nC {0:F2}$&$PERSISTENT_krpcValue"
            ),
        )

    def test_format_without_variables(self):
        # Text with no variables is left as written
        self.assertEqual(
            "A  \n[#ff0000]B", self.computer.format_variables("A  \n[#ff0000]B")
        )

    def test_format_invalid(self):
        with self.assertRaises(ValueError):
            self.computer.format_variables("{0}$&$ALTITUDE$&$NAME")
        with self.assertRaises(ValueError):
            self.computer.format_variables("{1}$&$ALTITUDE")


if __name__ == "__main__":
    unittest.main()
