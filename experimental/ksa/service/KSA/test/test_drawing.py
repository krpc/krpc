import math
import time
from pathlib import Path

import ksatest
from PIL import Image, ImageChops

import krpc

MAGENTA = (1, 0, 1)


class DrawingCase(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        cls.drawing = cls.ksa.Drawing
        cls.camera = cls.ksa.camera

    def setUp(self):
        # A test can load the save, which replaces the vehicles
        self.camera.mode = self.ksa.CameraMode.orbit
        self.frame = self.camera.focused_vehicle.reference_frame

    def tearDown(self):
        self.drawing.clear()

    def magenta_pixels(self, ui=False):
        """The number of magenta pixels in a screenshot."""
        # The screenshot file name has the time to the second
        time.sleep(1)
        path = Path(self.ksa.take_screenshot(ui=ui))
        try:
            with Image.open(path) as image:
                r, g, b = image.convert("RGB").split()
        finally:
            path.unlink(missing_ok=True)
        # The lines are a pixel wide and blended, so pale. Red and blue both well above
        # green picks them out from the sky, the clouds and the vehicles
        threshold = lambda x: 255 if x > 60 else 0  # noqa: E731
        red = ImageChops.subtract(r, g).point(threshold)
        blue = ImageChops.subtract(b, g).point(threshold)
        return ImageChops.multiply(red, blue).histogram()[255]


class TestDrawn(DrawingCase):
    """Each kind of object shows in a screenshot, drawn around the focused vehicle."""

    def setUp(self):
        super().setUp()
        self.size = self.camera.distance / 2
        self.assertLess(self.magenta_pixels(), 20)

    def assertDrawn(self, ui=False):
        self.assertGreater(self.magenta_pixels(ui), 50)

    def axes(self):
        s = self.size
        return [(s, 0, 0), (0, s, 0), (0, 0, s)]

    def test_line(self):
        for axis in self.axes():
            line = self.drawing.add_line(tuple(-x for x in axis), axis, self.frame)
            line.color = MAGENTA
        self.assertDrawn()

    def test_direction(self):
        for axis in self.axes():
            self.drawing.add_direction(axis, self.frame, self.size).color = MAGENTA
        self.assertDrawn()

    def test_polyline(self):
        self.drawing.add_polyline(self.axes(), self.frame, True).color = MAGENTA
        self.assertDrawn()

    def test_circle(self):
        for axis in self.axes():
            self.drawing.add_circle(
                (0, 0, 0), axis, self.size, self.frame
            ).color = MAGENTA
        self.assertDrawn()

    def test_arc(self):
        for axis in self.axes():
            start = (axis[1], axis[2], axis[0])
            arc = self.drawing.add_arc(
                (0, 0, 0), axis, start, math.pi, self.size, self.frame
            )
            arc.color = MAGENTA
        self.assertDrawn()

    def test_box(self):
        s = self.size
        self.drawing.add_box(
            (0, 0, 0), (s, s, s), (0, 0, 0, 1), self.frame
        ).color = MAGENTA
        self.assertDrawn()

    def test_sphere(self):
        self.drawing.add_sphere((0, 0, 0), self.size, self.frame).color = MAGENTA
        self.assertDrawn()

    def test_text(self):
        self.drawing.add_text("MMMMMMMM", (0, 0, 0), self.frame, 64).color = MAGENTA
        # The text is drawn with the game's UI
        self.assertLess(self.magenta_pixels(), 20)
        self.assertDrawn(ui=True)

    def test_hidden(self):
        sphere = self.drawing.add_sphere((0, 0, 0), self.size, self.frame, False)
        sphere.color = MAGENTA
        self.assertLess(self.magenta_pixels(), 20)
        sphere.visible = True
        self.assertDrawn()

    def test_remove(self):
        sphere = self.drawing.add_sphere((0, 0, 0), self.size, self.frame)
        sphere.color = MAGENTA
        self.assertDrawn()
        sphere.remove()
        self.assertLess(self.magenta_pixels(), 20)

    def test_removed_on_disconnect(self):
        conn = krpc.connect(name="second")
        ksa = conn.ksa
        frame = ksa.camera.focused_vehicle.reference_frame
        ksa.Drawing.add_sphere((0, 0, 0), self.size, frame).color = MAGENTA
        self.assertDrawn()
        conn.close()
        ksatest.wait_until(lambda: self.magenta_pixels() < 20, timeout=30)

    def test_clear_client_only(self):
        conn = krpc.connect(name="second")
        self.addCleanup(conn.close)
        ksa = conn.ksa
        frame = ksa.camera.focused_vehicle.reference_frame
        sphere = ksa.Drawing.add_sphere((0, 0, 0), self.size, frame)
        sphere.color = MAGENTA
        self.drawing.clear(True)
        self.assertEqual(MAGENTA, sphere.color)
        self.drawing.clear()
        with self.assertRaises(conn.krpc.ObjectDestroyedException):
            _ = sphere.color

    def test_destroyed_frame(self):
        focused = self.camera.focused_vehicle
        other = next(v for v in self.ksa.vehicles if v != focused)
        line = self.drawing.add_line((0, 0, 0), (0, 0, 10), other.reference_frame)
        other.destroy()
        self.addCleanup(self.load_save)
        # Drawing carries on without the line
        self.drawing.add_sphere((0, 0, 0), self.size, self.frame).color = MAGENTA
        self.assertDrawn()
        self.assertEqual((0, 0, 10), line.end)


class TestProperties(DrawingCase):
    def assertVectorAlmostEqual(self, expected, actual, delta=1e-6):
        self.assertEqual(len(expected), len(actual))
        for x, y in zip(expected, actual):
            self.assertAlmostEqual(x, y, delta=delta)

    def test_line(self):
        line = self.drawing.add_line((1, 2, 3), (4, 5, 6), self.frame)
        self.assertEqual((1, 2, 3), line.start)
        self.assertEqual((4, 5, 6), line.end)
        self.assertEqual(self.frame, line.reference_frame)
        self.assertTrue(line.visible)
        self.assertEqual((1, 1, 1), line.color)
        line.start = (0, 0, 0)
        line.end = (1, 0, 0)
        line.color = (0.5, 0.25, 0)
        line.visible = False
        frame = self.ksa.bodies["Earth"].reference_frame
        line.reference_frame = frame
        self.assertEqual((0, 0, 0), line.start)
        self.assertEqual((1, 0, 0), line.end)
        self.assertEqual((0.5, 0.25, 0), line.color)
        self.assertFalse(line.visible)
        self.assertEqual(frame, line.reference_frame)

    def test_direction(self):
        line = self.drawing.add_direction((0, 2, 0), self.frame, 5)
        self.assertEqual((0, 0, 0), line.start)
        self.assertEqual((0, 5, 0), line.end)
        with self.assertRaises(ValueError):
            self.drawing.add_direction((0, 0, 0), self.frame)

    def test_polyline(self):
        vertices = [(0, 0, 0), (1, 0, 0), (1, 1, 0)]
        polyline = self.drawing.add_polyline(vertices, self.frame)
        self.assertEqual(vertices, polyline.vertices)
        self.assertFalse(polyline.closed)
        polyline.closed = True
        polyline.vertices = vertices[:2]
        self.assertTrue(polyline.closed)
        self.assertEqual(vertices[:2], polyline.vertices)

    def test_circle(self):
        circle = self.drawing.add_circle((1, 2, 3), (0, 0, 2), 10, self.frame)
        self.assertEqual((1, 2, 3), circle.center)
        self.assertEqual((0, 0, 1), circle.normal)
        self.assertEqual(10, circle.radius)
        with self.assertRaises(ValueError):
            circle.radius = 0
        with self.assertRaises(ValueError):
            circle.normal = (0, 0, 0)

    def test_arc(self):
        arc = self.drawing.add_arc((1, 2, 3), (0, 0, 1), (2, 0, 0), 1.5, 10, self.frame)
        self.assertEqual((1, 2, 3), arc.center)
        self.assertEqual((0, 0, 1), arc.normal)
        self.assertEqual((1, 0, 0), arc.start_direction)
        self.assertEqual(1.5, arc.angle)
        self.assertEqual(10, arc.radius)
        arc.angle = -1
        self.assertEqual(-1, arc.angle)
        with self.assertRaises(ValueError):
            arc.radius = -1

    def test_box(self):
        half = math.sqrt(0.5)
        box = self.drawing.add_box(
            (1, 2, 3), (4, 5, 6), (0, 0, 2 * half, 2 * half), self.frame
        )
        self.assertEqual((1, 2, 3), box.center)
        self.assertEqual((4, 5, 6), box.size)
        self.assertVectorAlmostEqual((0, 0, half, half), box.rotation)

    def test_sphere(self):
        sphere = self.drawing.add_sphere((1, 2, 3), 4, self.frame)
        self.assertEqual((1, 2, 3), sphere.center)
        self.assertEqual(4, sphere.radius)
        with self.assertRaises(ValueError):
            self.drawing.add_sphere((0, 0, 0), 0, self.frame)

    def test_text(self):
        text = self.drawing.add_text("Hello", (1, 2, 3), self.frame)
        self.assertEqual("Hello", text.content)
        self.assertEqual((1, 2, 3), text.position)
        self.assertEqual(16, text.size)
        text.content = ""
        text.size = 32
        self.assertEqual("", text.content)
        self.assertEqual(32, text.size)
        with self.assertRaises(ValueError):
            text.size = 0

    def test_remove(self):
        line = self.drawing.add_line((0, 0, 0), (1, 0, 0), self.frame)
        line.remove()
        with self.assertRaises(self.conn.krpc.ObjectDestroyedException):
            _ = line.start

    def test_clear(self):
        line = self.drawing.add_line((0, 0, 0), (1, 0, 0), self.frame)
        self.drawing.clear()
        with self.assertRaises(self.conn.krpc.ObjectDestroyedException):
            _ = line.start
