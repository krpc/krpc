import os
import struct
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


# The MFD that RPM's patches put in the stock pods
MFD = "RasterPropMonitorBasicMFD"
BUTTONS = [
    "button_UP",
    "button_DOWN",
    "button_ENTER",
    "button_ESC",
    "button_HOME",
    "button_RIGHT",
    "button_LEFT",
    "buttonR9",
    "buttonR10",
]


class MonitorTestCase(RasterPropMonitorTestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        # The kerbal sits in the root pod, so RPM draws its monitors in the IVA
        root = cls.vessel.parts.root
        cls.monitors = [m for m in cls.rpm.monitors(cls.vessel) if m.part == root]
        assert len(cls.monitors) == 4
        cls.monitor = cls.monitors[0]
        cls.camera = cls.sc.camera
        cls.flight_mode = cls.camera.mode

    def show(self, page):
        self.monitor.active_page = page
        self.assertEqual(page, self.monitor.active_page)
        # RPM refreshes the text every few frames
        self.wait(0.5)


class TestMonitor(MonitorTestCase):
    def test_monitors(self):
        monitors = self.rpm.monitors(self.vessel)
        for monitor in monitors:
            self.assertIn(monitor.part, self.pods)
            self.assertEqual(MFD, monitor.prop_name)
        self.assertEqual(len(monitors), len(set(monitors)))
        self.assertIn(self.monitor, monitors)

    def test_layout(self):
        self.assertEqual(40, self.monitor.screen_width)
        self.assertEqual(20, self.monitor.screen_height)
        self.assertEqual(640, self.monitor.screen_pixel_width)
        self.assertEqual(640, self.monitor.screen_pixel_height)
        self.assertEqual(16, self.monitor.letter_width)
        self.assertEqual(32, self.monitor.letter_height)

    def test_buttons(self):
        self.assertEqual(BUTTONS, self.monitor.buttons)

    def test_pages(self):
        pages = self.monitor.pages
        for page in ("home", "test", "orbit", "targetmenu", "aviapfd"):
            self.assertIn(page, pages)

    def test_active_page(self):
        self.monitor.active_page = "orbit"
        self.assertEqual("orbit", self.monitor.active_page)
        self.assertEqual("home", self.monitors[1].active_page)
        self.monitor.active_page = "home"
        self.assertEqual("home", self.monitor.active_page)

    def test_active_page_unknown(self):
        with self.assertRaises(ValueError):
            self.monitor.active_page = "krpcNoSuchPage"

    def test_default_color(self):
        self.monitor.active_page = "home"
        self.assertEqual((1, 1, 1, 1), self.monitor.default_color)
        # The page sets a green tint
        self.monitor.active_page = "aviapfd"
        self.assertEqual((0, 1, 0, 1), self.monitor.default_color)
        self.monitor.active_page = "home"

    def test_variables(self):
        # RPM reads the page text when it builds the page, so the IVA is not needed
        self.monitor.active_page = "home"
        self.assertEqual(["RPMVERSION"], self.monitor.variables)
        self.monitor.active_page = "test"
        self.assertEqual([], self.monitor.variables)
        self.monitor.active_page = "orbit"
        self.assertIn("PATCHORBITBODY", self.monitor.variables)
        self.monitor.active_page = "home"

    def test_press_button_unknown(self):
        for button in (-1, len(BUTTONS)):
            with self.assertRaises(ValueError):
                self.monitor.press_button(button)

    def test_readback_outside_iva(self):
        # RPM does not draw the monitor in the flight camera, and reading it updates it
        self.assertEqual(self.flight_mode, self.camera.mode)
        self.show("home")
        lines = [line.strip() for line in self.monitor.text.split("\n")]
        self.assertIn("Junk Systems Inc.", lines)
        self.assertIn("Junk Systems Inc.", self.monitor.raw_text)
        image = self.monitor.image()
        self.assertEqual(b"\x89PNG\r\n\x1a\n", image[:8])
        self.assertEqual((640, 640), struct.unpack(">II", image[16:24]))

    def test_other_pod_outside_iva(self):
        root = self.vessel.parts.root
        other = [m for m in self.rpm.monitors(self.vessel) if m.part != root][0]
        other.active_page = "home"
        self.wait(0.1)
        self.assertIn("Junk Systems Inc.", other.text)


class TestMonitorIVA(MonitorTestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        # KSP does not enter the IVA straight after a launch. Setting the mode again in the
        # IVA leaves it
        for _ in range(20):
            if cls.camera.mode == cls.sc.CameraMode.iva:
                break
            cls.camera.mode = cls.sc.CameraMode.iva
            cls.wait(0.5)
        assert cls.camera.mode == cls.sc.CameraMode.iva
        cls.wait(1)
        cls.version = cls.rpm.computer(cls.monitor.part).format_variables(
            "{0}$&$RPMVERSION"
        )

    @classmethod
    def tearDownClass(cls):
        cls.camera.mode = cls.flight_mode
        super().tearDownClass()

    def test_text(self):
        self.show("home")
        lines = self.monitor.text.split("\n")
        self.assertIn("Junk Systems Inc.", [line.strip() for line in lines])
        # The half width tag draws nothing, and the variable is formatted
        self.assertIn("- HOME RPM v" + self.version, lines)

    def test_raw_text(self):
        self.show("home")
        lines = self.monitor.raw_text.split("\n")
        self.assertIn("-[hw] HOME[/hw] RPM v" + self.version, lines)

    def test_text_tags(self):
        # The character test page uses each kind of tag
        self.show("test")
        raw = self.monitor.raw_text.split("\n")
        self.assertEqual("[@x38][@y20]This page intentionally left blank", raw[1])
        lines = [line.rstrip() for line in self.monitor.text.split("\n")]
        self.assertEqual(len(raw), len(lines))
        self.assertEqual("This page intentionally left blank", lines[1])
        for line in (
            "The system is also capable ofsuperscript",
            "as well assubscript.",
            "Also halfwidth and double.",
            "and both sub/superscript and width at once",
            "And multiple different fonts at once.",
            "Remember: All pages are user-editable!",
        ):
            self.assertIn(line, lines)

    def test_image(self):
        self.show("home")
        image = self.monitor.image()
        self.assertEqual(b"\x89PNG\r\n\x1a\n", image[:8])
        width, height = struct.unpack(">II", image[16:24])
        self.assertEqual((640, 640), (width, height))

    def test_image_jpeg(self):
        self.show("home")
        image = self.monitor.image(self.rpm.ImageFormat.jpeg)
        self.assertEqual(b"\xff\xd8\xff", image[:3])
        self.assertEqual((640, 640), jpeg_size(image))
        for quality in (0, 101):
            with self.assertRaises(ValueError):
                self.monitor.image(self.rpm.ImageFormat.jpeg, quality)
        # PNG ignores the quality
        self.assertEqual(b"\x89PNG\r\n\x1a\n", self.monitor.image(quality=0)[:8])

    def test_press_button(self):
        # The target menu moves its cursor with the down button
        self.show("targetmenu")
        before = self.monitor.raw_text
        self.monitor.press_button(BUTTONS.index("button_DOWN"))
        self.wait(0.5)
        self.assertNotEqual(before, self.monitor.raw_text)
        self.monitor.press_button(BUTTONS.index("button_UP"))
        self.wait(0.5)
        self.assertEqual(before, self.monitor.raw_text)

    def test_other_pod(self):
        # The kerbal is not in the other pod, so RPM does not draw its monitors, and reading
        # one updates it
        root = self.vessel.parts.root
        other = [m for m in self.rpm.monitors(self.vessel) if m.part != root][0]
        other.active_page = "home"
        self.wait(0.1)
        self.assertIn("- HOME RPM v" + self.version, other.text.split("\n"))


def jpeg_size(data):
    """The width and height of a JPEG image, from its start of frame segment."""
    pos = 2
    while True:
        marker, length = struct.unpack(">2sH", data[pos : pos + 4])
        if marker in (b"\xff\xc0", b"\xff\xc2"):
            height, width = struct.unpack(">HH", data[pos + 5 : pos + 9])
            return width, height
        pos += 2 + length


if __name__ == "__main__":
    unittest.main()
