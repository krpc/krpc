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
