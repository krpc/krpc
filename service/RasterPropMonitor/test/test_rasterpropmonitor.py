import os
import time
import unittest

from krpctools.game.images import assert_jpeg, assert_png, png
from krpctools.game.ksp import TestCase

CRAFT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "craft")

# RPM's patches give the Mk1 lander can a computer, and leave the fuel tank without one
POD = "landerCabinSmall"
TANK = "fuelTankSmallFlat"
# RPM's radial camera part, two of which sit on the root pod as ExtCam1 and ExtCam2
CAMERA = "JSIPrimitiveExternalCamera.v2"


class RasterPropMonitorTestCase(TestCase):
    mods = ["RasterPropMonitor"]

    @classmethod
    def setUpClass(cls):
        # Two pods, so the part and vessel variables differ
        cls.vessel = cls.launch("RasterPropMonitor", directory=CRAFT)
        cls.conn = cls.connect()
        cls.rpm = cls.conn.raster_prop_monitor
        cls.sc = cls.conn.space_center
        cls.pods = cls.vessel.parts.with_name(POD)
        assert len(cls.pods) == 2


class TestComputer(RasterPropMonitorTestCase):
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

    @classmethod
    def enter_iva(cls):
        cls.camera.mode = cls.sc.CameraMode.iva
        cls.wait(1)

    @classmethod
    def leave_iva(cls):
        cls.camera.mode = cls.flight_mode
        cls.wait(1)

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
        for page in (
            "home",
            "test",
            "orbit",
            "targetmenu",
            "aviapfd",
            "krpc",
            "krpctest",
        ):
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
        assert_png(self.monitor.image(), (640, 640))

    def test_display_outside_iva(self):
        # The text follows its variable with the flight camera on
        self.assertEqual(self.flight_mode, self.camera.mode)
        computer = self.rpm.computer(self.monitor.part)
        display = self.monitor.display
        display.text = "V {0:F1}$&$PERSISTENT_krpcOutside"
        self.show("krpc")
        for value in (1.5, 2.5):
            computer.set_persistent_variable("krpcOutside", value)
            # A monitor updates at most once a frame
            self.wait(0.1)
            self.assertEqual("V %.1f" % value, self.monitor.text)
        computer.remove_persistent_variable("krpcOutside")
        display.clear()
        self.show("home")

    def test_image_outside_iva(self):
        self.assertEqual(self.flight_mode, self.camera.mode)
        display = self.monitor.display
        self.show("krpc")
        blank = self.monitor.image()
        display.image = png(64, 64, (0, 0, 255))
        self.wait(0.1)
        self.assertNotEqual(blank, self.monitor.image())
        display.clear()
        self.show("home")

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
        cls.enter_iva()
        cls.version = cls.rpm.computer(cls.monitor.part).format_variables(
            "{0}$&$RPMVERSION"
        )

    @classmethod
    def tearDownClass(cls):
        cls.leave_iva()
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
        assert_png(self.monitor.image(), (640, 640))

    def test_image_jpeg(self):
        self.show("home")
        assert_jpeg(self.monitor.image(self.rpm.ImageFormat.jpeg), (640, 640))
        for quality in (0, 101):
            with self.assertRaises(ValueError):
                self.monitor.image(self.rpm.ImageFormat.jpeg, quality)
        # PNG ignores the quality
        assert_png(self.monitor.image(quality=0))

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


# The page buttons of RPM's bundled MFD, and the global buttons it does not use for pages
PAGE_BUTTONS = (
    ["buttonR%d" % i for i in range(1, 8)]
    + ["button_%s" % b for b in "ABCDEFG"]
    + ["button_STBY"]
)


class TestPageButtons(MonitorTestCase):
    def tearDown(self):
        for button in PAGE_BUTTONS:
            self.monitor.release_capture(button)
        self.monitor.active_page = "home"

    def test_page_buttons(self):
        self.assertCountEqual(PAGE_BUTTONS, self.monitor.page_buttons)

    def test_press_page_button(self):
        # The NAV button selects the map page
        self.monitor.press_page_button("buttonR1")
        self.assertEqual("map", self.monitor.active_page)
        # A button with two pages shows one, then the other
        self.monitor.press_page_button("buttonR2")
        self.assertEqual("orbit", self.monitor.active_page)
        self.monitor.press_page_button("buttonR2")
        self.assertEqual("orbitgraphical", self.monitor.active_page)

    def test_press_page_button_unknown(self):
        for button in ("buttonR8", "button_UP", "krpcNoSuchButton"):
            with self.assertRaises(ValueError):
                self.monitor.press_page_button(button)

    def test_capture(self):
        self.monitor.capture_button("buttonR1", "krpc")
        self.assertCountEqual(PAGE_BUTTONS, self.monitor.page_buttons)
        with self.monitor.captured_button_pressed("buttonR1") as captured:
            with captured.condition:
                self.monitor.press_page_button("buttonR1")
                self.assertTrue(captured.wait(5))
        self.assertEqual("krpc", self.monitor.active_page)
        # The other buttons are RPM's
        self.monitor.press_page_button("buttonR3")
        self.assertEqual("targetcam", self.monitor.active_page)

    def test_capture_other_page(self):
        # A captured button can show any page, here one of RPM's
        self.monitor.capture_button("buttonR1", "orbit")
        self.monitor.press_page_button("buttonR1")
        self.assertEqual("orbit", self.monitor.active_page)

    def test_capture_one_monitor(self):
        self.monitor.capture_button("buttonR1", "krpc")
        other = self.monitors[1]
        other.press_page_button("buttonR1")
        self.assertEqual("map", other.active_page)
        other.active_page = "home"

    def test_release(self):
        self.monitor.capture_button("buttonR1", "krpc")
        self.monitor.release_capture("buttonR1")
        with self.monitor.captured_button_pressed("buttonR1") as captured:
            with captured.condition:
                self.monitor.press_page_button("buttonR1")
                self.assertFalse(captured.wait(0.5))
        self.assertEqual("map", self.monitor.active_page)
        # Releasing a button that is not captured does nothing
        self.monitor.release_capture("buttonR1")

    def test_capture_invalid(self):
        with self.assertRaises(ValueError):
            self.monitor.capture_button("button_UP", "krpc")
        with self.assertRaises(ValueError):
            self.monitor.capture_button("buttonR1", "krpcNoSuchPage")


class TestExternalCamera(RasterPropMonitorTestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.cameras = sorted(
            cls.rpm.cameras(cls.vessel), key=lambda camera: camera.name
        )

    def tearDown(self):
        for camera in self.cameras:
            camera.fov = camera.max_fov

    def test_cameras(self):
        self.assertEqual(
            ["ExtCam1", "ExtCam2"], [camera.name for camera in self.cameras]
        )
        parts = self.vessel.parts.with_name(CAMERA)
        self.assertCountEqual(parts, [camera.part for camera in self.cameras])

    def test_field_of_view(self):
        camera = self.cameras[0]
        self.assertEqual(5, camera.min_fov)
        self.assertEqual(60, camera.max_fov)
        self.assertEqual(60, camera.fov)
        camera.fov = 30
        self.assertEqual(30, camera.fov)
        # A value outside the camera's range is clamped
        camera.fov = 1
        self.assertEqual(5, camera.fov)
        camera.fov = 100
        self.assertEqual(60, camera.fov)

    def test_image(self):
        # The flight camera is on, as the cameras render in any camera mode
        assert_png(self.cameras[0].image(320, 240), (320, 240))

    def test_images_differ(self):
        # The cameras face different ways
        first, second = (camera.image(160, 120) for camera in self.cameras)
        self.assertNotEqual(first, second)

    def test_image_field_of_view(self):
        camera = self.cameras[0]
        wide = camera.image(160, 120)
        camera.fov = camera.min_fov
        self.assertNotEqual(wide, camera.image(160, 120))

    def test_image_size(self):
        for width, height in ((0, 10), (10, 0), (-1, 10)):
            with self.assertRaises(ValueError):
                self.cameras[0].image(width, height)

    def test_image_jpeg(self):
        jpeg = self.rpm.ImageFormat.jpeg
        assert_jpeg(self.cameras[0].image(320, 240, jpeg), (320, 240))

    def test_image_quality(self):
        camera = self.cameras[0]
        jpeg = self.rpm.ImageFormat.jpeg
        small, large = (camera.image(320, 240, jpeg, quality) for quality in (10, 95))
        self.assertLess(len(small), len(large))

    def test_image_quality_range(self):
        jpeg = self.rpm.ImageFormat.jpeg
        for quality in (0, 101):
            with self.assertRaises(ValueError):
                self.cameras[0].image(10, 10, jpeg, quality)
        # PNG ignores the quality
        assert_png(self.cameras[0].image(10, 10, quality=0))

    def test_image_stream(self):
        # A stream at a rate is a video of the camera
        frames = []
        jpeg = self.rpm.ImageFormat.jpeg
        stream = self.conn.add_stream(self.cameras[0].image, 160, 120, jpeg)
        stream.rate = 10
        stream.add_callback(frames.append)
        try:
            time.sleep(0.5)
            count = len(frames)
            time.sleep(2)
            self.assertAlmostEqual(20, len(frames) - count, delta=4)
        finally:
            stream.remove()


# Tag text and the characters RPM draws for it, from TextRenderer.ParseText. A line with no
# closing bracket comes last, as RPM looks for the bracket past the end of the line.
TAG_RULES = [
    ("[hw]a[/hw][dw]b[/dw][sup]c[/sup][sub]d[/sub]", "abcd"),
    ("[@x5]e[@y-3]f[@xq]g", "efg"),
    ("[#ff0000]h[#ff000080]i", "hi"),
    ("[font1]j[font0]", "j"),
    ("[abc]k[#12]", "[abc]k[#12]"),
    ("x[]y", "x]y"),
    ("x[a]y", "xa]y"),
    ("[[]", "[]"),
    ("[[abc]", "[[abc]"),
    ("x[no close", "xno close"),
]


class TestDisplay(MonitorTestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.display = cls.monitor.display
        cls.enter_iva()

    @classmethod
    def tearDownClass(cls):
        cls.leave_iva()
        super().tearDownClass()

    def setUp(self):
        self.display.clear()
        self.show("krpc")

    def lines(self, monitor=None):
        return (monitor or self.monitor).text.split("\n")

    def test_own_display(self):
        self.assertIsNone(self.display.name)
        self.assertEqual(self.display, self.monitor.display)
        self.assertNotEqual(self.display, self.monitors[1].display)
        self.display.text = "Hello [#ff0000]World"
        self.assertEqual("Hello [#ff0000]World", self.display.text)
        self.wait(0.5)
        self.assertEqual(["Hello World"], self.lines())
        self.assertEqual("Hello [#ff0000]World", self.monitor.raw_text)

    def test_shown(self):
        self.assertTrue(self.display.shown)
        self.show("home")
        self.assertFalse(self.display.shown)
        self.show("krpc")
        self.assertTrue(self.display.shown)

    def test_tag_rules(self):
        self.display.text = "\n".join(text for text, _ in TAG_RULES)
        self.wait(0.5)
        self.assertEqual([drawn for _, drawn in TAG_RULES], self.lines())

    def test_variables(self):
        computer = self.rpm.computer(self.monitor.part)
        computer.set_persistent_variable("krpcDisplay", 2.5)
        self.display.text = "V {0:F1}$&$PERSISTENT_krpcDisplay"
        self.wait(0.5)
        self.assertEqual(["V 2.5"], self.lines())
        self.assertEqual(["PERSISTENT_krpcDisplay"], self.monitor.variables)
        # RPM updates the variable with no call to the display
        computer.set_persistent_variable("krpcDisplay", 3.5)
        self.wait(0.5)
        self.assertEqual(["V 3.5"], self.lines())
        computer.remove_persistent_variable("krpcDisplay")

    def test_image(self):
        self.wait(0.5)
        blank = self.monitor.image()
        red = png(64, 64, (255, 0, 0))
        self.display.image = red
        self.assertEqual(red, self.display.image)
        self.wait(0.5)
        self.assertNotEqual(blank, self.monitor.image())
        self.display.image = None
        self.assertIsNone(self.display.image)

    def test_invalid_image(self):
        with self.assertRaises(ValueError):
            self.display.image = b"not an image"
        self.assertIsNone(self.display.image)

    def test_clear(self):
        self.display.text = "Text"
        self.display.image = png(8, 8, (0, 255, 0))
        self.display.clear()
        self.assertEqual("", self.display.text)
        self.assertIsNone(self.display.image)

    def test_button_events(self):
        up = BUTTONS.index("button_UP")
        with self.display.button_pressed(up) as pressed:
            with self.display.button_released(up) as released:
                with pressed.condition:
                    self.monitor.press_button(up)
                    self.assertTrue(pressed.wait(5))
                with released.condition:
                    self.assertTrue(released.wait(5))

    def test_button_events_other_button(self):
        up = BUTTONS.index("button_UP")
        down = BUTTONS.index("button_DOWN")
        with self.display.button_pressed(up) as pressed:
            with pressed.condition:
                self.monitor.press_button(down)
                self.assertFalse(pressed.wait(0.5))

    def test_button_events_hidden(self):
        # A press on another page goes to that page's handler
        up = BUTTONS.index("button_UP")
        self.show("home")
        with self.display.button_pressed(up) as pressed:
            with pressed.condition:
                self.monitor.press_button(up)
                self.assertFalse(pressed.wait(0.5))

    def test_named_display(self):
        named = self.rpm.display(self.vessel, "test")
        self.assertEqual("test", named.name)
        self.assertEqual(named, self.rpm.display(self.vessel, "test"))
        self.assertNotEqual(named, self.display)
        named.text = "Shared"
        # The test overlay adds a page showing the display to every screen
        for monitor in self.monitors[:2]:
            monitor.active_page = "krpctest"
        self.wait(0.5)
        self.assertTrue(named.shown)
        for monitor in self.monitors[:2]:
            self.assertEqual(["Shared"], self.lines(monitor))
        for monitor in self.monitors[:2]:
            monitor.active_page = "krpc"
        self.assertFalse(named.shown)
        named.clear()

    def test_named_display_empty_name(self):
        with self.assertRaises(ValueError):
            self.rpm.display(self.vessel, " ")

    def test_capture_kept_when_iva_rebuilt(self):
        # KSP builds new buttons, and the capture is applied to them
        self.monitor.capture_button("buttonR1", "krpc")
        self.leave_iva()
        self.enter_iva()
        self.monitor.active_page = "home"
        self.monitor.press_page_button("buttonR1")
        self.assertEqual("krpc", self.monitor.active_page)
        self.monitor.release_capture("buttonR1")
        self.monitor.press_page_button("buttonR1")
        self.assertEqual("map", self.monitor.active_page)

    def test_kept_when_iva_rebuilt(self):
        self.display.text = "Kept"
        self.leave_iva()
        self.enter_iva()
        self.show("krpc")
        self.assertEqual(["Kept"], self.lines())


if __name__ == "__main__":
    unittest.main()
