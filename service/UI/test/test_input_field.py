import unittest
import krpctest


class TestInputField(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.canvas = cls.connect().ui.stock_canvas

    def test_input_field(self):
        input_field = self.canvas.add_input_field()
        self.assertIsNotNone(input_field.rect_transform)
        self.assertTrue(input_field.visible)
        self.assertEqual("", input_field.value)
        self.assertIsNotNone(input_field.text)
        self.assertFalse(input_field.changed)
        input_field.remove()
        self.assertRaises(ValueError, input_field.remove)

    def test_value(self):
        input_field = self.canvas.add_input_field()
        self.assertEqual("", input_field.value)
        self.assertFalse(input_field.changed)
        input_field.value = "Foo"
        self.assertTrue(input_field.changed)
        input_field.changed = False
        self.assertFalse(input_field.changed)
        input_field.remove()

    def test_placeholder(self):
        input_field = self.canvas.add_input_field()
        self.assertEqual("", input_field.placeholder.content)
        input_field.placeholder.content = "Enter a name"
        self.assertEqual("Enter a name", input_field.placeholder.content)
        # The hint is not a value.
        self.assertEqual("", input_field.value)
        input_field.remove()

    def test_the_placeholder_cannot_be_removed_on_its_own(self):
        # The placeholder is part of the input field, so it goes when the field goes.
        input_field = self.canvas.add_input_field()
        self.assertRaises(RuntimeError, input_field.placeholder.remove)
        input_field.remove()

    def test_interactable(self):
        input_field = self.canvas.add_input_field()
        self.assertTrue(input_field.interactable)
        input_field.interactable = False
        self.assertFalse(input_field.interactable)
        self.assertTrue(input_field.visible)
        input_field.interactable = True
        self.assertTrue(input_field.interactable)
        input_field.remove()


if __name__ == "__main__":
    unittest.main()
