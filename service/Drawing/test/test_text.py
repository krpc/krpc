import unittest
import krpc
import krpctest


class TestText(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.drawing = cls.connect().drawing
        cls.vessel = cls.connect().space_center.active_vessel
        cls.ref = cls.vessel.reference_frame
        ui = cls.connect().ui
        cls.style = ui.FontStyle
        cls.alignment = ui.TextAlignment
        cls.anchor = ui.TextAnchor

    def add_text(self):
        return self.drawing.add_text(
            "Jebediah Kerman", self.ref, (0, 0, 0), (0, 0, 0, 1))

    def test_text(self):
        text = self.add_text()
        self.assertEqual("Jebediah Kerman", text.content)
        self.assertEqual(self.ref, text.reference_frame)
        self.assertEqual((0, 0, 0), text.position)
        self.assertEqual((0, 0, 0, 1), text.rotation)
        self.assertEqual("Arial", text.font)
        self.assertGreater(len(text.available_fonts), 0)
        self.assertEqual(12, text.size)
        self.assertEqual(1, text.character_size)
        self.assertEqual(self.style.normal, text.style)
        self.assertEqual((1, 1, 1), text.color)
        self.assertEqual("GUI/Text Shader", text.material)
        self.assertEqual(self.alignment.left, text.alignment)
        self.assertEqual(1, text.line_spacing)
        self.assertEqual(self.anchor.upper_left, text.anchor)
        self.wait()
        text.remove()
        self.assertRaises(krpc.client.RPCError, text.remove)

    def test_text_properties(self):
        text = self.add_text()
        font = text.available_fonts[-1:][0]
        text.font = font
        text.size = 20
        text.character_size = 2
        text.style = self.style.bold
        text.color = (1, 0, 0)
        text.alignment = self.alignment.right
        text.line_spacing = 2
        text.anchor = self.anchor.upper_right
        self.assertEqual(font, text.font)
        self.assertEqual(20, text.size)
        self.assertEqual(2, text.character_size)
        self.assertEqual(self.style.bold, text.style)
        self.assertEqual((1, 0, 0), text.color)
        self.assertEqual(self.alignment.right, text.alignment)
        self.assertEqual(2, text.line_spacing)
        self.assertEqual(self.anchor.upper_right, text.anchor)
        self.wait()
        text.remove()
        self.assertRaises(krpc.client.RPCError, text.remove)


if __name__ == '__main__':
    unittest.main()
