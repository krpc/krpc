import unittest
import time
import krpc
import krpctest

class TestInputField(krpctest.TestCase):

    @classmethod
    def setUp(cls):
        krpctest.new_save()
        cls.conn = krpctest.connect(cls)
        cls.ui = cls.conn.ui

    @classmethod
    def tearDown(cls):
        cls.conn.close()

    def add_input_field(self):
        panel = self.ui.add_panel()
        return panel.add_input_field()

    def test_input_field(self):
        input_field = self.add_input_field()
        self.assertIsNotNone(input_field.rect_transform)
        self.assertTrue(input_field.visible)
        self.assertEquals('', input_field.value)
        self.assertIsNotNone(input_field.text)
        self.assertFalse(input_field.changed)
        time.sleep(0.5)
        input_field.remove()
        self.assertRaises(krpc.client.RPCError, input_field.remove)

    def test_value(self):
        input_field = self.add_input_field()
        self.assertEquals('', input_field.value)
        self.assertFalse(input_field.changed)
        input_field.value = 'Foo'
        self.assertTrue(input_field.changed)
        input_field.changed = False
        self.assertFalse(input_field.changed)
        input_field.remove()

if __name__ == '__main__':
    unittest.main()
