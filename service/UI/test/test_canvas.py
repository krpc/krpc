import unittest
import krpctest


class TestCanvas(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.ui = cls.connect().ui
        cls.canvas = cls.ui.stock_canvas

    def test_same_element_is_the_same_object(self):
        # An element is the same object however it is reached, so that a client can
        # compare the objects it is given and hold on to them.
        self.assertEqual(self.canvas, self.ui.stock_canvas)
        panel = self.canvas.add_panel()
        self.assertEqual(panel.rect_transform, panel.rect_transform)
        other = self.canvas.add_panel()
        self.assertNotEqual(panel, other)
        other.remove()
        panel.remove()

    def test_an_added_canvas_is_scaled_like_the_stock_one(self):
        # Unity applies the scale of a canvas to its transform, so an interface built on
        # a canvas a client adds comes out the same size as one on the stock canvas.
        canvas = self.ui.add_canvas()
        # The scale factor is applied by Unity in its own update, not when the canvas is
        # created, so the transform is only scaled from the next frame on.
        self.wait()
        self.assertEqual(self.canvas.rect_transform.scale, canvas.rect_transform.scale)
        canvas.remove()

    def test_the_stock_canvas_cannot_be_removed(self):
        # It belongs to the game, not to the client that reached it.
        self.assertRaises(RuntimeError, self.canvas.remove)

    def test_add_panel(self):
        panel = self.canvas.add_panel()
        self.assertTrue(panel.visible)
        panel.remove()
        panel = self.canvas.add_panel(False)
        self.assertFalse(panel.visible)
        panel.remove()

    def test_rect_transform(self):
        rect = self.canvas.rect_transform
        width, height = rect.size
        self.assertGreater(width, 0)
        self.assertGreater(height, 0)
        self.assertEqual((0, 0), rect.position)
        self.assertEqual((0, 0, 625), rect.local_position)
        self.assertEqual((width, height), rect.size)
        self.assertEqual((width / 2, height / 2), rect.upper_right)
        self.assertEqual((-width / 2, -height / 2), rect.lower_left)
        self.assertEqual((0, 0), rect.anchor_max)
        self.assertEqual((0, 0), rect.anchor_min)
        self.assertEqual((0.5, 0.5), rect.pivot)
        self.assertEqual((0, 0, 0, 1), rect.rotation)
        # The scale of the stock canvas follows the interface scale the player has set,
        # so there is no one value to check it against.


if __name__ == "__main__":
    unittest.main()
