import unittest
import krpc
import krpctest


class TestText(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        ui = cls.connect().ui
        cls.canvas = ui.stock_canvas
        cls.style = ui.FontStyle
        cls.anchor = ui.TextAnchor

    def test_text(self):
        text = self.canvas.add_text('Jebediah Kerman')
        self.assertIsNotNone(text.rect_transform)
        self.assertTrue(text.visible)
        self.assertEqual('Jebediah Kerman', text.content)
        self.assertEqual('Arial', text.font)
        self.assertGreater(len(text.available_fonts), 0)
        self.assertEqual(14, text.size)
        self.assertEqual(self.style.normal, text.style)
        self.assertAlmostEqual((0.196, 0.196, 0.196), text.color, places=3)
        self.assertEqual(self.anchor.upper_left, text.alignment)
        self.assertEqual(1, text.line_spacing)
        self.wait()
        text.remove()
        self.assertRaises(krpc.client.RPCError, text.remove)

    def test_properties(self):
        text = self.canvas.add_text('Jebediah Kerman')
        font = text.available_fonts[-1:][0]
        text.font = font
        text.size = 20
        text.style = self.style.bold
        text.color = (1, 0, 0)
        text.alignment = self.anchor.upper_right
        text.line_spacing = 2
        self.assertEqual(font, text.font)
        self.assertEqual(20, text.size)
        self.assertEqual(self.style.bold, text.style)
        self.assertEqual((1, 0, 0), text.color)
        self.assertEqual(self.anchor.upper_right, text.alignment)
        self.assertEqual(2, text.line_spacing)
        self.wait()
        text.remove()
        self.assertRaises(krpc.client.RPCError, text.remove)


if __name__ == '__main__':
    unittest.main()
