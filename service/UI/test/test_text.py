import unittest
import krpctest


class TestText(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.conn = cls.connect()
        cls.destroyed = cls.conn.krpc.ObjectDestroyedException
        ui = cls.connect().ui
        cls.canvas = ui.stock_canvas
        cls.style = ui.FontStyle
        cls.anchor = ui.TextAnchor

    def test_text(self):
        text = self.canvas.add_text("Jebediah Kerman")
        self.assertIsNotNone(text.rect_transform)
        self.assertTrue(text.visible)
        self.assertEqual("Jebediah Kerman", text.content)
        self.assertEqual("Arial", text.font)
        self.assertGreater(len(text.available_fonts), 0)
        # The size and color a label starts out with are taken from the skin the game
        # draws its own interface in, so they are not fixed values.
        self.assertGreater(text.size, 0)
        self.assertEqual(4, len(text.color))
        self.assertEqual(self.style.normal, text.style)
        self.assertEqual(self.anchor.upper_left, text.alignment)
        self.assertEqual(1, text.line_spacing)
        text.remove()
        self.assertRaises(self.destroyed, text.remove)

    def test_properties(self):
        text = self.canvas.add_text("Jebediah Kerman")
        font = text.available_fonts[-1:][0]
        text.font = font
        text.size = 20
        text.style = self.style.bold
        text.color = (1, 0, 0, 0.5)
        text.alignment = self.anchor.upper_right
        text.line_spacing = 2
        self.assertEqual(font, text.font)
        self.assertEqual(20, text.size)
        self.assertEqual(self.style.bold, text.style)
        self.assertEqual((1, 0, 0, 0.5), text.color)
        self.assertEqual(self.anchor.upper_right, text.alignment)
        self.assertEqual(2, text.line_spacing)
        text.remove()
        self.assertRaises(self.destroyed, text.remove)

    def test_word_wrap(self):
        text = self.canvas.add_text("Jebediah Kerman")
        self.assertTrue(text.word_wrap)
        # Turned off for a value label, so that the label asks a layout for one line
        # and a changing value does not reflow the interface.
        text.word_wrap = False
        self.assertFalse(text.word_wrap)
        text.word_wrap = True
        self.assertTrue(text.word_wrap)
        text.remove()
        self.assertRaises(self.destroyed, text.remove)


if __name__ == "__main__":
    unittest.main()
