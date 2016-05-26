import unittest
import time
import krpc
import krpctest

class TestText(krpctest.TestCase):

    @classmethod
    def setUp(cls):
        krpctest.new_save()
        cls.conn = krpctest.connect(cls)
        cls.ui = cls.conn.ui
        cls.style = cls.conn.ui.FontStyle
        cls.anchor = cls.conn.ui.TextAnchor

    @classmethod
    def tearDown(cls):
        cls.conn.close()

    def add_text(self):
        panel = self.ui.add_panel()
        return panel.add_text("Jebediah Kerman")

    def test_text(self):
        text = self.add_text()
        self.assertIsNotNone(text.rect_transform)
        self.assertTrue(text.visible)
        self.assertEquals("Jebediah Kerman", text.content)
        self.assertEquals("Arial", text.font)
        self.assertGreater(len(text.available_fonts), 0)
        self.assertEquals(14, text.size)
        self.assertEquals(self.style.normal, text.style)
        self.assertClose((0.196, 0.196, 0.196), text.color, error=0.001)
        self.assertEquals(self.anchor.upper_left, text.alignment)
        self.assertEquals(1, text.line_spacing)
        time.sleep(0.5)
        text.remove()
        self.assertRaises(krpc.client.RPCError, text.remove)

    def test_properties(self):
        text = self.add_text()
        font = text.available_fonts[-1:][0]
        text.font = str(font) # FIXME: font has type unicode not type str
        text.size = 20
        text.style = self.style.bold
        text.color = (1, 0, 0)
        text.alignment = self.anchor.upper_right
        text.line_spacing = 2
        self.assertEquals(font, text.font)
        self.assertEquals(20, text.size)
        self.assertEquals(self.style.bold, text.style)
        self.assertEquals((1, 0, 0), text.color)
        self.assertEquals(self.anchor.upper_right, text.alignment)
        self.assertEquals(2, text.line_spacing)
        time.sleep(0.5)
        text.remove()
        self.assertRaises(krpc.client.RPCError, text.remove)

if __name__ == '__main__':
    unittest.main()
