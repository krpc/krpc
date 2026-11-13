import struct
import unittest

from krpctools.game.images import (
    assert_jpeg,
    assert_png,
    jpeg_size,
    png,
    png_size,
)

# A JPEG of 3 by 2 pixels, cut after its start of frame segment
JPEG = (
    b"\xff\xd8"
    + b"\xff\xe0"
    + struct.pack(">H", 16)
    + b"JFIF\x00" * 2
    + b"\x00\x00\x00\x00"
    + b"\xff\xc0"
    + struct.pack(">HBHHB", 11, 8, 2, 3, 1)
    + b"\x01\x11\x00"
)


class TestImages(unittest.TestCase):
    def test_png(self):
        image = png(3, 2, (255, 0, 0))
        assert_png(image)
        assert_png(image, (3, 2))
        self.assertEqual((3, 2), png_size(image))
        self.assertEqual(2, image[25])

    def test_png_pixels(self):
        image = png(4, 1, lambda x, y: (x, y, 0, 255))
        assert_png(image, (4, 1))
        self.assertEqual(6, image[25])

    def test_assert_png(self):
        with self.assertRaises(AssertionError):
            assert_png(JPEG)
        with self.assertRaises(AssertionError):
            assert_png(png(3, 2, (0, 0, 0)), (2, 3))

    def test_jpeg(self):
        assert_jpeg(JPEG)
        assert_jpeg(JPEG, (3, 2))
        self.assertEqual((3, 2), jpeg_size(JPEG))

    def test_assert_jpeg(self):
        with self.assertRaises(AssertionError):
            assert_jpeg(png(3, 2, (0, 0, 0)))
        with self.assertRaises(AssertionError):
            assert_jpeg(JPEG, (2, 3))

    def test_jpeg_without_frame(self):
        with self.assertRaises(ValueError):
            jpeg_size(b"\xff\xd8\xff\xd9")


if __name__ == "__main__":
    unittest.main()
