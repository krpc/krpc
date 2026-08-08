import base64
import unittest
import krpctest

# A 2x2 PNG, small enough to keep in the test rather than on disk.
PNG = base64.b64decode(
    "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAIAAAD91JpzAAAAFklEQVQI12P8"
    "z8DAwMDAxAADTGgcMAAALAAM/1RxRgAAAABJRU5ErkJggg=="
)


class TestImage(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.canvas = cls.connect().ui.stock_canvas

    def test_image(self):
        image = self.canvas.add_image()
        self.assertIsNotNone(image.rect_transform)
        self.assertTrue(image.visible)
        self.assertEqual(b"", image.content)
        image.remove()
        self.assertRaises(ValueError, image.remove)

    def test_color(self):
        image = self.canvas.add_image()
        image.color = (0, 1, 0, 1)
        self.assertEqual((0, 1, 0, 1), image.color)
        image.remove()

    def test_content(self):
        image = self.canvas.add_image()
        image.content = PNG
        self.assertEqual(PNG, image.content)
        image.content = b""
        self.assertEqual(b"", image.content)
        image.remove()

    def test_set_pixels(self):
        image = self.canvas.add_image()
        red = b"\xff\x00\x00\xff"
        green = b"\x00\xff\x00\xff"
        image.set_pixels(red * 4, 2, 2)
        # The raw pixels are not kept, so there are no file contents to read back.
        self.assertEqual(b"", image.content)
        # Redrawing at the same size draws into the picture already there, and a new
        # size makes a new picture.
        image.set_pixels(green * 4, 2, 2)
        image.set_pixels(red * 8, 4, 2)
        image.remove()

    def test_set_pixels_needs_the_right_amount_of_data(self):
        image = self.canvas.add_image()
        self.assertRaises(ValueError, image.set_pixels, b"\xff" * 15, 2, 2)
        self.assertRaises(ValueError, image.set_pixels, b"", 2, 2)
        self.assertRaises(ValueError, image.set_pixels, b"\xff" * 16, 0, 2)
        self.assertRaises(ValueError, image.set_pixels, b"\xff" * 16, -2, -2)
        image.remove()

    def test_update_pixels(self):
        image = self.canvas.add_image()
        red = b"\xff\x00\x00\xff"
        green = b"\x00\xff\x00\xff"
        image.set_pixels(red * 16, 4, 4)
        # Redraw blocks in the middle, in a corner and along an edge, without sending
        # the rest of the picture again.
        image.update_pixels(green * 4, 1, 1, 2, 2)
        image.update_pixels(green * 4, 0, 0, 2, 2)
        image.update_pixels(green * 2, 3, 2, 1, 2)
        image.remove()

    def test_update_pixels_needs_a_raw_picture(self):
        # There is nothing to draw into until SetPixels has set the size, and a
        # picture loaded from a file is not redrawn: the file contents a client reads
        # back would no longer be what is on the screen.
        image = self.canvas.add_image()
        pixel = b"\x00\xff\x00\xff"
        self.assertRaises(RuntimeError, image.update_pixels, pixel, 0, 0, 1, 1)
        image.content = PNG
        self.assertRaises(RuntimeError, image.update_pixels, pixel, 0, 0, 1, 1)
        image.remove()

    def test_update_pixels_must_fit_inside_the_picture(self):
        image = self.canvas.add_image()
        image.set_pixels(b"\xff\x00\x00\xff" * 16, 4, 4)
        pixel = b"\x00\xff\x00\xff"
        self.assertRaises(ValueError, image.update_pixels, pixel * 4, 3, 3, 2, 2)
        self.assertRaises(ValueError, image.update_pixels, pixel * 4, -1, 0, 2, 2)
        self.assertRaises(ValueError, image.update_pixels, pixel * 4, 0, -1, 2, 2)
        self.assertRaises(ValueError, image.update_pixels, pixel * 3, 0, 0, 2, 2)
        self.assertRaises(ValueError, image.update_pixels, pixel * 4, 0, 0, 0, 2)
        image.remove()

    def test_a_file_and_pixels_replace_each_other(self):
        image = self.canvas.add_image()
        image.set_pixels(b"\xff\x00\x00\xff" * 4, 2, 2)
        image.content = PNG
        self.assertEqual(PNG, image.content)
        image.set_pixels(b"\x00\xff\x00\xff" * 4, 2, 2)
        self.assertEqual(b"", image.content)
        image.remove()

    def test_content_rejects_junk(self):
        image = self.canvas.add_image()
        image.content = PNG
        self.assertRaises(ValueError, setattr, image, "content", b"not an image")
        self.assertEqual(PNG, image.content)
        image.remove()


if __name__ == "__main__":
    unittest.main()
