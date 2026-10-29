import time
from pathlib import Path

import ksatest
from PIL import Image, ImageChops

import krpc


class UICase(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        cls.ui = cls.ksa.UI
        cls.tools = cls.conn.testing_tools

    def setUp(self):
        self.window = self.ui.add_window("krpctest")
        self.window.position = (100, 100)

    def tearDown(self):
        self.ui.clear()

    def frames(self):
        """Let the game draw a few frames."""
        time.sleep(0.5)


class TestWindow(UICase):
    def screenshot(self):
        """A screenshot with the UI, as RGB."""
        # The screenshot file name has the time to the second
        time.sleep(1)
        path = Path(self.ksa.take_screenshot(ui=True))
        try:
            with Image.open(path) as image:
                return image.convert("RGB")
        finally:
            path.unlink(missing_ok=True)

    def changed_pixels(self, a, b):
        threshold = lambda x: 255 if x > 40 else 0  # noqa: E731
        diff = ImageChops.difference(a, b).convert("L").point(threshold)
        return diff.histogram()[255]

    def test_drawn(self):
        self.window.size = (400, 300)
        self.window.add_label("Hello")
        shown = self.screenshot()
        self.window.visible = False
        hidden = self.screenshot()
        self.assertGreater(self.changed_pixels(shown, hidden), 400 * 300 // 2)

    def test_properties(self):
        self.assertEqual("krpctest", self.window.title)
        self.assertTrue(self.window.visible)
        self.assertTrue(self.window.enabled)
        self.window.title = "renamed"
        self.window.enabled = False
        self.assertEqual("renamed", self.window.title)
        self.assertFalse(self.window.enabled)

    def test_position(self):
        ksatest.wait_until(lambda: self.window.position == (100, 100))
        self.window.position = (200, 150)
        self.assertEqual((200, 150), self.window.position)
        self.frames()
        self.assertEqual((200, 150), self.window.position)

    def test_fits_widgets(self):
        ksatest.wait_until(lambda: self.window.size[0] > 0)
        width = self.window.size[0]
        self.window.add_label("A label much wider than the title of the window")
        ksatest.wait_until(lambda: self.window.size[0] > width + 100)

    def test_size(self):
        self.window.size = (500, 400)
        self.frames()
        self.assertEqual((500, 400), self.window.size)
        self.window.add_label("A label much wider than the window " * 10)
        self.frames()
        self.assertEqual((500, 400), self.window.size)
        with self.assertRaises(ValueError):
            self.window.size = (0, 100)

    def test_cascade(self):
        other = self.ui.add_window("other")
        self.assertNotEqual(self.window.position, other.position)

    def test_remove(self):
        row = self.window.add_row()
        button = row.add_button("Button")
        self.window.remove()
        for obj in (self.window, row, button):
            with self.assertRaises(self.conn.krpc.ObjectDestroyedException):
                _ = obj.visible

    def test_remove_widget(self):
        label = self.window.add_label("Label")
        label.remove()
        with self.assertRaises(self.conn.krpc.ObjectDestroyedException):
            _ = label.content
        self.assertTrue(self.window.visible)

    def test_clear_client_only(self):
        conn = krpc.connect(name="second")
        self.addCleanup(conn.close)
        other = conn.ksa.UI.add_window("second")
        self.ui.clear(True)
        self.assertEqual("second", other.title)
        with self.assertRaises(self.conn.krpc.ObjectDestroyedException):
            _ = self.window.title

    def test_removed_on_disconnect(self):
        conn = krpc.connect(name="second")
        other = conn.ksa.UI.add_window("second")
        label = other.add_label("Label")
        # The window's id, to look it up from this connection
        window_id = other._object_id
        label_id = label._object_id
        conn.close()
        window = self.ksa.Window(self.conn, window_id)
        label = self.ksa.Label(self.conn, label_id)

        def removed(obj):
            try:
                _ = obj.visible
            except self.conn.krpc.ObjectDestroyedException:
                return True
            return False

        ksatest.wait_until(lambda: removed(window) and removed(label))


class TestWidgets(UICase):
    def test_label(self):
        label = self.window.add_label("Hello")
        self.assertEqual("Hello", label.content)
        label.content = "Goodbye"
        self.assertEqual("Goodbye", label.content)
        self.assertTrue(label.visible)
        label.visible = False
        self.assertFalse(label.visible)

    def test_button(self):
        button = self.window.add_button("Press")
        self.assertEqual("Press", button.content)
        self.assertFalse(button.clicked)
        ksatest.wait_until(lambda: self.tools.click_button(button))
        ksatest.wait_until(lambda: button.clicked)
        button.clicked = False
        self.frames()
        self.assertFalse(button.clicked)

    def test_disabled(self):
        button = self.window.add_button("Press")
        button.enabled = False
        ksatest.wait_until(lambda: self.tools.click_button(button))
        self.frames()
        self.assertFalse(button.clicked)

    def test_disabled_window(self):
        button = self.window.add_button("Press")
        self.window.enabled = False
        ksatest.wait_until(lambda: self.tools.click_button(button))
        self.frames()
        self.assertFalse(button.clicked)

    def test_row(self):
        row = self.window.add_row()
        a = row.add_button("A")
        b = row.add_button("B")
        ksatest.wait_until(lambda: self.tools.click_button(b))
        ksatest.wait_until(lambda: b.clicked)
        self.assertFalse(a.clicked)

    def test_toggle(self):
        toggle = self.window.add_toggle("Check")
        self.assertEqual("Check", toggle.content)
        self.assertFalse(toggle.checked)
        toggle.checked = True
        self.assertTrue(toggle.checked)
        self.assertFalse(toggle.changed)
        ksatest.wait_until(lambda: self.tools.click_toggle(toggle))
        ksatest.wait_until(lambda: toggle.changed)
        self.assertFalse(toggle.checked)

    def test_input_field(self):
        field = self.window.add_input_field("Name")
        self.assertEqual("Name", field.label)
        self.assertEqual("", field.value)
        field.value = "Jeb"
        self.assertEqual("Jeb", field.value)
        self.assertFalse(field.changed)
        ksatest.wait_until(lambda: self.tools.type_text(field, "!"))
        ksatest.wait_until(lambda: field.changed)
        self.assertEqual("Jeb!", field.value)

    def test_input_field_clears_controls(self):
        field = self.window.add_input_field("Name")
        button = self.window.add_button("Done")
        control = self.ksa.active_vehicle.control
        self.addCleanup(setattr, control, "pitch", 0)
        ksatest.wait_until(lambda: self.tools.type_text(field, "x"))
        ksatest.wait_until(lambda: field.changed)
        control.pitch = 1
        self.frames()
        self.assertEqual(0, control.pitch)
        # Clicking elsewhere ends the edit
        ksatest.wait_until(lambda: self.tools.click_button(button))
        ksatest.wait_until(lambda: button.clicked)
        control.pitch = 1
        self.frames()
        self.assertEqual(1, control.pitch)

    def test_slider(self):
        slider = self.window.add_slider("", 10, 20)
        self.assertEqual("", slider.label)
        self.assertEqual(10, slider.value)
        self.assertEqual(10, slider.minimum)
        self.assertEqual(20, slider.maximum)
        ksatest.wait_until(lambda: self.tools.click_slider(slider, 0.5))
        ksatest.wait_until(lambda: slider.changed)
        self.assertAlmostEqual(15, slider.value, delta=1)

    def test_slider_range(self):
        slider = self.window.add_slider("Value", 0, 10)
        slider.value = 20
        self.assertEqual(10, slider.value)
        slider.maximum = 5
        self.assertEqual(5, slider.value)
        slider.minimum = -5
        slider.value = -10
        self.assertEqual(-5, slider.value)
        with self.assertRaises(ValueError):
            slider.minimum = 5
        with self.assertRaises(ValueError):
            slider.maximum = -5
        with self.assertRaises(ValueError):
            self.window.add_slider("Value", 1, 1)

    def test_dropdown(self):
        dropdown = self.window.add_dropdown(["A", "B", "C"], "Pick")
        self.assertEqual("Pick", dropdown.label)
        self.assertEqual(["A", "B", "C"], dropdown.options)
        self.assertEqual(-1, dropdown.selected_index)
        dropdown.selected_index = 0
        self.assertEqual(0, dropdown.selected_index)
        self.assertFalse(dropdown.changed)
        ksatest.wait_until(lambda: self.tools.click_dropdown(dropdown))
        ksatest.wait_until(lambda: self.tools.click_dropdown_option(dropdown, 2))
        ksatest.wait_until(lambda: dropdown.changed)
        self.assertEqual(2, dropdown.selected_index)

    def test_dropdown_options(self):
        dropdown = self.window.add_dropdown(["A", "B", "C"], "Pick")
        dropdown.selected_index = 2
        dropdown.options = ["A", "B", "C", "D"]
        self.assertEqual(2, dropdown.selected_index)
        dropdown.options = ["A"]
        self.assertEqual(-1, dropdown.selected_index)
        with self.assertRaises(ValueError):
            dropdown.selected_index = 1

    def test_separator(self):
        separator = self.window.add_separator()
        self.assertTrue(separator.visible)
