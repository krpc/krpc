import unittest
import krpctest


class TestSlider(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.conn = cls.connect()
        cls.destroyed = cls.conn.krpc.ObjectDestroyedException
        cls.canvas = cls.conn.ui.stock_canvas

    def test_slider(self):
        slider = self.canvas.add_slider()
        self.assertIsNotNone(slider.rect_transform)
        self.assertTrue(slider.visible)
        self.assertEqual(0, slider.min)
        self.assertEqual(1, slider.max)
        self.assertEqual(0, slider.value)
        self.assertFalse(slider.changed)
        slider.remove()
        self.assertRaises(self.destroyed, slider.remove)

    def test_value(self):
        slider = self.canvas.add_slider()
        # The range must never be crossed, so it is grown from the end that is moving
        # away before the other end is brought up to it.
        slider.max = 20
        slider.min = 10
        slider.value = 15
        self.assertEqual(10, slider.min)
        self.assertEqual(20, slider.max)
        self.assertEqual(15, slider.value)
        slider.remove()

    def test_setting_the_value_is_not_a_change_by_the_user(self):
        slider = self.canvas.add_slider()
        # Setting the range moves the value into it, which is not a change by the user
        # either.
        slider.max = 20
        slider.min = 10
        self.assertEqual(10, slider.value)
        self.assertFalse(slider.changed)
        slider.value = 15
        self.assertFalse(slider.changed)
        slider.remove()

    def test_a_value_outside_the_range_is_refused(self):
        # Unity would silently move the value to the nearest end of the range, leaving
        # the client reading back a value it did not set with no way to tell.
        slider = self.canvas.add_slider()
        slider.min = 0
        slider.max = 10
        slider.value = 5
        self.assertRaises(ValueError, setattr, slider, "value", 100)
        self.assertRaises(ValueError, setattr, slider, "value", -100)
        self.assertEqual(5, slider.value)
        slider.remove()

    def test_a_range_with_its_ends_crossed_is_refused(self):
        slider = self.canvas.add_slider()
        slider.min = 0
        slider.max = 10
        self.assertRaises(ValueError, setattr, slider, "min", 20)
        self.assertRaises(ValueError, setattr, slider, "max", -20)
        self.assertEqual(0, slider.min)
        self.assertEqual(10, slider.max)
        slider.remove()

    def test_vertical(self):
        slider = self.canvas.add_slider(vertical=True)
        self.assertTrue(slider.visible)
        # A vertical slider is taller than it is wide, where a horizontal one is the
        # other way round.
        size = slider.rect_transform.size
        self.assertGreater(size[1], size[0])
        slider.max = 10
        slider.value = 5
        self.assertEqual(5, slider.value)
        slider.remove()

    def test_interactable(self):
        slider = self.canvas.add_slider()
        self.assertTrue(slider.interactable)
        slider.interactable = False
        self.assertFalse(slider.interactable)
        slider.remove()


if __name__ == "__main__":
    unittest.main()
