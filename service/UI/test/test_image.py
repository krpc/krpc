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
        image.color = (0, 1, 0)
        self.assertEqual((0, 1, 0), image.color)
        image.remove()

    def test_content(self):
        image = self.canvas.add_image()
        image.content = PNG
        self.assertEqual(PNG, image.content)
        image.content = b""
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
