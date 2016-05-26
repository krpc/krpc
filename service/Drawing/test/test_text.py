import unittest
import time
import krpc
import krpctest

class TestText(krpctest.TestCase):

    @classmethod
    def setUp(cls):
        krpctest.new_save()
        cls.conn = krpctest.connect(cls)
        cls.drawing = cls.conn.drawing
        cls.vessel = cls.conn.space_center.active_vessel
        cls.ref = cls.vessel.reference_frame
        cls.style = cls.conn.ui.FontStyle
        cls.alignment = cls.conn.ui.TextAlignment
        cls.anchor = cls.conn.ui.TextAnchor

    @classmethod
    def tearDown(cls):
        cls.conn.close()

    def add_text(self):
        return self.drawing.add_text("Jebediah Kerman", self.ref, (0, 0, 0), (0, 0, 0, 1))

    def test_text(self):
        text = self.add_text()
        self.assertEquals("Jebediah Kerman", text.content)
        self.assertEquals(self.ref, text.reference_frame)
        self.assertEquals((0, 0, 0), text.position)
        self.assertEquals((0, 0, 0, 1), text.rotation)
        self.assertEquals("Arial", text.font)
        self.assertGreater(len(text.available_fonts), 0)
        self.assertEquals(12, text.size)
        self.assertEquals(1, text.character_size)
        self.assertEquals(self.style.normal, text.style)
        self.assertEquals((1, 1, 1), text.color)
        self.assertEquals("GUI/Text Shader", text.material)
        self.assertEquals(self.alignment.left, text.alignment)
        self.assertEquals(1, text.line_spacing)
        self.assertEquals(self.anchor.upper_left, text.anchor)
        time.sleep(0.5)
        text.remove()
        self.assertRaises(krpc.client.RPCError, text.remove)

    def test_text_properties(self):
        text = self.add_text()
        font = text.available_fonts[-1:][0]
        text.font = str(font) # FIXME: font has type unicode not type str
        text.size = 20
        text.character_size = 2
        text.style = self.style.bold
        text.color = (1, 0, 0)
        text.alignment = self.alignment.right
        text.line_spacing = 2
        text.anchor = self.anchor.upper_right
        self.assertEquals(font, text.font)
        self.assertEquals(20, text.size)
        self.assertEquals(2, text.character_size)
        self.assertEquals(self.style.bold, text.style)
        self.assertEquals((1, 0, 0), text.color)
        self.assertEquals(self.alignment.right, text.alignment)
        self.assertEquals(2, text.line_spacing)
        self.assertEquals(self.anchor.upper_right, text.anchor)
        time.sleep(0.5)
        text.remove()
        self.assertRaises(krpc.client.RPCError, text.remove)

if __name__ == '__main__':
    unittest.main()
