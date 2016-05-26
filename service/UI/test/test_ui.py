import unittest
import krpctest

class TestUI(krpctest.TestCase):

    @classmethod
    def setUp(cls):
        krpctest.new_save()
        cls.conn = krpctest.connect(cls)
        cls.ui = cls.conn.ui
        cls.message_position = cls.conn.ui.MessagePosition

    @classmethod
    def tearDown(cls):
        cls.conn.close()

    def test_add_panel(self):
        panel = self.ui.add_panel()
        self.assertTrue(panel.visible)
        panel = self.ui.add_panel(False)
        self.assertFalse(panel.visible)

    def test_message(self):
        self.ui.message("One")
        self.ui.message("Two", 5)
        self.ui.message("Three", 1, self.message_position.top_right)

    def test_rect_transform(self):
        rect = self.ui.rect_transform
        width, height = rect.size
        self.assertGreater(width, 0)
        self.assertGreater(height, 0)
        self.assertEqual((0, 0), rect.position)
        self.assertEqual((0, 0, 625), rect.local_position)
        self.assertEqual((width, height), rect.size)
        self.assertEqual((width/2, height/2), rect.upper_right)
        self.assertEqual((-width/2, -height/2), rect.lower_left)
        self.assertEqual((0, 0), rect.anchor_max)
        self.assertEqual((0, 0), rect.anchor_min)
        self.assertEqual((0.5, 0.5), rect.pivot)
        self.assertEqual((0, 0, 0, 1), rect.rotation)
        self.assertEqual((1, 1, 1), rect.scale)

if __name__ == '__main__':
    unittest.main()
