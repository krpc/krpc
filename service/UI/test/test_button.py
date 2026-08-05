import unittest
import krpctest


class TestButton(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.canvas = cls.connect().ui.stock_canvas

    def test_button(self):
        button = self.canvas.add_button("Foo")
        self.assertIsNotNone(button.rect_transform)
        self.assertTrue(button.visible)
        self.assertIsNotNone(button.text)
        self.assertEqual("Foo", button.text.content)
        self.assertFalse(button.clicked)
        button.remove()
        self.assertRaises(ValueError, button.remove)

    def test_the_label_cannot_be_removed_on_its_own(self):
        # The label is part of the button, so it goes when the button goes.
        button = self.canvas.add_button("Foo")
        self.assertRaises(RuntimeError, button.text.remove)
        button.remove()

    def test_pressed(self):
        # Pressing needs a pointer, which a test does not have, so this covers that a
        # button that has never been touched is not reported as held down.
        button = self.canvas.add_button("Foo")
        self.assertFalse(button.pressed)
        button.remove()

    def test_color(self):
        # Tinting the control is how a script shows a state of its own, such as an
        # engaged autopilot, the way the game's own interfaces do.
        button = self.canvas.add_button("Foo")
        self.assertEqual((1, 1, 1, 1), button.color)
        button.color = (0, 1, 0, 1)
        self.assertEqual((0, 1, 0, 1), button.color)
        button.remove()

    def test_tooltip(self):
        # The tooltip is only shown while the pointer rests on the control, which a
        # test does not have, so this covers the round trip.
        button = self.canvas.add_button("Foo")
        self.assertEqual("", button.tooltip)
        button.tooltip = "Runs the self test"
        self.assertEqual("Runs the self test", button.tooltip)
        button.tooltip = ""
        self.assertEqual("", button.tooltip)
        button.remove()

    def test_interactable(self):
        button = self.canvas.add_button("Foo")
        self.assertTrue(button.interactable)
        button.interactable = False
        self.assertFalse(button.interactable)
        self.assertTrue(button.visible)
        button.interactable = True
        self.assertTrue(button.interactable)
        button.remove()


if __name__ == "__main__":
    unittest.main()
