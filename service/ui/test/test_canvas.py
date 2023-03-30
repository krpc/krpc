import unittest
import krpctest


class TestCanvas(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.canvas = cls.connect().ui.stock_canvas

    def test_add_panel(self):
        panel = self.canvas.add_panel()
        self.assertTrue(panel.visible)
        panel = self.canvas.add_panel(False)
        self.assertFalse(panel.visible)

    def test_rect_transform(self):
        rect = self.canvas.rect_transform
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
        self.assertEqual((1.0, 1.0, 1.0), rect.scale)


if __name__ == '__main__':
    unittest.main()
