import unittest
import time
import krpc
import krpctest

class TestButton(krpctest.TestCase):

    @classmethod
    def setUp(cls):
        krpctest.new_save()
        cls.conn = krpctest.connect(cls)
        cls.ui = cls.conn.ui

    @classmethod
    def tearDown(cls):
        cls.conn.close()

    def add_button(self):
        panel = self.ui.add_panel()
        return panel.add_button('Foo')

    def test_button(self):
        button = self.add_button()
        self.assertIsNotNone(button.rect_transform)
        self.assertTrue(button.visible)
        self.assertIsNotNone(button.text)
        self.assertEqual('Foo', button.text.content)
        self.assertFalse(button.clicked)
        time.sleep(0.5)
        button.remove()
        self.assertRaises(krpc.client.RPCError, button.remove)

if __name__ == '__main__':
    unittest.main()
