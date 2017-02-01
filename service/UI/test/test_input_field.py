import unittest
import krpc
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
        self.assertEqual('', input_field.value)
        self.assertIsNotNone(input_field.text)
        self.assertFalse(input_field.changed)
        self.wait()
        input_field.remove()
        self.assertRaises(krpc.client.RPCError, input_field.remove)

    def test_value(self):
        input_field = self.canvas.add_input_field()
        self.assertEqual('', input_field.value)
        self.assertFalse(input_field.changed)
        input_field.value = 'Foo'
        self.assertTrue(input_field.changed)
        input_field.changed = False
        self.assertFalse(input_field.changed)
        input_field.remove()


if __name__ == '__main__':
    unittest.main()
