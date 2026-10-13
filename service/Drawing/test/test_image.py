import struct
import unittest
import zlib
import krpctest


def png(width, height):
    """A PNG image of a ring, opaque at its middle and half transparent at its edges,
    on a clear background."""
    rows = b""
    for y in range(height):
        rows += b"\x00"
        for x in range(width):
            dx = (x + 0.5) / width - 0.5
            dy = (y + 0.5) / height - 0.5
            radius = (dx * dx + dy * dy) ** 0.5
            if 0.3 < radius < 0.45:
                alpha = 255 if 0.34 < radius < 0.41 else 128
                rows += bytes((255, 128, 0, alpha))
            else:
                rows += bytes((0, 0, 0, 0))

    def chunk(kind, data):
        return (
            struct.pack(">I", len(data))
            + kind
            + data
            + struct.pack(">I", zlib.crc32(kind + data) & 0xFFFFFFFF)
        )

    header = struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)
    return (
        b"\x89PNG\r\n\x1a\n"
        + chunk(b"IHDR", header)
        + chunk(b"IDAT", zlib.compress(rows))
        + chunk(b"IEND", b"")
    )


class TestImage(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.conn = cls.connect()
        cls.destroyed = cls.conn.krpc.ObjectDestroyedException
        cls.drawing = cls.conn.drawing
        cls.ref = cls.conn.space_center.active_vessel.reference_frame

    def test_image(self):
        image = self.drawing.add_image(png(32, 16))
        self.assertEqual(32, image.width)
        self.assertEqual(16, image.height)
        image.remove()
        self.assertRaises(self.destroyed, image.remove)
        self.assertRaises(self.destroyed, getattr, image, "width")

    def test_invalid_data(self):
        self.assertRaises(ValueError, self.drawing.add_image, b"not an image")

    def test_map_marker(self):
        image = self.drawing.add_image(png(32, 32))
        marker = self.drawing.add_map_marker((0, 0, 0), self.ref)
        self.assertIsNone(marker.image)
        marker.image = image
        self.wait()
        self.assertEqual(image, marker.image)
        marker.icon = self.drawing.MapMarkerIcon.probe
        self.wait()
        image.remove()
        self.wait()
        marker.image = None
        self.assertIsNone(marker.image)
        marker.remove()

    def test_navball_marker(self):
        image = self.drawing.add_image(png(32, 32))
        marker = self.drawing.add_navball_marker((0, 1, 0), self.ref)
        self.assertIsNone(marker.image)
        marker.image = image
        self.wait()
        self.assertEqual(image, marker.image)
        marker.icon = "default"
        self.wait()
        marker.image = None
        self.wait()
        self.assertIsNone(marker.image)
        marker.remove()
        image.remove()


if __name__ == "__main__":
    unittest.main()
