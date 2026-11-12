import unittest

from krpctools.game.images import png
from krpctools.game.ksp import TestCase


def ring(x, y, width, height):
    """A ring, opaque at its middle and half transparent at its edges, on a clear
    background."""
    dx = (x + 0.5) / width - 0.5
    dy = (y + 0.5) / height - 0.5
    radius = (dx * dx + dy * dy) ** 0.5
    if 0.3 < radius < 0.45:
        alpha = 255 if 0.34 < radius < 0.41 else 128
        return (255, 128, 0, alpha)
    return (0, 0, 0, 0)


def ring_png(width, height):
    return png(width, height, lambda x, y: ring(x, y, width, height))


class TestImage(TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.conn = cls.connect()
        cls.destroyed = cls.conn.krpc.ObjectDestroyedException
        cls.drawing = cls.conn.drawing
        cls.ref = cls.conn.space_center.active_vessel.reference_frame

    def test_image(self):
        image = self.drawing.add_image(ring_png(32, 16))
        self.assertEqual(32, image.width)
        self.assertEqual(16, image.height)
        image.remove()
        self.assertRaises(self.destroyed, image.remove)
        self.assertRaises(self.destroyed, getattr, image, "width")

    def test_invalid_data(self):
        self.assertRaises(ValueError, self.drawing.add_image, b"not an image")

    def test_map_marker(self):
        image = self.drawing.add_image(ring_png(32, 32))
        marker = self.drawing.add_map_marker((0, 0, 0), self.ref)
        self.assertIsNone(marker.image)
        marker.image = image
        self.wait_for_frame()
        self.assertEqual(image, marker.image)
        marker.icon = self.drawing.MapMarkerIcon.probe
        self.wait_for_frame()
        image.remove()
        self.wait_for_frame()
        marker.image = None
        self.assertIsNone(marker.image)
        marker.remove()

    def test_navball_marker(self):
        image = self.drawing.add_image(ring_png(32, 32))
        marker = self.drawing.add_navball_marker((0, 1, 0), self.ref)
        self.assertIsNone(marker.image)
        marker.image = image
        self.wait_for_frame()
        self.assertEqual(image, marker.image)
        marker.icon = "default"
        self.wait_for_frame()
        marker.image = None
        self.wait_for_frame()
        self.assertIsNone(marker.image)
        marker.remove()
        image.remove()


if __name__ == "__main__":
    unittest.main()
