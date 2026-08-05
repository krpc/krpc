import unittest
import krpctest


class TestInputField(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        ui = cls.connect().ui
        cls.canvas = ui.stock_canvas
        cls.content_types = ui.InputContentType

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
        input_field.value = "Foo"
        self.assertEqual("Foo", input_field.value)
        input_field.remove()

    def test_setting_the_value_is_not_a_change_by_the_user(self):
        input_field = self.canvas.add_input_field()
        self.assertFalse(input_field.changed)
        input_field.value = "Foo"
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

    def test_content_type(self):
        # The filter applies to what the user types, which a test cannot do, so this
        # covers the round trip and that a client's own value is not filtered.
        input_field = self.canvas.add_input_field()
        types = self.content_types
        self.assertEqual(types.standard, input_field.content_type)
        for content_type in (
            types.integer,
            types.decimal,
            types.alphanumeric,
            types.password,
            types.standard,
        ):
            input_field.content_type = content_type
            self.assertEqual(content_type, input_field.content_type)
        input_field.content_type = types.decimal
        input_field.value = "1.25"
        self.assertEqual("1.25", input_field.value)
        input_field.remove()

    def test_character_limit(self):
        input_field = self.canvas.add_input_field()
        self.assertEqual(0, input_field.character_limit)
        input_field.character_limit = 5
        self.assertEqual(5, input_field.character_limit)
        self.assertRaises(ValueError, setattr, input_field, "character_limit", -1)
        self.assertEqual(5, input_field.character_limit)
        input_field.character_limit = 0
        self.assertEqual(0, input_field.character_limit)
        input_field.remove()

    def test_read_only(self):
        input_field = self.canvas.add_input_field()
        self.assertFalse(input_field.read_only)
        input_field.read_only = True
        self.assertTrue(input_field.read_only)
        # A client can still set the value of a read-only field.
        input_field.value = "Foo"
        self.assertEqual("Foo", input_field.value)
        input_field.read_only = False
        self.assertFalse(input_field.read_only)
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
